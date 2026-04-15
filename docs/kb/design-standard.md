# Core UI Design Standard (v1)

This standard defines the baseline token and style rules for `Stackdose.UI.Core` controls.

## Scope

- Applies to `Stackdose.UI.Core/Controls/*` and `Stackdose.UI.Core/Themes/*`.
- `Stackdose.UI.Templates` should consume these tokens instead of introducing parallel color semantics.

## Token Layers

### 1) Legacy Domain Tokens (compatible)

- `Cyber.*`
- `Plc.*`
- `Sensor.*`
- `Log.*`
- `Button.*`
- `Status.*`
- `PrintHead.*`

These stay for backward compatibility.

### 2) Semantic Alias Tokens (preferred for new work)

- Surface: `Surface.Bg.Page`, `Surface.Bg.Panel`, `Surface.Bg.Card`, `Surface.Bg.Control`
- Border: `Surface.Border.Default`, `Surface.Border.Strong`
- Text: `Text.Primary`, `Text.Secondary`, `Text.Tertiary`
- Accent/Action: `Accent.Primary`, `Action.Primary`, `Action.Success`, `Action.Warning`, `Action.Error`, `Action.Info`

New controls should use semantic aliases first unless a domain-specific token is required.

## Naming Rules

1. Use `Domain.Category.State` naming format.
2. Avoid hardcoded hex values in control XAML.
3. If a color is reused by 2+ controls, define a token.
4. Keep dark/light dictionaries in sync for every key.

## Style Rules

1. Interactive controls must have hover/pressed/disabled states.
2. Inputs must include a focus border state.
3. Empty states should be explicit for list/data views.
4. Typography:
   - labels: 11-13
   - values: 12-16
   - headings: 16+

## Migration Guidance (Core -> Templates)

1. Templates should reference Core semantic tokens.
2. Move shared button/input style patterns to Core theme components.
3. Keep template-specific branding in Templates, but map colors through Core tokens.

## Verification Checklist

- No hardcoded hex in changed `Controls/*.xaml`.
- Theme token key exists in both `Colors.xaml` and `LightColors.xaml`.
- Existing control behavior and bindings remain unchanged.
- Regression tests pass (`Stackdose.UI.Core.Tests`).
