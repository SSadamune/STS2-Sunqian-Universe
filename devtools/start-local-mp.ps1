param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [ValidateSet("Both", "Host", "Join")]
    [string]$Mode = "Both",
    [string]$HostMode = "host_standard",
    [int]$ClientId = 1000,
    [int]$JoinDelaySeconds = 5,
    [int]$WindowWidth = 0,
    [int]$WindowHeight = 0
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $GameDir "SlayTheSpire2.exe"
if (-not (Test-Path $exe)) {
    throw "Game not found: $exe"
}

$appIdPath = Join-Path $GameDir "steam_appid.txt"
if (-not (Test-Path $appIdPath)) {
    Set-Content -Path $appIdPath -Value "2868840" -Encoding ascii
    Write-Host "Created steam_appid.txt (2868840)" -ForegroundColor Cyan
}

Add-Type -AssemblyName System.Windows.Forms
$area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$topChrome = 140
$sideMargin = 32
$hostX = $area.Left + $sideMargin
$hostY = $area.Top + $topChrome
$joinX = $hostX + 80
$joinY = $hostY

if ($WindowWidth -le 0) {
    $WindowWidth = [Math]::Max(960, [Math]::Min(1280, $area.Width - $sideMargin * 2))
}
if ($WindowHeight -le 0) {
    $WindowHeight = [Math]::Max(540, $area.Height - $topChrome - 48)
}

function Start-Sts2Instance {
    param(
        [int]$X,
        [int]$Y,
        [string[]]$GameArgs
    )

    $argLine = @(
        "--windowed",
        "--resolution",
        "$WindowWidth`x$WindowHeight",
        "--position",
        "$X,$Y"
    ) + $GameArgs

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $exe
    $startInfo.WorkingDirectory = $GameDir
    $startInfo.UseShellExecute = $true
    $startInfo.Arguments = [string]::Join(" ", $argLine)

    Write-Host "Launching: SlayTheSpire2.exe $($startInfo.Arguments)" -ForegroundColor Cyan
    [void][System.Diagnostics.Process]::Start($startInfo)
}

if (-not (Get-Process -Name steam -ErrorAction SilentlyContinue)) {
    Write-Warning "Steam does not appear to be running. Start Steam before local multiplayer if join fails."
}

if ($Mode -in @("Both", "Host")) {
    Start-Sts2Instance -X $hostX -Y $hostY -GameArgs @("-fastmp", $HostMode)
}

if ($Mode -in @("Both", "Join")) {
    if ($Mode -eq "Both") {
        Write-Host "Waiting $JoinDelaySeconds s for host..." -ForegroundColor DarkGray
        Start-Sleep -Seconds $JoinDelaySeconds
    }
    Start-Sts2Instance -X $joinX -Y $joinY -GameArgs @("-fastmp", "join", "-clientId", "$ClientId")
}

Write-Host "Done." -ForegroundColor Green
