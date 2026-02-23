# UbiDemo DevicePage Registration Idempotence and Immediate PlcLabel Update

## TL;DR

> **Quick Summary**: Remove repeated `DevicePage` registration on every re-entry and restore immediate `PlcLabel` updates using existing `PlcStatus`/`PlcLabel` auto-refresh flow, without `UI.Core` edits.
>
> **Deliverables**:
> - Idempotent registration lifecycle in `Stackdose.App.UbiDemo`
> - Deterministic monitor ownership and teardown/rebind behavior
> - Verified immediate update behavior on `DevicePage` without click/re-entry
>
> **Estimated Effort**: Short
> **Parallel Execution**: YES - 3 waves + final verification
> **Critical Path**: T1 -> T4 -> T6 -> T8 -> F1/F3

---

## Context

### Original Request
User asked to first remove the issue where `DevicePage` registers again every time the page is entered.

### Interview Summary
**Key Discussions**:
- Scope is `Stackdose.App.UbiDemo` only.
- No `UI.Core` modification in this pass.
- No page-click refresh workaround, no manual refresh hack.

**Research Findings**:
- Symptom indicates lifecycle/registration timing mismatch more than missing control.
- Existing test infra exists in repo, but no dedicated `UbiDemo` test project.

### Metis Review
**Identified Gaps (addressed)**:
- Need explicit idempotence criteria (no duplicate registrations/subscriptions after repeated navigation).
- Need explicit lifecycle ownership (start/stop/dispose responsibility).
- Need edge-case checks (rapid nav, device switch, reconnect).

---

## Work Objectives

### Core Objective
Ensure `UbiDevicePage` registration/monitor lifecycle is idempotent so `PlcLabel` values update immediately after writes, without requiring page re-click/re-entry.

### Concrete Deliverables
- Lifecycle fix in `Stackdose.App.UbiDemo` page/runtime wiring.
- Registration deduplication guard with deterministic detach/attach behavior.
- Verification evidence for immediate update and no duplicate monitor growth.

### Definition of Done
- [ ] Re-enter `DevicePage` 10 times and monitored registration count does not grow unexpectedly.
- [ ] `M200/M201/M202` write reflects in top `PlcLabel` values without re-clicking page.
- [ ] No changes made under `Stackdose.UI.Core`.

### Must Have
- Idempotent registration path.
- Immediate visible updates on active `DevicePage`.
- Safe handling on device switch and reconnect scenarios.

### Must NOT Have (Guardrails)
- No edits in `Stackdose.UI.Core`.
- No artificial page refresh trigger.
- No App.Demo behavior regression.

---

## Verification Strategy (MANDATORY)

> **ZERO HUMAN INTERVENTION** - all verification scenarios are executable by agent.

### Test Decision
- **Infrastructure exists**: YES (repo-level xUnit projects)
- **Automated tests**: Tests-after (default applied)
- **Framework**: `dotnet test` (existing projects)
- **UbiDemo-specific automated tests**: None currently; primary verification via agent-executed runtime scenarios.

### QA Policy
Evidence output path: `.sisyphus/evidence/`

- **UI runtime**: Playwright/dev-browser style interaction or equivalent automation for page navigation and value assertions.
- **CLI/build**: `dotnet build`, `dotnet test` for regression safety.
- **API not applicable** for this scope.

---

## Execution Strategy

### Parallel Execution Waves

Wave 1 (Start immediately - discovery and instrumentation boundaries):
- T1: Map actual registration lifecycle entry points and ownership in UbiDemo.
- T2: Map navigation caching/recreation behavior for `UbiDevicePage`.
- T3: Map monitor identity keys and cross-page registration overlap (DevicePage vs Settings/Maintenance).

Wave 2 (After Wave 1 - fix design + implementation, parallel where independent):
- T4: Implement idempotent registration guard in DevicePage lifecycle path.
- T5: Normalize device switch detach/attach path to prevent stale/duplicate subscriptions.
- T6: Align initial value propagation timing with active page binding lifecycle (UbiDemo only).

