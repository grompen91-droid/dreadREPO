param(
    [string]$PrTitle = "",
    [string]$PrBody = "",
    [string]$ChangedFiles = "",
    [string]$PrAuthor = ""
)

$exitCode = 0
$reasons = @()

$projectKeywords = @(
    'dread', 'audio', 'monster', 'tension', 'panic', 'sprint',
    'adrenaline', 'footstep', 'breath', 'crouch', 'bepinex', 'harmony',
    'config', 'stamina', 'enemy', 'plugin', 'fix', 'feat', 'refactor',
    'docs', 'chore', 'ci', 'build', 'version', 'thunderstore'
)

$projectFiles = @(
    'Plugin\.cs', 'DreadConfig\.cs', 'AudioDreadSystem\.cs',
    'MonsterOverhaulSystem\.cs', 'TensionSystem\.cs',
    '\.csproj', 'manifest\.json', 'build\.ps1', 'CHANGELOG\.md',
    'README\.md', 'icon\.png', 'audio/', 'docs/', '\.github/',
    'AGENTS\.md', 'CLAUDE\.md'
)

if ($PrTitle) {
    $titleLower = $PrTitle.ToLower()
    $hasKeyword = ($projectKeywords | Where-Object { $titleLower -match [regex]::Escape($_) }).Count -gt 0
    if (-not $hasKeyword) {
        $exitCode = 1
        $reasons += "PR title does not mention any project-relevant keywords"
    }
} else {
    $exitCode = 1
    $reasons += "PR has no title"
}

if ($PrBody) {
    $bodyLower = $PrBody.ToLower()
    $bodyLines = ($PrBody -split "`n").Count
    if ($bodyLines -lt 3) {
        $reasons += "PR body is very short ($bodyLines lines) -- may be missing context"
    }
    if ($bodyLower -match '^\s*$|update.*readme|fix.*typo|bump|version') {
        $reasons += "PR body appears minimal or non-descriptive"
    }
} else {
    $reasons += "PR has no body"
}

if ($ChangedFiles) {
    $files = $ChangedFiles -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
    $inScope = 0
    $outOfScope = 0
    $outFiles = @()

    foreach ($f in $files) {
        $matched = $false
        foreach ($pattern in $projectFiles) {
            if ($f -match $pattern) {
                $matched = $true
                break
            }
        }
        if ($matched) { $inScope++ }
        else {
            $outOfScope++
            $outFiles += $f
        }
    }

    if ($inScope -eq 0 -and $files.Count -gt 0) {
        $exitCode = 1
        $reasons += "No changed files match known project paths"
    }
    if ($outOfScope -gt $inScope -and $files.Count -gt 1) {
        $reasons += "Most changed files ($outOfScope of $($files.Count)) are outside expected project paths"
    }
}

$result = @{
    passed  = ($exitCode -eq 0)
    reasons = $reasons
}

$result | ConvertTo-Json -Compress

exit $exitCode