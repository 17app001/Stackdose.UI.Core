# 重構進度追蹤

> 每完成一階段打勾 + 補日期 + 補 commit SHA。
> 接手 AI：先看最下面「當前焦點 / 下一步」區塊。

---

## 階段總覽

| 階段 | 狀態 | 日期 | Commit | 說明 |
|---|---|---|---|---|
| **B0** 底層現況校正 | ✅ 完成 | 2026-04-21 | `01a903c` | 盤點、修文件，不動程式碼 |
| **B1** 抽共用基類 | ✅ 完成 | 2026-04-21 | `b0e424d` | PlcLabel/Text/StatusIndicator/AlarmViewer/SensorViewer 全遷移 |
| **B2** 事件能力收斂 | ✅ 完成 | 2026-04-21 | `b0e424d` | PlcEventContext + ControlValueChanged event bus（與 B1 同 commit） |
| **B3** Templates/Shell 策略化 | ✅ 完成 | 2026-04-21 | `70b919f` | IShellStrategy + FreeCanvas/SinglePage/Standard |
| **B4** Behavior Schema | ✅ 完成 | 2026-04-21 | `4a8cc13` | BehaviorEvent/Condition/Action POCO + events[] |
| **B5** Behavior Engine | ✅ 完成 | 2026-04-22 | `34d9c1f` | BehaviorEngine + 6 Handler + SecuredButton click + DesignRuntime 接線 |
| **B6** Designer UI | ⚪ 待命 | — | — | — |
| **B7** Standard 模式收尾 | ⚪ 待命 | — | — | — |
| **B8** docs 全面對齊 | ⚪ 待命 | — | — | — |

圖例：⚪ 待命 / 🟡 進行中 / ✅ 完成 / ⛔ 擱置

---

## B0 子任務 checklist

- [x] **B0.0** 建立 `docs/refactor/` 元文件（README / PLAN / PROGRESS / HANDOFF）— 2026-04-21
- [x] **B0.1** Core Controls DP 盤點 → `B0-control-inventory.md` — 2026-04-21
- [x] **B0.2** Templates Controls / Pages 盤點 — 2026-04-21
- [x] **B0.3** 文件 vs 實際差異表 → `B0-findings.md` — 2026-04-21
- [x] **B0.4** 修正 `docs/kb/controls-reference.md` — 2026-04-21
- [x] **B0.5** 修正 `docs/kb/architecture.md` + `docs/PROJECT_MAP.md` + `CLAUDE.md` — 2026-04-21
- [x] **B0.6** 更新 `CURRENT_FOCUS.md` + `devlog/2026-04.md`（`index.html` 不存在，跳過） — 2026-04-21
- [x] **B0.7** Commit 所有 B0 變更（`01a903c`）+ 回報用戶、停手 — 2026-04-21

---

## B1+B2 子任務 checklist

- [x] **B1.1** 移動 `PlcValueChangedEventArgs` 到 `Helpers/`（共用） — 2026-04-21
- [x] **B1.2** 升級 `PlcControlBase`：加 `ValueChanged` event、`RaiseValueChanged()`、`OnPlcConnected`/`OnPlcDataUpdated` 抽象覆寫、`OnGlobalStatusChanged` — 2026-04-21
- [x] **B1.3** 升級 `PlcEventContext`：加 `ControlValueChanged` static event、`PublishControlValueChanged()` — 2026-04-21
- [x] **B1.4** PlcLabel → PlcControlBase（移除手動 PlcStatus 訂閱、修 OnThemeChanged override）— 2026-04-21
- [x] **B1.5** PlcText → PlcControlBase（移除 `_subscribedStatus` 樣板、移除重複 SafeInvoke）— 2026-04-21
- [x] **B1.6** PlcStatusIndicator → PlcControlBase（完整重寫，精簡 70% 程式碼）— 2026-04-21
- [x] **B1.7** AlarmViewer → PlcControlBase（移除 `BindToStatus`/`_boundStatus`/`OnScanUpdated`）— 2026-04-21
- [x] **B1.8** SensorViewer → PlcControlBase（移除手動 ConnectionEstablished 訂閱）— 2026-04-21
- [x] **B1.9** 編譯驗證：0 errors、0 warnings — 2026-04-21

---

## 當前焦點 / 下一步

> 接手 AI：從這裡開始做事。

**現在在做：** 🛑 **等待使用者授權** B6（B5 Behavior Engine 已完成並 commit `34d9c1f`）

**B5 產出：**
- 新增：`UI.Core/Models/BehaviorModels.cs`（移入 UI.Core，打破循環依賴）
- 新增：`UI.Core/Models/IControlWithBehaviors.cs`（介面：BehaviorEngine 不依賴具體類別）
- 新增：`UI.Core/Helpers/BehaviorEventBus.cs`（靜態事件匯流排，解耦 SecuredButton → Engine）
- 新增：`ShellShared/Behaviors/BehaviorEngine.cs`（核心：訂閱 PLC + click，評估條件，派發 Handler）
- 新增：`ShellShared/Behaviors/IBehaviorActionHandler.cs` + `BehaviorActionContext.cs` + `ControlRuntimeTag.cs`
- 新增：6 個 Handler（SetProp / WritePlc / LogAudit / ShowDialog / Navigate / SetStatus）
- 修改：`SecuredButton.xaml.cs`（BehaviorId DP + 驗證後 Fire BehaviorEventBus）
- 修改：`RuntimeControlFactory.cs`（AttachBehaviorTag + BuildPropSetters）
- 修改：`MainWindow.xaml.cs`（BehaviorEngine 欄位、BindDocument、PlcManager 接線、Closing Dispose）
- 修改：`DesignerItemDefinition.cs`（implements IControlWithBehaviors）

**事件流程：**
```
PLC 值變 → PlcEventContext.ControlValueChanged
SecuredButton 點擊 → BehaviorEventBus.ControlEventFired
                     ↓
             BehaviorEngine.DispatchCore
                     ↓
           評估 when 條件 → 執行 do 動作
```

**已完成的 commits：**
| Commit | 內容 |
|---|---|
| `b0e424d` | B1+B2：5 個控件遷移 PlcControlBase、PlcEventContext 事件匯流 |
| `70b919f` | B3：IShellStrategy + 三策略 + DesignRuntime 接線 |
| `4a8cc13` | B4：BehaviorEvent/Condition/Action POCO + events[] |
| `e497a93` | docs：B4 designer-system.md + PROGRESS + devlog |
| `34d9c1f` | B5：BehaviorEngine + Handlers + SecuredButton click + DesignRuntime 接線 |

**下一步（需使用者授權後才能做）：** B6 Designer UI（在 MachinePageDesigner 中加入 events 編輯 UI）。

---

## 已知規矩 / 用戶偏好（重構期間）

- ✅ **可以移除舊控件**：不必為 UbiDemo / 測試專案編譯買單
- ✅ **底層先完成**：B0-B3 都處理底層，B4 之後才做上層
- 🛑 **每階段 stop-and-report**：除非明確授權連跑
- 🛑 **不要動 `docs/kb/` 中途版本**：B0 例外（目的就是改它），B1–B7 產出放 `docs/refactor/`，B8 才統一回灌
