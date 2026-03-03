# Single-Page Empty Canvas Preset (Keep Top PlcStatus)

## TL;DR
> **Summary**: Add a new local-editable single-page layout preset that keeps the existing top `PlcStatus` section and makes the lower design area empty (no `GroupBoxBlock`) for direct Visual Studio Designer drag/drop.
> **Deliverables**:
> - New `-DesignerLayoutPreset EmptyCanvas` option in script
> - Generated local-editable page with top section unchanged + empty lower design surface
> - Updated quickstart docs listing the new preset and usage examples
> **Effort**: Short
> **Parallel**: YES - 2 waves
> **Critical Path**: T1 preset contract update -> T2 markup switch update -> T3 generated app verification

## Context
### Original Request
Add a layout with no `GroupBox`, default empty, and directly draggable area; keep top `PlcStatus`.

### Interview Summary
- User confirmed: keep top `PlcStatus` section.
- Lower design region should be empty and drag-ready in Visual Studio Designer.
- This is for `-SinglePageDesignerLocalEditable` flow.

### Metis Review (gaps addressed)
- Metis identified key risks: ambiguity of "empty" lower area (instruction text can violate empty), duplicated preset docs that can drift, and no-drop-target UX if the lower area is literally absent.
- This plan resolves them by requiring: no lower instruction text for `EmptyCanvas`, docs parity checks, and an explicit empty drop-surface container while preserving top `TopPlcStatus`.

## Work Objectives
### Core Objective
Introduce a new preset for local-editable single-page scaffolding that removes all default grouped containers from the lower area while preserving the existing top runtime/status wiring.

### Deliverables
- Script parameter accepts new preset keyword.
- `$designerLayoutMarkup` switch emits empty lower area markup for the new preset.
- Generated page still includes top title + `TopPlcStatus` area unchanged.
- Docs include new preset in option list and usage snippets.
- Generated project compiles successfully.

### Definition of Done (verifiable conditions with commands)
- `scripts/init-shell-app.ps1` includes `EmptyCanvas` in `DesignerLayoutPreset` ValidateSet.
- Running generation with `-SinglePageDesignerLocalEditable -DesignerLayoutPreset EmptyCanvas` creates page XAML with no `GroupBoxBlock` in lower area.
- Top section in generated page still contains `TopPlcStatus` and machine summary block.
- Generated page in `EmptyCanvas` contains no lower instruction text (`Design this area in Visual Studio Designer.`).
- `QUICKSTART.md` includes `EmptyCanvas` example.
- Generated project `dotnet build` passes without errors.

### Must Have
- New preset is only meaningful for local-editable mode.
- Top section behavior (header + `TopPlcStatus`) is unchanged.
- Lower area is a drag-ready empty `Grid`/surface without sample controls.
- Lower area has no default instruction text for `EmptyCanvas`.
- No changes to `Stackdose.UI.Core` control logic.

### Must NOT Have (guardrails, AI slop patterns, scope boundaries)
- Must NOT alter non-local-editable template mode behavior.
- Must NOT remove existing presets (`ThreeColumn`, `TwoColumn64`, `TwoByTwo`).
- Must NOT inject runtime drag/drop code-behind logic.
- Must NOT modify unrelated solution membership or generated test app artifacts.

## Verification Strategy
> ZERO HUMAN INTERVENTION — all verification is agent-executed.
- Test decision: tests-after + `dotnet build`
- QA policy: Every task includes agent-executed happy/failure checks
- Evidence: `.sisyphus/evidence/task-{N}-{slug}.{ext}`

## Execution Strategy
### Parallel Execution Waves
> Target: 5-8 tasks per wave. <3 per wave (except final) = under-splitting.
> Extract shared dependencies as Wave-1 tasks for max parallelism.

Wave 1: Script contract + markup generation + docs alignment
Wave 2: Generation verification + negative checks + cleanup safety checks

### Dependency Matrix (full, all tasks)
- T1 blocks T2, T4, T5
- T2 blocks T3
- T4 independent after T1
- T3 blocks T5
- T5 is final pre-merge quality gate

### Agent Dispatch Summary (wave -> task count -> categories)
- Wave 1 -> 3 tasks -> quick (script/docs)
- Wave 2 -> 2 tasks -> quick + unspecified-low (verification/audit)

