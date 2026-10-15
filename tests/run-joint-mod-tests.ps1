[CmdletBinding()]
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$RitsuLibDir = "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747602295",
    [string]$YjcmDir = "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3782162360",
    [string]$NewsanguoDir = "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3784364146",
    [string]$GodotExe = "",
    [int]$TimeoutSeconds = 300,
    [switch]$ListOnly,
    [switch]$SkipModInstall
)

$ErrorActionPreference = "Stop"
$TestsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$GameDir = [System.IO.Path]::GetFullPath($GameDir)
$GameModsDir = Join-Path $GameDir "mods"

function Sync-WorkshopMod {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDir,
        [Parameter(Mandatory = $true)]
        [string]$ManifestName,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedId
    )

    $SourceDir = [System.IO.Path]::GetFullPath($SourceDir)
    $ManifestPath = Join-Path $SourceDir $ManifestName
    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        throw "$ExpectedId manifest was not found: $ManifestPath"
    }

    $Manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($Manifest.id -ne $ExpectedId) {
        throw "Unexpected mod id '$($Manifest.id)' in $ManifestPath; expected '$ExpectedId'."
    }

    $TargetDir = Join-Path $GameModsDir $ExpectedId
    if ([System.IO.Path]::GetFullPath($TargetDir) -eq $SourceDir) {
        throw "Source and target directories must differ for ${ExpectedId}: $SourceDir"
    }

    Write-Host "Syncing $ExpectedId $($Manifest.version) from Workshop..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null
    Get-ChildItem -LiteralPath $SourceDir -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $TargetDir -Recurse -Force
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $GameDir "SlayTheSpire2.exe") -PathType Leaf)) {
    throw "SlayTheSpire2.exe was not found under: $GameDir"
}
if ($TimeoutSeconds -le 0) {
    throw "TimeoutSeconds must be greater than zero."
}

Sync-WorkshopMod -SourceDir $YjcmDir -ManifestName "mod_manifest.json" -ExpectedId "Yjcm"
Sync-WorkshopMod -SourceDir $NewsanguoDir -ManifestName "newsanguo.json" -ExpectedId "newsanguo"

$RunArgs = @{
    GameDir = $GameDir
    RitsuLibDir = $RitsuLibDir
    Filter = "JointModTests"
    TimeoutSeconds = $TimeoutSeconds
    JointModTests = $true
}
if (-not [string]::IsNullOrWhiteSpace($GodotExe)) {
    $RunArgs.GodotExe = $GodotExe
}
if ($ListOnly) {
    $RunArgs.ListOnly = $true
}
if ($SkipModInstall) {
    $RunArgs.SkipModInstall = $true
}

& (Join-Path $TestsDir "run-tests.ps1") @RunArgs