Wave 3 (After Wave 2 - verification and regression hardening):
- T7: Add diagnostic assertions/log markers (UbiDemo-scoped) and verify no growth after repeated navigation.
- T8: Execute runtime scenarios for M200/M201/M202 immediate update + reconnect/rapid-nav edge cases.

Wave FINAL (After all implementation tasks - independent parallel reviews):
- F1: Plan compliance audit.
- F2: Code quality/build/test review.
- F3: Full QA scenario replay with evidence.
- F4: Scope fidelity check (no UI.Core touches, no scope creep).

### Dependency Matrix (full)

- **T1**: Blocked By: None | Blocks: T4, T6
- **T2**: Blocked By: None | Blocks: T4, T8
- **T3**: Blocked By: None | Blocks: T5, T7
- **T4**: Blocked By: T1, T2 | Blocks: T8
- **T5**: Blocked By: T3 | Blocks: T8
- **T6**: Blocked By: T1 | Blocks: T8
- **T7**: Blocked By: T3, T4 | Blocks: F1, F3
- **T8**: Blocked By: T4, T5, T6 | Blocks: F1, F2, F3, F4
- **F1**: Blocked By: T7, T8 | Blocks: completion
- **F2**: Blocked By: T8 | Blocks: completion
- **F3**: Blocked By: T7, T8 | Blocks: completion
- **F4**: Blocked By: T8 | Blocks: completion

### Agent Dispatch Summary

- Wave 1: T1/T2/T3 -> `quick` (analysis and mapping)
- Wave 2: T4/T5 -> `unspecified-high`, T6 -> `quick`
- Wave 3: T7/T8 -> `unspecified-high`
- Final: F1 -> `oracle`, F2/F3 -> `unspecified-high`, F4 -> `deep`

---

## TODOs

