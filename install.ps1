param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$GodotExe = ""
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($GodotExe)) {
    $GodotExe = Join-Path $ProjectRoot "tools\godot451\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"
}
$BuildDir = Join-Path $ProjectRoot ".builds"
$ModId = "sunqian-universe"
$ModDir = Join-Path $GameDir "mods\$ModId"

Write-Host "Building $ModId..." -ForegroundColor Cyan
dotnet build (Join-Path $ProjectRoot "$ModId.csproj") -c Debug

if (-not (Test-Path $GodotExe)) {
    throw "Godot not found at: $GodotExe"
}

Write-Host "Exporting PCK..." -ForegroundColor Cyan
$pckPath = Join-Path $BuildDir "$ModId.pck"
if (Test-Path $pckPath) {
    Remove-Item $pckPath -Force
}

$godotArgs = @(
    "--headless",
    "--path", $ProjectRoot,
    "--export-pack", $ModId,
    $pckPath
)
$godotProcess = Start-Process -FilePath $GodotExe -ArgumentList $godotArgs -PassThru -NoNewWindow
$deadline = (Get-Date).AddMinutes(2)
while (-not (Test-Path $pckPath)) {
    if ($godotProcess.HasExited -and $godotProcess.ExitCode -ne 0) {
        throw "Godot export failed with exit code $($godotProcess.ExitCode)"
    }
    if ((Get-Date) -gt $deadline) {
        throw "Timed out waiting for PCK export: $pckPath"
    }
    Start-Sleep -Milliseconds 250
}
if (-not $godotProcess.HasExited) {
    $godotProcess.WaitForExit()
}
if ($godotProcess.ExitCode -ne 0) {
    Write-Warning "Godot exited with code $($godotProcess.ExitCode), but PCK was created."
}

$files = @(
    (Join-Path $ProjectRoot "$ModId.json"),
    (Join-Path $BuildDir "$ModId.dll"),
    (Join-Path $BuildDir "$ModId.pck")
)

foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        throw "Missing build artifact: $file"
    }
}

New-Item -ItemType Directory -Force -Path $ModDir | Out-Null

Write-Host "Installing to $ModDir..." -ForegroundColor Cyan
Copy-Item -Path $files -Destination $ModDir -Force

Write-Host "Done. Installed mod files:" -ForegroundColor Green
Get-ChildItem $ModDir | Format-Table Name, Length -AutoSize
