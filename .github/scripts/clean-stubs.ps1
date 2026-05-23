param(
    [string]$StubsDir = "$PSScriptRoot/../stubs"
)

$stubsPath = Resolve-Path $StubsDir -ErrorAction SilentlyContinue
if ($stubsPath -and (Test-Path $stubsPath)) {
    Remove-Item -Recurse -Force $stubsPath
    Write-Host "Deleted cached stubs: $stubsPath"
} else {
    Write-Host "No stubs cache found at $stubsPath"
}