- [ ] 1. Map DevicePage registration lifecycle entry points

  **What to do**:
  - Trace all `UbiDevicePage` registration triggers (constructor, loaded, navigated, selection changed).
  - Identify exact monitor registration call chain and ownership object.
  - Document duplicate path candidates and safe single-entry target.

  **Must NOT do**:
  - Do not modify `Stackdose.UI.Core`.
  - Do not add temporary refresh calls.

  **Recommended Agent Profile**:
  - **Category**: `quick` (focused code-path mapping)
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `playwright`: not needed for static lifecycle trace.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T2, T3)
  - **Blocks**: T4, T6
  - **Blocked By**: None

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs` - Primary lifecycle event handlers and monitor registration calls.
  - `Stackdose.App.UbiDemo/MainWindow.xaml.cs` - Navigation flow and page activation entry.
  - `Stackdose.App.UbiDemo/Services/UbiRuntimeMapper.cs` - Runtime mapping and monitor target identity.

  **Acceptance Criteria**:
  - [ ] A single canonical registration entry point is identified.
  - [ ] All duplicate registration paths are listed with trigger conditions.

  **QA Scenarios**:
  ```text
  Scenario: Lifecycle map generated correctly
    Tool: Bash
    Preconditions: Repository in readable state
    Steps:
      1. Inspect target files and list all registration trigger methods.
      2. Build sequence diagram text: navigation -> registration -> label update.
      3. Confirm there is one intended canonical registration entry.
    Expected Result: Duplicate paths and canonical path are explicit.
    Failure Indicators: Missing trigger path or ambiguous ownership.
    Evidence: .sisyphus/evidence/task-1-lifecycle-map.md

  Scenario: Duplicate path edge-case identified
    Tool: Bash
    Preconditions: Same as above
    Steps:
      1. Evaluate rapid re-entry call sequence.
      2. Mark where repeated subscription can happen.
    Expected Result: At least one concrete duplicate-risk point captured.
    Evidence: .sisyphus/evidence/task-1-duplicate-risk.md
  ```

  **Evidence to Capture**:
  - [ ] task-1-lifecycle-map.md
  - [ ] task-1-duplicate-risk.md

  **Commit**: NO

- [ ] 2. Confirm DevicePage caching/recreation behavior

  **What to do**:
  - Verify whether `UbiDevicePage` is recreated or reused on each navigation.
  - Confirm event subscription accumulation risk for each mode.
  - Lock behavior assumptions used by fix.

  **Must NOT do**:
  - Do not change global navigation architecture.

  **Recommended Agent Profile**:
  - **Category**: `quick` (targeted navigation analysis)
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `ultrabrain`: unnecessary for limited scope.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T3)
  - **Blocks**: T4, T8
  - **Blocked By**: None

  **References**:
  - `Stackdose.UI.Templates/Shell/MainContainer.xaml.cs` - Template shell page host behavior.
  - `Stackdose.App.UbiDemo/MainWindow.xaml.cs` - UbiDemo-specific navigation entry.

  **Acceptance Criteria**:
  - [ ] Page instance lifecycle mode (reuse/recreate) is explicitly confirmed.
  - [ ] Risk impact on registration duplication is documented.

  **QA Scenarios**:
  ```text
  Scenario: Navigation lifecycle verified
    Tool: Bash
    Preconditions: Buildable solution
    Steps:
      1. Navigate Overview -> DevicePage -> Overview -> DevicePage in automation.
      2. Record page instance identity markers/logs.
      3. Compare whether instance is reused or recreated.
    Expected Result: One definitive lifecycle mode captured.
    Failure Indicators: Contradictory lifecycle evidence.
    Evidence: .sisyphus/evidence/task-2-navigation-lifecycle.md

  Scenario: Rapid nav edge-case characterization
    Tool: Bash
    Preconditions: Same route available
    Steps:
      1. Perform 10 fast nav loops.
      2. Record whether registration callback count grows.
    Expected Result: Growth pattern baseline captured for pre-fix state.
    Evidence: .sisyphus/evidence/task-2-rapid-nav-baseline.md
  ```

  **Evidence to Capture**:
  - [ ] task-2-navigation-lifecycle.md
  - [ ] task-2-rapid-nav-baseline.md

  **Commit**: NO

- [ ] 3. Map monitor identity and cross-page overlap

  **What to do**:
  - Identify monitor identity key (device id/address tuple).
  - Verify overlap between `DevicePage` and `SettingsPage` monitored-device paths.
  - Define dedup rule for same device identity.

  **Must NOT do**:
  - Do not introduce new global monitor service architecture.

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `writing`: not doc-centric task.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 1 (with T1, T2)
  - **Blocks**: T5, T7
  - **Blocked By**: None

  **References**:
  - `Stackdose.App.UbiDemo/Pages/SettingsPage.xaml.cs` - monitored device surfaces in maintenance.
  - `Stackdose.App.UbiDemo/ViewModels/SettingsPageViewModel.cs` - monitored list data source handling.
  - `Stackdose.App.UbiDemo/Services/UbiRuntimeLoader.cs` - device config/runtime hydration path.

  **Acceptance Criteria**:
  - [ ] Identity key is formally defined.
  - [ ] Same-device dedup rule across pages is documented.

  **QA Scenarios**:
  ```text
  Scenario: Identity key consistency
    Tool: Bash
    Preconditions: MachineA and MachineB config available
    Steps:
      1. Inspect runtime mapping for both pages.
      2. Compare key construction logic.
      3. Validate same machine yields same key in both contexts.
    Expected Result: Shared key schema and dedup rule confirmed.
    Failure Indicators: Different keys for same monitored device.
    Evidence: .sisyphus/evidence/task-3-identity-key.md

  Scenario: Multi-page overlap edge-case
    Tool: Bash
    Preconditions: DevicePage and Maintenance both reachable
    Steps:
      1. Activate monitoring from both surfaces.
      2. Check duplicate registration count signal.
    Expected Result: Overlap risk documented with exact trigger.
    Evidence: .sisyphus/evidence/task-3-overlap-risk.md
  ```

  **Evidence to Capture**:
  - [ ] task-3-identity-key.md
  - [ ] task-3-overlap-risk.md

  **Commit**: NO

- [ ] 4. Implement idempotent registration in DevicePage lifecycle

  **What to do**:
  - Implement UbiDemo-side guard so repeated page entry does not re-register the same monitor/subscription.
  - Ensure existing registration is reused or cleanly replaced per chosen ownership rule.
  - Keep label update source tied to existing `PlcStatus`/binding pipeline.

  **Must NOT do**:
  - No page-click refresh hooks.
  - No `UI.Core` edits.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `frontend-ui-ux`: visual redesign not in scope.

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 2 (core)
  - **Blocks**: T8
  - **Blocked By**: T1, T2

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs` - exact registration lifecycle implementation target.
  - `Stackdose.App.UbiDemo/Services/UbiRuntimeMapper.cs` - source of monitored addresses and runtime context.

  **Acceptance Criteria**:
  - [ ] Re-entering `DevicePage` does not increase registration count for same device.
  - [ ] Registration behavior remains correct when opening different machines.

  **QA Scenarios**:
  ```text
  Scenario: Happy path idempotent re-entry
    Tool: Bash
    Preconditions: DevicePage accessible for MachineA
    Steps:
      1. Enter DevicePage, capture registration count snapshot.
      2. Navigate away and back 10 times.
      3. Capture count again and compare.
    Expected Result: Count remains stable for same device identity.
    Failure Indicators: Count increases with each re-entry.
    Evidence: .sisyphus/evidence/task-4-idempotent-reentry.md

  Scenario: Edge case different machine switch
    Tool: Bash
    Preconditions: MachineA and MachineB enabled
    Steps:
      1. Enter MachineA DevicePage then switch to MachineB DevicePage.
      2. Verify previous machine registration detaches or is not duplicated.
    Expected Result: No stale duplicate for previous machine.
    Evidence: .sisyphus/evidence/task-4-machine-switch.md
  ```

  **Evidence to Capture**:
  - [ ] task-4-idempotent-reentry.md
  - [ ] task-4-machine-switch.md

  **Commit**: NO

