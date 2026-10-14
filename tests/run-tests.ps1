[CmdletBinding()]
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$RitsuLibDir = "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747602295",
    [string]$GodotExe = "",
    [string]$Filter = "",
    [int]$TimeoutSeconds = 180,
    [switch]$ListOnly,
    [switch]$SkipModInstall,
    [switch]$JointModTests
)

$ErrorActionPreference = "Stop"
$TestsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $TestsDir
$TestProject = Join-Path $TestsDir "SunqianUniverse.Tests\SunqianUniverse.Tests.csproj"
$StagingDir = Join-Path $TestsDir "SunqianUniverse.Tests\obj\Sts2Payload"
$TestUserDataDir = Join-Path $TestsDir "SunqianUniverse.Tests\obj\Sts2TestData"
$GameDir = [System.IO.Path]::GetFullPath($GameDir)
$RitsuLibDir = [System.IO.Path]::GetFullPath($RitsuLibDir)
$GameModsDir = Join-Path $GameDir "mods"
$RitsuLibTargetDir = Join-Path $GameModsDir "STS2-RitsuLib"

if (-not (Test-Path -LiteralPath (Join-Path $GameDir "SlayTheSpire2.exe") -PathType Leaf)) {
    throw "SlayTheSpire2.exe was not found under: $GameDir"
}
if (-not (Test-Path -LiteralPath $TestProject -PathType Leaf)) {
    throw "Test project was not found: $TestProject"
}
if ($TimeoutSeconds -le 0) {
    throw "TimeoutSeconds must be greater than zero."
}

$RitsuManifestPath = Join-Path $RitsuLibDir "mod_manifest.json"
if (-not (Test-Path -LiteralPath $RitsuManifestPath -PathType Leaf)) {
    throw "RitsuLib manifest was not found: $RitsuManifestPath"
}
$RitsuManifest = Get-Content -LiteralPath $RitsuManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($RitsuManifest.id -ne "STS2-RitsuLib") {
    throw "Unexpected RitsuLib mod id '$($RitsuManifest.id)' in $RitsuManifestPath"
}

Write-Host "Syncing STS2-RitsuLib $($RitsuManifest.version) from Workshop..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $RitsuLibTargetDir | Out-Null
Get-ChildItem -LiteralPath $RitsuLibDir -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $RitsuLibTargetDir -Recurse -Force
}

if (-not $SkipModInstall) {
    Write-Host "Building and installing sunqian-universe..." -ForegroundColor Cyan
    if ([string]::IsNullOrWhiteSpace($GodotExe)) {
        & (Join-Path $ProjectRoot "install.ps1") -GameDir $GameDir
    }
    else {
        & (Join-Path $ProjectRoot "install.ps1") -GameDir $GameDir -GodotExe $GodotExe
    }
}

# TestTheSpire recreates its payload directories. Removing only its dedicated
# staging root first also prevents stale dependency assemblies from surviving.
if (Test-Path -LiteralPath $StagingDir) {
    Remove-Item -LiteralPath $StagingDir -Recurse -Force
}

$Target = if ($ListOnly) { "ListSts2Tests" } else { "RunSts2Tests" }
$MsBuildArgs = @(
    "msbuild",
    $TestProject,
    "-restore",
    "-t:$Target",
    "-p:Configuration=Debug",
    "-p:Sts2Path=$GameDir"
)
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $MsBuildArgs += "-p:Sts2TestArgs=--sts2-test-filter=$Filter"
}
if ($JointModTests) {
    $MsBuildArgs += "-p:Sts2JointModTests=true"
}

Write-Host "$Target via TestTheSpire..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $TestUserDataDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $TestUserDataDir "Local") | Out-Null

# TestTheSpire 0.1.7 sets XDG_DATA_HOME, but STS2 0.111.0 on Windows reads
# APPDATA instead. Passing all three variables to the child process keeps the
# generated settings and test saves isolated from the player's normal profile.
$StartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$StartInfo.FileName = "dotnet"
$StartInfo.UseShellExecute = $false
$StartInfo.CreateNoWindow = $true
$StartInfo.RedirectStandardOutput = $true
$StartInfo.RedirectStandardError = $true
$StartInfo.Arguments = ($MsBuildArgs | ForEach-Object {
    if ($_ -match '[\s"]') {
        '"' + ($_ -replace '"', '\"') + '"'
    }
    else {
        $_
    }
}) -join ' '
$StartInfo.EnvironmentVariables["APPDATA"] = $TestUserDataDir
$StartInfo.EnvironmentVariables["LOCALAPPDATA"] = Join-Path $TestUserDataDir "Local"
$StartInfo.EnvironmentVariables["XDG_DATA_HOME"] = $TestUserDataDir

$DotnetProcess = [System.Diagnostics.Process]::new()
$DotnetProcess.StartInfo = $StartInfo
try {
    if (-not $DotnetProcess.Start()) {
        throw "Failed to start dotnet for $Target."
    }
    $StandardOutputTask = $DotnetProcess.StandardOutput.ReadToEndAsync()
    $StandardErrorTask = $DotnetProcess.StandardError.ReadToEndAsync()
    $Deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while (-not $DotnetProcess.HasExited) {
        if ((Get-Date) -gt $Deadline) {
            & taskkill.exe /PID $DotnetProcess.Id /T /F | Out-Null
            $LogName = if ($ListOnly) { "list.log" } else { "run.log" }
            $LogPath = Join-Path $TestUserDataDir "logs\$LogName"
            $DotnetProcess.WaitForExit()
            Write-Host $StandardOutputTask.Result
            Write-Host $StandardErrorTask.Result -ForegroundColor DarkYellow
            throw "$Target timed out after $TimeoutSeconds seconds. Full log: $LogPath"
        }
        Start-Sleep -Milliseconds 250
    }

    # Populate ExitCode reliably after asynchronous Start-Process completion.
    $DotnetProcess.WaitForExit()
    $DotnetProcess.Refresh()
    $DotnetExitCode = $DotnetProcess.ExitCode
    if ($DotnetExitCode -ne 0) {
        Write-Host $StandardOutputTask.Result
        Write-Host $StandardErrorTask.Result -ForegroundColor DarkYellow
        throw "$Target failed with exit code $DotnetExitCode"
    }

    $LogName = if ($ListOnly) { "list.log" } else { "run.log" }
    $LogPath = Join-Path $TestUserDataDir "logs\$LogName"
    Write-Host "Full STS2 log: $LogPath" -ForegroundColor DarkGray
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        Select-String -LiteralPath $LogPath `
            -Pattern '\[sunqian-universe\.Tests\] (START|PASSED|FAILED|SKIP|SUMMARY)' |
            ForEach-Object { Write-Host $_.Line }
    }
}
finally {
    $DotnetProcess.Dispose()
}
