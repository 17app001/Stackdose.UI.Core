# Shell App Quickstart (3 Steps)

Use this when you need a new machine-type app quickly, without deep framework knowledge.

## 1) Generate a new app

```powershell
powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.YourMachine" -DestinationRoot . -IncludeSecondDemoSampleConfigs
```

This creates a runnable WPF app with `Config/app-meta.json` and optional sample machine configs.

## 2) Edit config only (start simple)

Update files under your new app's `Config/` folder:

- `app-meta.json`
- `MachineA.config.json` / `MachineB.config.json` (if included)
- alarm/sensor json files

To change detail-page bindings without C# edits, set `detailPage` in `app-meta.json`.

Minimum machine config keys (required for JSON-only flow):

- `machine.id`
- `machine.name`
- `machine.enable`
- `alarmConfigFile`
- `sensorConfigFile`
- `plc.ip` / `plc.port` / `plc.pollIntervalMs`
- `tags.status.isRunning` / `tags.status.isAlarm`
- `tags.process.batchNo` / `tags.process.recipeNo` / `tags.process.nozzleTemp`

At this stage, do not change adapter code unless your machine needs custom mapping behavior.

## 3) Build and run

```powershell
dotnet build .\Stackdose.App.YourMachine\Stackdose.App.YourMachine.csproj -c Debug
```

Then open/run the app and confirm:

- menu/title come from your config
- machine cards and monitor values load
- no unexpected PLC poll-time spikes

For deeper integration details, see `Stackdose.UI.Core/Shell/SECOND_APP_QUICKSTART.md`.