- [ ] 5. Normalize detach/attach behavior on active device change

  **What to do**:
  - Ensure device change path removes stale callbacks/subscriptions before attaching new device monitor.
  - Guarantee one active subscription set per active device context.

  **Must NOT do**:
  - Do not alter business scope outside monitor lifecycle.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `git-master`: not a git workflow task.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T6)
  - **Blocks**: T8
  - **Blocked By**: T3

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs` - active device switching handling.
  - `Stackdose.App.UbiDemo/Services/UbiRuntimeLoader.cs` - machine runtime source during switch.

  **Acceptance Criteria**:
  - [ ] Switching devices does not leave stale monitor callbacks.
  - [ ] Only one active monitor path remains per selected device.

  **QA Scenarios**:
  ```text
  Scenario: Happy path device switch
    Tool: Bash
    Preconditions: At least MachineA and MachineB enabled
    Steps:
      1. Open MachineA DevicePage and capture active registration map.
      2. Switch to MachineB.
      3. Capture registration map and verify MachineA stale entries are removed or inactive.
    Expected Result: Active map matches selected machine only.
    Failure Indicators: Stale MachineA entries still active after switch.
    Evidence: .sisyphus/evidence/task-5-device-switch-cleanup.md

  Scenario: Edge case rapid multi-switch
    Tool: Bash
    Preconditions: Same as above
    Steps:
      1. Alternate A/B selection rapidly for 20 actions.
      2. Validate registration map does not accumulate duplicates.
    Expected Result: Stable active registration cardinality.
    Evidence: .sisyphus/evidence/task-5-rapid-switch.md
  ```

  **Evidence to Capture**:
  - [ ] task-5-device-switch-cleanup.md
  - [ ] task-5-rapid-switch.md

  **Commit**: NO

- [ ] 6. Align first-update timing to visible DevicePage state

  **What to do**:
  - Ensure initial monitor value propagation reaches currently visible labels on first active cycle.
  - Fix ordering issues between registration, binding readiness, and first callback in UbiDemo wiring.

  **Must NOT do**:
  - Do not add manual refresh timers.
  - Do not require user click interaction.

  **Recommended Agent Profile**:
  - **Category**: `quick`
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `playwright`: runtime automation happens in T8, not implementation.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 2 (with T5)
  - **Blocks**: T8
  - **Blocked By**: T1

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml` - top labels and bound addresses.
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs` - timing of registration relative to page visibility.

  **Acceptance Criteria**:
  - [ ] First visible update appears without re-click after entering DevicePage.
  - [ ] M200/M201/M202 reflected in corresponding top labels within one monitor cycle.

  **QA Scenarios**:
  ```text
  Scenario: Happy path immediate first update
    Tool: Bash
    Preconditions: DevicePage open with active PLC connection
    Steps:
      1. Write M200 value in maintenance/control area.
      2. Observe top DevicePage label bound to M200.
      3. Repeat for M201 and M202.
    Expected Result: Labels reflect values without page re-click/re-entry.
    Failure Indicators: Value changes only after manual page interaction.
    Evidence: .sisyphus/evidence/task-6-immediate-update.md

  Scenario: Edge case reconnect timing
    Tool: Bash
    Preconditions: Active page with temporary PLC disconnect/reconnect simulation
    Steps:
      1. Disconnect then reconnect PLC/session.
      2. Write M200 again and observe label.
    Expected Result: Updates resume automatically after reconnect.
    Evidence: .sisyphus/evidence/task-6-reconnect.md
  ```

  **Evidence to Capture**:
  - [ ] task-6-immediate-update.md
  - [ ] task-6-reconnect.md

  **Commit**: NO

- [ ] 7. Add temporary UbiDemo-scoped diagnostics for duplication proof

  **What to do**:
  - Add minimal diagnostics/counters in UbiDemo layer to prove registration/subscription cardinality.
  - Ensure diagnostics can be removed or disabled after verification.

  **Must NOT do**:
  - No permanent noisy logging in production path.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: `[]`
  - **Skills Evaluated but Omitted**:
    - `writing`: runtime proof task, not documentation-only.

  **Parallelization**:
  - **Can Run In Parallel**: YES
  - **Parallel Group**: Wave 3 (with T8)
  - **Blocks**: F1, F3
  - **Blocked By**: T3, T4

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs` - best place for scoped counters.
  - `Stackdose.App.UbiDemo/Pages/SettingsPage.xaml.cs` - optional cross-page monitor visibility tie-in.

  **Acceptance Criteria**:
  - [ ] Diagnostic output clearly shows stable cardinality after repeated re-entry.
  - [ ] Diagnostic footprint is scoped and removable.

  **QA Scenarios**:
  ```text
  Scenario: Happy path stable cardinality evidence
    Tool: Bash
    Preconditions: Diagnostics enabled
    Steps:
      1. Enter/leave DevicePage 10 times.
      2. Capture diagnostics snapshot after each loop.
    Expected Result: Cardinality line remains flat after initial registration.
    Failure Indicators: Monotonic growth across loops.
    Evidence: .sisyphus/evidence/task-7-cardinality-series.md

  Scenario: Edge case diagnostics off
    Tool: Bash
    Preconditions: Toggle diagnostics disable path
    Steps:
      1. Disable diagnostics mode.
      2. Verify app behavior unchanged while no debug spam emitted.
    Expected Result: Functional parity retained without diagnostics.
    Evidence: .sisyphus/evidence/task-7-diagnostics-off.md
  ```

  **Evidence to Capture**:
  - [ ] task-7-cardinality-series.md
  - [ ] task-7-diagnostics-off.md

  **Commit**: NO

