$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$upstreamRoot = Join-Path $repoRoot 'NeuralCourt'
$scene = Join-Path $upstreamRoot 'Assets\Scenes\BasketballDemo.unity'
$source = Join-Path $repoRoot 'patches\Assets\NBA2K12Showcase'
$target = Join-Path $upstreamRoot 'Assets\NBA2K12Showcase'
$manifestPath = Join-Path $upstreamRoot 'Packages\manifest.json'

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
if (-not (Test-Path $manifestPath)) { throw "Unity Packages/manifest.json was not found: $manifestPath" }

New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force

# glTFast provides Unity import support for GLB/GLTF files. Keep a backup of the existing manifest.
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$dependencies = $manifest.dependencies
if ($null -eq $dependencies) { throw 'Packages/manifest.json has no dependencies object.' }

$gltfProperty = $dependencies.PSObject.Properties['com.unity.cloud.gltfast']
if ($null -eq $gltfProperty) {
    $backupPath = $manifestPath + '.backup.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
    Copy-Item -LiteralPath $manifestPath -Destination $backupPath -ErrorAction Stop
    Add-Member -InputObject $dependencies -MemberType NoteProperty -Name 'com.unity.cloud.gltfast' -Value '6.20.0'
    $json = $manifest | ConvertTo-Json -Depth 100
    Set-Content -LiteralPath $manifestPath -Value $json -Encoding UTF8
    Write-Host "Added glTFast 6.20.0. Original manifest backed up to: $backupPath" -ForegroundColor Yellow
} elseif ([string]$gltfProperty.Value -ne '6.20.0') {
    Write-Host "glTFast is already configured as $($gltfProperty.Value). The installer did not change your chosen version." -ForegroundColor Yellow
} else {
    Write-Host 'glTFast 6.20.0 is already in the project manifest.' -ForegroundColor Green
}

$privateRoot = Join-Path $target 'PrivateAssets'
New-Item -ItemType Directory -Path $privateRoot -Force | Out-Null
$ignorePath = Join-Path $privateRoot '.gitignore'
if (-not (Test-Path $ignorePath)) {
    $ignoreContents = @("# Local proprietary/third-party imports are never committed.", "*", "!.gitignore") -join [Environment]::NewLine
    Set-Content -LiteralPath $ignorePath -Value $ignoreContents -Encoding UTF8
}

Write-Host ''
Write-Host 'NBA2K12 Showcase editor patch installed.' -ForegroundColor Green
Write-Host "Unity project: $upstreamRoot"
Write-Host 'Open the NeuralCourt folder in Unity Hub, then open Assets/Scenes/BasketballDemo.unity.'
Write-Host 'Unity may need a few minutes to resolve the newly added glTFast package.'
Write-Host 'Use Tools > NBA2K12 Showcase to apply gameplay framing, the live HUD/roster, and CC0 asset import tools.'
