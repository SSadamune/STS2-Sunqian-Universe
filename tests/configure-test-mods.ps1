param(
    [Parameter(Mandatory = $true)][string]$SettingsPath,
    [Parameter(Mandatory = $true)][string]$ModsDir,
    [Parameter(Mandatory = $true)][string]$EnabledModIds
)

$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
$enabledIds = $EnabledModIds.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries)
$installedIds = @(Get-ChildItem -LiteralPath $ModsDir -Directory | ForEach-Object {
    Get-ChildItem -LiteralPath $_.FullName -Filter '*.json' -File | ForEach-Object {
        $manifest = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($manifest.id) { [string]$manifest.id }
    }
})

# TestTheSpire 0.1.7 guesses IDs from filenames; mod_manifest.json is not a mod ID.
# Read the actual manifests so optional mods stay disabled in the base suite.
$settings.mod_settings.mod_list = @(
    @($enabledIds + $installedIds) | Sort-Object -Unique | ForEach-Object {
        [ordered]@{ id = $_; source = 'mods_directory'; is_enabled = ($_ -cin $enabledIds) }
    }
)
$json = $settings | ConvertTo-Json -Depth 30
[System.IO.File]::WriteAllText($SettingsPath, $json, [System.Text.UTF8Encoding]::new($false))
