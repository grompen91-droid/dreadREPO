param(
    [string]$OutDir = "$PSScriptRoot/../stubs/refs"
)

$ErrorActionPreference = "Stop"
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $false
}

$stubsDir = New-Item -ItemType Directory -Force $OutDir | Select-Object -ExpandProperty FullName

$stubCode = Get-Content "$PSScriptRoot/UnityEngine_stubs.cs" -Raw

$unityStubCs = "$stubsDir/UnityEngine_stubs.cs"
$stubCode | Out-File -FilePath $unityStubCs -Encoding utf8

$emptyStubCs = "$stubsDir/_empty.cs"
'// empty stub' | Out-File -FilePath $emptyStubCs -Encoding utf8

function Write-StubProject {
    param([string]$Name, [string]$SourceFile, [string]$Directory, [string[]]$References)

    $refsBlock = ""
    if ($References) {
        $refLines = $References | ForEach-Object { "    <Reference Include=`"$_`" HintPath=`"$Directory/$_.dll`" />`n" }
        $refsBlock = "  <ItemGroup>`n$refLines  </ItemGroup>"
    }

    $csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <AssemblyName>$Name</AssemblyName>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <Configurations>Release</Configurations>
     <OutDir>$Directory</OutDir>
     <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
     <IntermediateOutputPath>$Directory/obj/$Name/</IntermediateOutputPath>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$SourceFile" />
  </ItemGroup>
$refsBlock
</Project>
"@
    $csprojPath = "$Directory/$Name.csproj"
    $csprojContent | Out-File -FilePath $csprojPath -Encoding utf8
    return $csprojPath
}

# Build the real Unity stubs (has actual types)
$unityCsproj = Write-StubProject -Name "UnityEngine" -SourceFile $unityStubCs -Directory $stubsDir
Write-Host "[gen-stubs] Compiling UnityEngine stubs..."
$output = dotnet build $unityCsproj -c Release --nologo 2>&1
$exitCode = $LASTEXITCODE
$output | Out-String | ForEach-Object { Write-Host "$_" }
if ($exitCode -ne 0) {
    Write-Host "::error::[gen-stubs] Failed to compile UnityEngine stubs (exit $exitCode)"
    exit 1
}
$dllPath = "$stubsDir/UnityEngine.dll"
if (Test-Path $dllPath) {
    $size = (Get-Item $dllPath).Length / 1KB
    Write-Host "[gen-stubs] Created UnityEngine.dll ($size KB)"
}

# Build the Assembly-CSharp stubs (game-specific types, depends on UnityEngine stubs)
$acCsproj = Write-StubProject -Name "Assembly-CSharp" -SourceFile "$PSScriptRoot/Assembly-CSharp_stubs.cs" -Directory $stubsDir -References @("UnityEngine")
Write-Host "[gen-stubs] Compiling Assembly-CSharp stubs..."
$output = dotnet build $acCsproj -c Release --nologo 2>&1
$exitCode = $LASTEXITCODE
$output | Out-String | ForEach-Object { Write-Host "$_" }
if ($exitCode -ne 0) {
    Write-Host "::error::[gen-stubs] Failed to compile Assembly-CSharp stubs (exit $exitCode)"
    exit 1
}
$acDllPath = "$stubsDir/Assembly-CSharp.dll"
if (Test-Path $acDllPath) {
    $size = (Get-Item $acDllPath).Length / 1KB
    Write-Host "[gen-stubs] Created Assembly-CSharp.dll ($size KB)"
}

# Generate empty stub assemblies (no MSBuild restore needed, fast sequential builds)
$emptyAssemblies = @(
    'UnityEngine.CoreModule',
    'UnityEngine.AudioModule',
    'UnityEngine.UI',
    'UnityEngine.PhysicsModule',
    'UnityEngine.ImageConversionModule',
    'UnityEngine.AnimationModule',
    'UnityEngine.AIModule',
    'UnityEngine.UIModule',
    'UnityEngine.UnityWebRequestModule',
    'UnityEngine.UnityWebRequestAudioModule',
    'PhotonUnityNetworking',
    'Photon3Unity3D'
)

foreach ($name in $emptyAssemblies) {
    $csproj = Write-StubProject -Name $name -SourceFile $emptyStubCs -Directory $stubsDir
    $output = dotnet build $csproj -c Release --nologo 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $output | Out-String | ForEach-Object { Write-Host "$_" }
        Write-Host "::error::[gen-stubs] Failed to compile $name (exit $exitCode)"
        exit 1
    }
}

# Download and extract BepInEx (cached across runs)
$bepinVersion = "5.4.21"
$bepinUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepinVersion/BepInEx_x64_$bepinVersion.0.zip"
$bepinZip = "$stubsDir/../bepinex.zip"
$bepinDir = "$stubsDir/../bepinex"

$coreDir = "$stubsDir/core"
New-Item -ItemType Directory -Force $coreDir | Out-Null

if (!(Test-Path "$coreDir/BepInEx.dll")) {
    if (!(Test-Path "$bepinDir/BepInEx/core/BepInEx.dll")) {
        Write-Host "[gen-stubs] Downloading BepInEx $bepinVersion..."
        try {
            Invoke-WebRequest -Uri $bepinUrl -OutFile $bepinZip -UseBasicParsing -ErrorAction Stop
            Expand-Archive -Path $bepinZip -DestinationPath $bepinDir -Force
            Remove-Item $bepinZip -Force
        } catch {
            Write-Warning "[gen-stubs] BepInEx download failed: $_"
        }
    }
    if (Test-Path "$bepinDir/BepInEx/core/BepInEx.dll") {
        Copy-Item "$bepinDir/BepInEx/core/BepInEx.dll" "$coreDir/BepInEx.dll"
        Copy-Item "$bepinDir/BepInEx/core/0Harmony.dll" "$coreDir/0Harmony.dll"
        Write-Host "[gen-stubs] Copied BepInEx.dll ($((Get-Item "$coreDir/BepInEx.dll").Length / 1KB) KB)"
        Write-Host "[gen-stubs] Copied 0Harmony.dll ($((Get-Item "$coreDir/0Harmony.dll").Length / 1KB) KB)"
    } else {
        Write-Host "::error::[gen-stubs] BepInEx not found after download attempt from $bepinUrl"
        exit 1
    }
} else {
    Write-Host "[gen-stubs] BepInEx core already present (cached)"
}

Write-Host "[gen-stubs] Done"
