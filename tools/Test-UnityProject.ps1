[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'NeuralCourt'),
    [string]$UnityPath,
    [string]$LogPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'unity-validation.log'),
    [ValidateRange(1, 180)]
    [int]$TimeoutMinutes = 25
)

$ErrorActionPreference = 'Stop'

function Find-UnityEditor {
    $candidates = @()
    if ($UnityPath) { $candidates += $UnityPath }

    $hubRoots = @(
        (Join-Path $env:ProgramFiles 'Unity Hub\Editor'),
        (Join-Path ${env:ProgramFiles(x86)} 'Unity\Hub\Editor'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Unity Hub\Editor')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

    foreach ($root in $hubRoots) {
        $candidates += Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' }
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw 'Unity.exe was not found. Open Unity Hub > Installs, copy the Editor path, and rerun with -UnityPath "C:\...\Editor\Unity.exe".'
}

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Unity project folder not found: $ProjectPath. Initialize the NeuralCourt submodule first by running tools\Install-ShowcasePatch.ps1."
}
$project = (Resolve-Path -LiteralPath $ProjectPath).Path
if (-not (Test-Path -LiteralPath (Join-Path $project 'Assets\Scenes\BasketballDemo.unity'))) {
    throw "Expected demo scene is missing from $project."
}

$editor = Find-UnityEditor
$log = [System.IO.Path]::GetFullPath($LogPath)
$logDirectory = Split-Path -Parent $log
if ($logDirectory -and -not (Test-Path -LiteralPath $logDirectory)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

Write-Host "Unity Editor: $editor" -ForegroundColor Cyan
Write-Host "Project:      $project" -ForegroundColor Cyan
Write-Host "Log:          $log" -ForegroundColor Cyan
Write-Host "Running batch-mode project import and C# compilation (timeout: $TimeoutMinutes minutes). This does not prove Play Mode gameplay or visual quality." -ForegroundColor Yellow

# Allow Unity's API updater to run during a clean import of projects upgraded between versions.
$arguments = @(
    '-accept-apiupdate',
    '-batchmode',
    '-quit',
    '-projectPath', ('"' + $project + '"'),
    '-logFile', ('"' + $log + '"')
)
$process = Start-Process -FilePath $editor -ArgumentList $arguments -PassThru
$timeoutMilliseconds = $TimeoutMinutes * 60 * 1000
if (-not $process.WaitForExit($timeoutMilliseconds)) {
    try { $process.Kill() } catch { }
    throw "Unity batch validation exceeded $TimeoutMinutes minutes. Process was stopped; inspect $log for an import/package stall."
}
$process.Refresh()

$logText = if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Raw } else { '' }
$compileErrors = @(
    [regex]::Matches($logText, '(?im)^.*error CS\d+:.*$') |
        ForEach-Object { $_.Value.Trim() } |
        Select-Object -Unique
)
$unityErrors = @(
    [regex]::Matches($logText, '(?im)^.*\b(error|exception)\b.*$') |
        ForEach-Object { $_.Value.Trim() } |
        Select-Object -Unique
)

if ($compileErrors.Count -gt 0) {
    Write-Host "`nC# compile errors:" -ForegroundColor Red
    $compileErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

$fatalImportPatterns = @(
    '(?im)^.*Script compilation failed.*$',
    '(?im)^.*Failed to resolve packages.*$',
    '(?im)^.*Aborting batchmode due to failure.*$',
    '(?im)^.*Assembly-CSharp.*error.*$'
)
$fatalImportMatches = foreach ($pattern in $fatalImportPatterns) {
    [regex]::Matches($logText, $pattern) |
        ForEach-Object { $_.Value.Trim() }
}
$fatalImportErrors = @($fatalImportMatches | Select-Object -Unique)
if ($fatalImportErrors.Count -gt 0) {
    Write-Host "`nUnity import/compilation failures:" -ForegroundColor Red
    $fatalImportErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

if ($process.ExitCode -ne 0) {
    Write-Host "`nUnity exited with code $($process.ExitCode). Review $log" -ForegroundColor Red
    $unityErrors | Select-Object -Last 30 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit $process.ExitCode
}

Write-Host "`nBatch-mode import/compile completed without detected C# compiler or fatal import errors." -ForegroundColor Green
Write-Host "Log saved to: $log"
Write-Host 'Next: open BasketballDemo.unity, run Tools > NBA2K12 Showcase > 16 - One-Click Core Setup, then test 5v5 movement, pass, shoot, score/restart, and a wired controller in Play Mode.'