## TODOs
> Implementation + Test = ONE task. Never separate.
> EVERY task MUST have: Agent Profile + Parallelization + QA Scenarios.

- [ ] 1. Extend preset contract to include `EmptyCanvas`

  **What to do**: Update `scripts/init-shell-app.ps1` parameter contract so `-DesignerLayoutPreset` accepts `EmptyCanvas` while preserving existing defaults and valid options.
  **Must NOT do**: Do not change default from `ThreeColumn`; do not alter non-local-editable flags.

  **Recommended Agent Profile**:
  - Category: `quick` — Reason: single-file parameter contract update
  - Skills: `[]` — no additional skill required
  - Omitted: `playwright` — not needed for script parameter change

  **Parallelization**: Can Parallel: NO | Wave 1 | Blocks: 2,4,5 | Blocked By: none

  **References** (executor has NO interview context — be exhaustive):
  - Pattern: `scripts/init-shell-app.ps1:14` — `ValidateSet("ThreeColumn", "TwoColumn64", "TwoByTwo")`
  - Pattern: `scripts/init-shell-app.ps1:15` — default preset remains `ThreeColumn`
  - Pattern: `scripts/init-shell-app.ps1:189` — local-editable-only preset warning gate

  **Acceptance Criteria** (agent-executable only):
  - [ ] `grep "ValidateSet\(\"ThreeColumn\", \"TwoColumn64\", \"TwoByTwo\", \"EmptyCanvas\"\)" scripts/init-shell-app.ps1` returns one match.
  - [ ] `grep "\[string\]\$DesignerLayoutPreset = \"ThreeColumn\"" scripts/init-shell-app.ps1` still matches.

  **QA Scenarios** (MANDATORY — task incomplete without these):
  ```
  Scenario: Happy path - new preset accepted by script parser
    Tool: Bash
    Steps: Run `powershell -NoProfile -File .\scripts\init-shell-app.ps1 -AppName "Stackdose.App.PresetParseCheck" -DestinationRoot . -SinglePageDesignerLocalEditable -DesignerLayoutPreset EmptyCanvas`
    Expected: Script completes without parameter validation errors.
    Evidence: .sisyphus/evidence/task-1-preset-contract.txt

  Scenario: Failure/edge case - invalid preset still rejected
    Tool: Bash
    Steps: Run same command with `-DesignerLayoutPreset EmptyCanvasX`
    Expected: PowerShell ValidateSet error is shown.
    Evidence: .sisyphus/evidence/task-1-preset-contract-error.txt
  ```

  **Commit**: NO | Message: `feat(script): add EmptyCanvas preset contract` | Files: `scripts/init-shell-app.ps1`

- [ ] 2. Add `EmptyCanvas` layout markup branch with top section untouched

  **What to do**: Add a new branch in `$designerLayoutMarkup` switch that emits a lower-area empty design surface (Grid/Border) with no `GroupBoxBlock` and no sample controls.
  **Must NOT do**: Do not modify top section markup containing `MachineSummaryText` and `TopPlcStatus`; do not alter existing preset branches.

  **Recommended Agent Profile**:
  - Category: `quick` — Reason: localized script string-template update
  - Skills: `[]` — no external dependencies
  - Omitted: `frontend-ui-ux` — existing style tokens should be reused

  **Parallelization**: Can Parallel: NO | Wave 1 | Blocks: 3,5 | Blocked By: 1

  **References** (executor has NO interview context — be exhaustive):
  - Pattern: `scripts/init-shell-app.ps1:95` — `$designerLayoutMarkup = switch ($DesignerLayoutPreset)`
  - Pattern: `scripts/init-shell-app.ps1:120` — existing branch style (`TwoByTwo`) for indentation/templating
  - Pattern: `scripts/init-shell-app.ps1:160` — default (`ThreeColumn`) branch location
  - Pattern: `scripts/init-shell-app.ps1:364` — insertion point of `$designerLayoutMarkup`
  - Pattern: `scripts/init-shell-app.ps1:335-343` — top `TopPlcStatus` section that must remain unchanged

  **Acceptance Criteria** (agent-executable only):
  - [ ] `grep "\"EmptyCanvas\"" scripts/init-shell-app.ps1` returns switch-branch definition.
  - [ ] Generated page for this preset contains no `templateControls:GroupBoxBlock` in lower area markup.
  - [ ] Generated page still contains `TopPlcStatus` declaration.
  - [ ] Generated page for this preset contains no `Design this area in Visual Studio Designer.` text.

  **QA Scenarios** (MANDATORY — task incomplete without these):
  ```
  Scenario: Happy path - generated page is empty lower canvas
    Tool: Bash
    Steps: Generate project with `-SinglePageDesignerLocalEditable -DesignerLayoutPreset EmptyCanvas`; inspect generated `Pages/SingleDetailWorkspacePage.xaml`
    Expected: Lower region has empty drag surface, zero `GroupBoxBlock` entries, and no instruction text; top region still has `TopPlcStatus`.
    Evidence: .sisyphus/evidence/task-2-empty-canvas-markup.txt

  Scenario: Failure/edge case - existing presets remain unchanged
    Tool: Bash
    Steps: Generate project with `-DesignerLayoutPreset ThreeColumn`; inspect generated page.
    Expected: Existing three `GroupBoxBlock` placeholders still present.
    Evidence: .sisyphus/evidence/task-2-empty-canvas-markup-error.txt
  ```

  **Commit**: NO | Message: `feat(script): add EmptyCanvas markup branch` | Files: `scripts/init-shell-app.ps1`

