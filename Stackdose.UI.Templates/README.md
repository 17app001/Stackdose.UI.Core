# Stackdose.UI.Templates

Reusable WPF shell/template components for Stackdose applications.

## What this project provides

- `Controls/AppHeader`
- `Controls/LeftNavigation`
- `Controls/AppBottomBar`
- `Pages/BasePage`
- `Shell/MainContainer`

These components provide shared page layout and shell interaction patterns.

## Dependency

`Stackdose.UI.Templates` depends on `Stackdose.UI.Core` for:

- Theme resources
- Shared controls (for example, log/user management controls)
- Security/compliance context integration

## Resource model

`Resources/CommonColors.xaml` maps legacy template brush keys to `Stackdose.UI.Core` semantic/theme tokens.

This keeps compatibility for older XAML while aligning visual output with Core theme updates.

## Usage

1. Add project reference.
2. Merge `CommonColors.xaml` in app resources.
3. Host content using `BasePage` or `MainContainer`.

## Design rule

- Keep templates presentation-focused.
- Keep business logic in app/domain layers.
