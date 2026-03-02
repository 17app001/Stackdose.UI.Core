# Second App Quickstart

Goal: create a new shell-based app with minimal code by reusing shared shell contracts.

If you are new to this repo, start with `QUICKSTART.md` at repo root first.

For first runnable version, use JSON-only changes:

- `Config/app-meta.json`
- `Config/Machine*.config.json` with `alarmConfigFile` and `sensorConfigFile`
- referenced alarm/sensor json files
- use `detailPage` section in `app-meta.json` to change detail tag mapping without code edits

## 1) Prepare profile model

- Implement `IShellAppProfile` in your app metadata model.
- Provide `AppId`, `HeaderDeviceName`, `DefaultPageTitle`, `UseFrameworkShellServices`, `EnableMetaHotReload`, and `NavigationItems`.

## 2) Implement app adapters

- Implement a meta runtime service that satisfies:
  - `IShellMetaRuntimeService<TMeta, TSnapshot>`
  - `TSnapshot : IShellMetaSnapshot`
- Implement a bootstrap service that satisfies:
  - `IShellBootstrapService<TShellHost, TMetaRuntimeService, TNavigationService, TBootstrapState, TDevicePages>`

## 3) Use shared shell core

- Use `ShellNavigationService` as the default navigation engine.
- Use `ShellNavigationTargets` and `ShellRouteCatalog` for route keys and supported target checks.
- Keep app-specific runtime logic in your app adapter only.

## 4) Wire MainWindow

- Keep MainWindow as coordinator only:
  - create commands
  - call bootstrap `Start(...)` on loaded
  - call bootstrap `Stop(...)` on unloaded
  - apply snapshot to UI

## Minimal checklist before first run

- `dotnet build Stackdose.UI.Core/Stackdose.UI.Core.csproj`
- `dotnet build Stackdose.UI.Templates/Stackdose.UI.Templates.csproj`
- `dotnet test Stackdose.UI.Templates.Tests/Stackdose.UI.Templates.Tests.csproj`
