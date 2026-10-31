[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][ValidateSet('public', 'beta')][string]$Channel,
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$ReleaseInfoPath = Join-Path $GameDir 'release_info.json'
$ReleaseInfo = Get-Content -LiteralPath $ReleaseInfoPath -Raw -Encoding UTF8 | ConvertFrom-Json
$GameVersion = [version]($ReleaseInfo.version -replace '^v', '')
if (($Channel -eq 'public' -and $GameVersion -ne [version]'0.107.1') -or
    ($Channel -eq 'beta' -and $GameVersion -lt [version]'0.108.0')) {
    throw "Installed game $GameVersion does not match the requested $Channel reference channel."
}

$DataDir = Join-Path $GameDir 'data_sts2_windows_x86_64'
$Assemblies = @('sts2.dll', '0Harmony.dll', 'GodotSharp.dll')
foreach ($Assembly in $Assemblies) {
    if (-not (Test-Path -LiteralPath (Join-Path $DataDir $Assembly) -PathType Leaf)) {
        throw "Missing game reference: $Assembly under $DataDir"
    }
}

$Destination = Join-Path $ProjectRoot "lib\$Channel"
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
foreach ($Assembly in $Assemblies) {
    Copy-Item -LiteralPath (Join-Path $DataDir $Assembly) -Destination $Destination -Force
}
Copy-Item -LiteralPath $ReleaseInfoPath -Destination $Destination -Force
if ($Channel -eq 'beta') {
    # main still uses this historical path. Public builds use lib/public directly.
    Copy-Item -LiteralPath (Join-Path $Destination 'sts2.dll') -Destination (Join-Path $ProjectRoot 'lib\sts2.dll') -Force
}
Write-Host "Cached game $GameVersion references in $Destination"