- [ ] 3. Verify generation + build for `EmptyCanvas` preset

  **What to do**: Run end-to-end scaffold generation and `dotnet build` for a temporary verification app using the new preset.
  **Must NOT do**: Do not add generated temp app to solution/commit scope.

  **Recommended Agent Profile**:
  - Category: `quick` — Reason: command-based verification
  - Skills: `[]` — standard shell verification
  - Omitted: `git-master` — no git history operations required

  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: 5 | Blocked By: 2

  **References** (executor has NO interview context — be exhaustive):
  - Pattern: `scripts/init-shell-app.ps1` — generation entry point
  - Pattern: `scripts/init-shell-app.ps1:77` — note on `-DestinationRoot ..` when run under `scripts/`
  - Pattern: `scripts/init-shell-app.ps1:248` onward — local-editable page generation flow

  **Acceptance Criteria** (agent-executable only):
  - [ ] Generation command exits successfully for `EmptyCanvas`.
  - [ ] `dotnet build` of generated app exits with code 0.
  - [ ] Generated page includes `TopPlcStatus` and has no lower `GroupBoxBlock` placeholders.

  **QA Scenarios** (MANDATORY — task incomplete without these):
  ```
  Scenario: Happy path - scaffold + build
    Tool: Bash
    Steps: Generate `Stackdose.App.VerifyEmptyCanvasTmp` with `EmptyCanvas`; run `dotnet build` on generated csproj.
    Expected: Build succeeds, and generated page layout matches requirement.
    Evidence: .sisyphus/evidence/task-3-empty-canvas-build.txt

  Scenario: Failure/edge case - unsupported mode guard
    Tool: Bash
    Steps: Run script in template mode with `-SinglePageDesigner -DesignerLayoutPreset EmptyCanvas`.
    Expected: Existing warning behavior indicates preset applies to local-editable mode.
    Evidence: .sisyphus/evidence/task-3-empty-canvas-build-error.txt
  ```

  **Commit**: NO | Message: `test(script): verify EmptyCanvas scaffold generation` | Files: `scripts/Stackdose.App.VerifyEmptyCanvasTmp/*` (excluded from final commit)

