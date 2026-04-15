# Controls Visual Token Convergence

## Goal

Reduce hardcoded colors in `Stackdose.UI.Core/Controls/*.xaml` and converge on theme tokens so Dark/Light themes remain consistent and maintainable.

## Current State Snapshot

Based on current XAML scan (`#[0-9A-Fa-f]{3,8}`), remaining literals are concentrated in:

- `Controls/LoginDialog.xaml`
- `Controls/PrintHeadStatus.xaml`
- `Controls/PrintHeadController.xaml`
- `Controls/PrintHeadPanel.xaml`
- `Controls/LiveLogViewer.xaml`
- `Controls/PlcDeviceEditor.xaml`
- `Controls/PlcStatus.xaml`

Primary literal categories:

- **Overlay alpha colors**: `#CC000000`, `#2A000000`, `#22000000`, `#26000000`, `#2B000000`
- **Neutral grays**: `#424242`, `#757575`, `#808080`, `#9E9E9E`, `#E0E0E0`
- **Brand/action accents**: `#00BCD4`, `#00A868`, `#007ACC`, `#FFA726`, `#FF6F00`
- **PrintHead-specific accents**: `#ff4757`, `#00d4ff`, `#ffd700`, `#00ff7f`, `#ff69b4`

## Convergence Rules

1. Prefer existing tokens in `Themes/Colors.xaml` and `Themes/LightColors.xaml` first.
2. Add new tokens only when semantics are missing (e.g. overlay chip background, PrintHead telemetry accents).
3. Keep per-control visual identity, but bind identity through tokens, not literals.
4. Avoid one-off hex values inside control templates.

## Recommended Token Additions

Add these token groups to both dark/light theme dictionaries:

- **Overlay**
  - `Overlay.Bg.Scrim`
  - `Overlay.Bg.Strong`
  - `Overlay.Bg.Medium`
  - `Overlay.Bg.Light`
- **PrintHead**
  - `PrintHead.Accent.Temperature`
  - `PrintHead.Accent.Voltage`
  - `PrintHead.Accent.Encoder`
  - `PrintHead.Accent.PrintIndex`
  - `PrintHead.State.Active`
  - `PrintHead.State.Idle`

## Replacement Plan

### Phase 1 (Quick Wins)

- Replace overlay literals in:
  - `Controls/LiveLogViewer.xaml`
  - `Controls/PlcStatus.xaml`
  - `Controls/PlcDeviceEditor.xaml`
- Replace action/neutral literals in:
  - `Controls/PrintHeadPanel.xaml`

### Phase 2 (Login Dialog)

- Replace `LoginDialog.xaml` literals with tokenized panel/input/error variants.
- Keep current look, only remove hardcoded hex values.

### Phase 3 (PrintHead Controls)

- Tokenize telemetry/status colors in:
  - `Controls/PrintHeadStatus.xaml`
  - `Controls/PrintHeadController.xaml`
- Keep semantic emphasis (active/error/warning) through `Status.*` + `PrintHead.*` tokens.

## Verification Checklist

- No new hardcoded `#` colors introduced in modified controls.
- Dark/Light themes both define all newly introduced keys.
- Existing event hookups, x:Name targets, and bindings unchanged.
- Regression tests: `Stackdose.UI.Core.Tests` remain green.