- [ ] 8. Execute integrated runtime verification and finalize bug fix

  **What to do**:
  - Run full scenario verification for immediate update and no duplicate registration.
  - Remove/disable temporary diagnostics if no longer needed.
  - Prepare final focused commit.

  **Must NOT do**:
  - Do not introduce unrelated UI/layout changes.

  **Recommended Agent Profile**:
  - **Category**: `unspecified-high`
  - **Skills**: [`playwright`]
  - `playwright`: UI/runtime scenario automation with evidence capture.
  - **Skills Evaluated but Omitted**:
    - `frontend-ui-ux`: no design work in this fix.

  **Parallelization**:
  - **Can Run In Parallel**: NO
  - **Parallel Group**: Wave 3 (integration gate)
  - **Blocks**: F1, F2, F3, F4
  - **Blocked By**: T4, T5, T6

  **References**:
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml`
  - `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs`
  - `Stackdose.App.UbiDemo/Pages/SettingsPage.xaml`
  - `Stackdose.App.UbiDemo/Pages/SettingsPage.xaml.cs`

  **Acceptance Criteria**:
  - [ ] M200/M201/M202 updates reflected immediately in DevicePage top labels.
  - [ ] Re-entry no longer creates duplicate registrations.
  - [ ] Build and existing test suites pass.

  **QA Scenarios**:
  ```text
  Scenario: Happy path write-to-label immediate update
    Tool: Playwright
    Preconditions: App launched, SuperAdmin default login, PLC connected
    Steps:
      1. Navigate to MachineA DevicePage.
      2. Set test values for M200=1, M201=0, M202=1.
      3. Assert DevicePage top labels show 1/0/1 within timeout 2s.
    Expected Result: All three labels update without page click/re-entry.
    Failure Indicators: Any label updates only after additional navigation interaction.
    Evidence: .sisyphus/evidence/task-8-happy-m200-m201-m202.png

  Scenario: Failure path stale-registration prevention
    Tool: Playwright
    Preconditions: Same as above
    Steps:
      1. Perform 10 cycles DevicePage <-> Overview.
      2. Change M200 value.
      3. Assert single consistent label transition (no duplicate flicker/lag).
    Expected Result: Stable single update behavior and no duplicate side effects.
    Evidence: .sisyphus/evidence/task-8-reentry-stability.png
  ```

  **Evidence to Capture**:
  - [ ] task-8-happy-m200-m201-m202.png
  - [ ] task-8-reentry-stability.png

  **Commit**: YES
  - Message: `fix(ubidemo): prevent duplicate devicepage registration and immediate plc label update`
  - Files: `Stackdose.App.UbiDemo/Pages/UbiDevicePage.xaml.cs`, related UbiDemo runtime files only
  - Pre-commit: `dotnet build && dotnet test`

---

## Final Verification Wave (MANDATORY - after ALL implementation tasks)

- [ ] F1. **Plan Compliance Audit** - `oracle`
  Verify every Must Have is implemented and every Must NOT Have is absent; validate evidence files exist.
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT`

