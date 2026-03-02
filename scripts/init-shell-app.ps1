param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [string]$DestinationRoot = ".",

    [switch]$IncludeSecondDemoSampleConfigs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($AppName.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
    throw "AppName contains invalid file name characters: $AppName"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI not found in PATH. Install .NET SDK before running this script."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if (-not (Test-Path $DestinationRoot)) {
    New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null
}

$destinationRootFull = (Resolve-Path $DestinationRoot).Path
$projectDir = Join-Path $destinationRootFull $AppName

if (Test-Path $projectDir) {
    throw "Target project directory already exists: $projectDir"
}

Write-Host "[init-shell-app] Creating WPF project: $AppName"
dotnet new wpf -n $AppName -o $projectDir

$configDir = Join-Path $projectDir "Config"
New-Item -ItemType Directory -Path $configDir | Out-Null

$appMetaTemplate = Join-Path $repoRoot "Stackdose.UI.Core\Shell\app-meta.template.json"
$appMetaTarget = Join-Path $configDir "app-meta.json"
if (-not (Test-Path $appMetaTemplate)) {
    throw "App meta template not found: $appMetaTemplate"
}

Copy-Item $appMetaTemplate $appMetaTarget -Force

if ($IncludeSecondDemoSampleConfigs) {
    $sampleConfigDir = Join-Path $repoRoot "Stackdose.App.SecondDemo\Config"
    if (Test-Path $sampleConfigDir) {
        Copy-Item (Join-Path $sampleConfigDir "MachineA.config.json") (Join-Path $configDir "MachineA.config.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.config.json") (Join-Path $configDir "MachineB.config.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineA.alarms.json") (Join-Path $configDir "MachineA.alarms.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.alarms.json") (Join-Path $configDir "MachineB.alarms.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineA.sensors.json") (Join-Path $configDir "MachineA.sensors.json") -Force
        Copy-Item (Join-Path $sampleConfigDir "MachineB.sensors.json") (Join-Path $configDir "MachineB.sensors.json") -Force
    } else {
        Write-Warning "Sample config directory not found, skipping sample config copy: $sampleConfigDir"
    }
}

$readmePath = Join-Path $projectDir "SHELL_QUICKSTART.md"
@"
# Shell Quickstart

1. Configure your app in `Config/app-meta.json`.
2. If needed, update machine/alarm/sensor json files under `Config/`.
3. Build and run your project.

Reference:
- Repo root `QUICKSTART.md` (recommended)
- `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md` (advanced wiring details)
"@ | Set-Content -Path $readmePath -Encoding UTF8

Write-Host "[init-shell-app] Done. Generated: $projectDir"
