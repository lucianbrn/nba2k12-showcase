$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$upstreamRoot = Join-Path $repoRoot 'NeuralCourt'
$scene = Join-Path $upstreamRoot 'Assets\Scenes\BasketballDemo.unity'
$source = Join-Path $repoRoot 'patches\Assets\NBA2K12Showcase'
$target = Join-Path $upstreamRoot 'Assets\NBA2K12Showcase'

if (-not (Test-Path (Join-Path $repoRoot '.gitmodules'))) {
    throw 'Run this script from a clone of https://github.com/lucianbrn/nba2k12-showcase.'
}

if (-not (Test-Path $scene)) {
    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        throw 'Git for Windows is required to initialize the NeuralCourt submodule. Install Git and rerun this script.'
    }
    Write-Host 'Initializing the Unity source submodule...' -ForegroundColor Cyan
    & $git.Source -C $repoRoot submodule update --init --recursive
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $scene)) {
        throw 'Could not initialize NeuralCourt. Review the Git output and verify repository access.'
    }
}

if (-not (Test-Path $source)) { throw "Patch source is missing: $source" }
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force

Write-Host ''
Write-Host 'NBA2K12 Showcase editor patch installed.' -ForegroundColor Green
Write-Host "Unity project: $upstreamRoot"
Write-Host 'Open that folder in Unity Hub, then open Assets/Scenes/BasketballDemo.unity.'
Write-Host 'Use Tools > NBA2K12 Showcase to apply presentation settings, set palettes and audit the scene.'