- [ ] F2. **Code Quality Review** - `unspecified-high`
  Run `dotnet build` and `dotnet test`; inspect touched files for temporary hacks, dead code, and unsafe shortcuts.
  Output: `Build [PASS/FAIL] | Tests [PASS/FAIL] | Issues [N] | VERDICT`

- [ ] F3. **Real QA Replay** - `unspecified-high`
  Replay all task QA scenarios and store evidence under `.sisyphus/evidence/final-qa/`.
  Output: `Scenarios [N/N] | Edge Cases [N] | VERDICT`

- [ ] F4. **Scope Fidelity Check** - `deep`
  Ensure only `Stackdose.App.UbiDemo` scope changed and `Stackdose.UI.Core` remains untouched.
  Output: `Scope [PASS/FAIL] | Contamination [N] | VERDICT`

---

## Commit Strategy

- Group implementation tasks into one focused commit after T8 passes.
- Suggested message: `fix(ubidemo): prevent duplicate devicepage registration and restore immediate plclabel updates`

---

## Success Criteria

### Verification Commands
```bash
dotnet build Stackdose.App.UbiDemo/Stackdose.App.UbiDemo.csproj
dotnet test
```

### Final Checklist
- [ ] DevicePage no longer re-registers per re-entry.
- [ ] Top PlcLabels update without page re-click.
- [ ] No UI.Core file changed.
- [ ] Evidence files captured for happy and failure paths.
