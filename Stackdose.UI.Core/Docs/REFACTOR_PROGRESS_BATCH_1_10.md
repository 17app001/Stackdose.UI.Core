# Refactor Progress (Batch 1-10)

This document records the completed `Stackdose.UI.Core` refactor batches executed in this session.

## Batch 1-2: Controls Baseline + SecuredButton Stabilization

- Normalized major user-facing text in key controls.
- Refactored `SecuredButton` lifecycle and permission update flow.
- Preserved existing dependency properties and click behavior.

## Batch 3: LoginDialog Flow Split

- Split initialization and event wiring from constructor.
- Isolated authentication path into `AuthenticateAsync`, `ValidateCredentials`, `HandleLoginSuccess`, and `HandleLoginFailure`.
- Kept dialog API and behavior unchanged.

## Batch 4: LiveLogViewer Hardening

- Ensured collection unsubscription on unload.
- Replaced full ItemsSource rebind with view refresh (`CollectionViewSource`).

## Batch 5: UserManagementPanel Decomposition

- Extracted repeated target-user lookup and dialog messaging helpers.
- Extracted current-user-id resolution flow.
- Reduced repeated code in add/edit/reset/toggle handlers.

## Batch 6: PlcStatus Layered Refactor

- Split connection orchestration into dedicated retry/success/failure methods.
- Extracted monitor registration pipeline (`RegisterAutoMonitorAddresses`, source enumeration).
- Replaced silent monitor-registration catches with debug-logged catches.
- Split watchdog loop/reconnect handling into dedicated methods.
- Normalized global-instance cleanup rule via `ShouldKeepGlobalConnection`.

## Batch 7: Shared Control Runtime Helper

- Added `Controls/ControlRuntime.cs` for design-mode check and standard dialogs.

## Batch 8: Helper Adoption

- Applied `ControlRuntime` to `SecuredButton` and `UserManagementPanel`.
- Removed duplicated local dialog helper methods from `UserManagementPanel`.

## Batch 9: Text and Comment Normalization

- Normalized remaining high-visibility comments/messages in touched files, especially `LoginDialog`.

## Batch 10: Verification

- Regression tests executed via `Stackdose.UI.Core.Tests` after each major batch.
- Latest result: `Passed 3, Failed 0`.