- [ ] 4. Update quickstart docs with `EmptyCanvas` usage

  **What to do**: Update `QUICKSTART.md` option list and examples to include `EmptyCanvas`, including one direct generation command.
  **Must NOT do**: Do not remove existing examples for `ThreeColumn`, `TwoColumn64`, or `TwoByTwo`.

  **Recommended Agent Profile**:
  - Category: `writing` — Reason: documentation update in Traditional Chinese
  - Skills: `[]` — existing doc style is sufficient
  - Omitted: `frontend-ui-ux` — not relevant to docs

  **Parallelization**: Can Parallel: YES | Wave 1 | Blocks: 5 | Blocked By: 1

  **References** (executor has NO interview context — be exhaustive):
  - Pattern: `QUICKSTART.md` — section listing preset options and command examples
  - Pattern: `scripts/init-shell-app.ps1:1037-1038` — script-generated guidance text that enumerates presets

  **Acceptance Criteria** (agent-executable only):
  - [ ] `QUICKSTART.md` contains `EmptyCanvas` in preset option text.
  - [ ] `QUICKSTART.md` includes at least one runnable `powershell` command example with `-DesignerLayoutPreset EmptyCanvas`.

  **QA Scenarios** (MANDATORY — task incomplete without these):
  ```
  Scenario: Happy path - doc discoverability
    Tool: Bash
    Steps: Search `QUICKSTART.md` for `EmptyCanvas` and example command line.
    Expected: Both preset label and command example are present.
    Evidence: .sisyphus/evidence/task-4-quickstart-emptycanvas.txt

  Scenario: Failure/edge case - regression in existing preset docs
    Tool: Bash
    Steps: Verify `ThreeColumn`, `TwoColumn64`, and `TwoByTwo` still appear in preset section.
    Expected: Existing presets remain documented.
    Evidence: .sisyphus/evidence/task-4-quickstart-emptycanvas-error.txt
  ```

  **Commit**: NO | Message: `docs(quickstart): add EmptyCanvas preset usage` | Files: `QUICKSTART.md`

- [ ] 5. Final scope and artifact hygiene audit before execution handoff

  **What to do**: Confirm only intended files are changed, ensure no generated temporary app folders are included, and ensure existing presets still pass smoke checks.
  **Must NOT do**: Do not revert unrelated user-owned changes.

  **Recommended Agent Profile**:
  - Category: `unspecified-low` — Reason: final audit and guardrail enforcement
  - Skills: `[]` — basic repository hygiene checks
  - Omitted: `git-master` — no history rewrite operations needed

  **Parallelization**: Can Parallel: NO | Wave 2 | Blocks: none | Blocked By: 2,3,4

  **References** (executor has NO interview context — be exhaustive):
  - Pattern: `git status --short` output after implementation
  - Pattern: Script preset section in `scripts/init-shell-app.ps1`
  - Pattern: `QUICKSTART.md` preset commands section

  **Acceptance Criteria** (agent-executable only):
  - [ ] Working tree diff includes only intended files for this feature.
  - [ ] No `scripts/Stackdose.App.Verify*` temporary folders staged for commit.
  - [ ] Smoke generation with one legacy preset (e.g., `ThreeColumn`) still succeeds.

  **QA Scenarios** (MANDATORY — task incomplete without these):
  ```
  Scenario: Happy path - clean commit scope
    Tool: Bash
    Steps: Run `git status --short` and inspect staged/unstaged files after filtering.
    Expected: Only `scripts/init-shell-app.ps1` and `QUICKSTART.md` (plus intentional related docs) are in final scope.
    Evidence: .sisyphus/evidence/task-5-scope-audit.txt

  Scenario: Failure/edge case - accidental generated artifact inclusion
    Tool: Bash
    Steps: Check for `scripts/Stackdose.App.Verify*` and root `Stackdose.App.MySinglePage*` accidental staging.
    Expected: No temporary/generated folders are staged.
    Evidence: .sisyphus/evidence/task-5-scope-audit-error.txt
  ```

  **Commit**: YES | Message: `feat(script): add EmptyCanvas local-editable single-page preset` | Files: `scripts/init-shell-app.ps1`, `QUICKSTART.md`

## Final Verification Wave (4 parallel agents, ALL must APPROVE)
- [ ] F1. Plan Compliance Audit — oracle
- [ ] F2. Code Quality Review — unspecified-high
- [ ] F3. Real Manual QA — unspecified-high (+ playwright if UI)
- [ ] F4. Scope Fidelity Check — deep

## Commit Strategy
- Single commit recommended: `feat(script): add EmptyCanvas single-page preset for local editable scaffolds`
- Include only: `scripts/init-shell-app.ps1`, `QUICKSTART.md`
- Exclude unrelated generated app folders and solution file drift.

## Success Criteria
- New projects generated with `EmptyCanvas` open with unchanged top status strip and empty lower design region.
- Designer users can directly drag controls into the lower area without removing default `GroupBoxBlock` scaffolding.
- Existing preset behavior remains intact.
