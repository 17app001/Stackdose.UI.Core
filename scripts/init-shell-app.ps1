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
} else {
    @"
{
  "machine": {
    "id": "M1",
    "name": "Machine 01",
    "enable": true
  },
  "alarmConfigFile": "Config/Machine1.alarms.json",
  "sensorConfigFile": "Config/Machine1.sensors.json",
  "plc": {
    "ip": "127.0.0.1",
    "port": 5000,
    "pollIntervalMs": 150,
    "autoConnect": true
  },
  "tags": {
    "status": {
      "isRunning": { "address": "M201", "type": "bool", "access": "read" },
      "isAlarm": { "address": "M202", "type": "bool", "access": "read" }
    },
    "process": {
      "batchNo": { "address": "D400", "type": "string", "access": "read", "length": 8 },
      "recipeNo": { "address": "D410", "type": "string", "access": "read", "length": 8 },
      "nozzleTemp": { "address": "D420", "type": "int16", "access": "read" }
    }
  }
}
"@ | Set-Content -Path (Join-Path $configDir "Machine1.config.json") -Encoding UTF8

    @"
{
  "Alarms": [
    { "Device": "M202", "Bit": 0, "Label": "General Alarm" }
  ]
}
"@ | Set-Content -Path (Join-Path $configDir "Machine1.alarms.json") -Encoding UTF8

    @"
[
  { "Device": "D420", "Name": "Nozzle Temp" }
]
"@ | Set-Content -Path (Join-Path $configDir "Machine1.sensors.json") -Encoding UTF8
}

$readmePath = Join-Path $projectDir "SHELL_QUICKSTART.md"
@"
# Shell Quickstart

1. Configure your app in `Config/app-meta.json`.
2. Update machine/alarm/sensor json files under `Config/`.
   - required keys in machine config: alarmConfigFile, sensorConfigFile
3. Build and run your project.

Reference:
- Repo root `QUICKSTART.md` (recommended)
- `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md` (advanced wiring details)
"@ | Set-Content -Path $readmePath -Encoding UTF8

Write-Host "[init-shell-app] Done. Generated: $projectDir"
