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
| **B6** Designer UI | ✅ 完成 | 2026-04-22 | `f314dcf` | PropertyPanel → TabControl + EventsPanel 事件編輯 UI |
| **B7** Standard 模式收尾 | ✅ 完成 | 2026-04-21 | `d7c185a` | PageDefinition + pages[] + SetupMultiPageNavigation + Navigator 接線 |
| **B8** docs 全全面對齊 | ✅ 完成 | 2026-04-21 | `b11398a` | kb/ 新增 behavior-system + foundation-base-classes；architecture/controls 更新 |
| **B9** 格式與腳本同步 | ✅ 完成 | 2026-04-21 | `49d4109` | 同步 Designer JSON 格式 (pageId/name/layout) 與 init-shell-app 腳本產出 |


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

**現在在做：** ✅ **B0–B8 全部完成**（`refactor/foundation-and-behavior` 重構結束）

**B7 產出：**
- 新增：`Stackdose.Tools.MachinePageDesigner/Models/PageDefinition.cs`（id / title / canvasItems[]）
- 修改：`DesignDocument`（加 `pages[]`，空清單向後相容）
- 修改：`DesignRuntime.csproj`（直接引用 UI.Templates）
- 修改：`MainWindow.xaml.cs`（`ApplyShellStrategy` 回傳 bool；Standard+pages[] 走 `SetupMultiPageNavigation`；`BehaviorEngine.Navigator` 完整接線）

**B6 產出：**
- 新增：`MachinePageDesigner/ViewModels/BehaviorEventViewModel.cs`（包裝 BehaviorEvent POCO，ObservableCollection<BehaviorActionViewModel>，靜態 OnTypes/WhenOps）
- 新增：`MachinePageDesigner/ViewModels/BehaviorActionViewModel.cs`（包裝 BehaviorAction POCO，ShowTarget/ShowProp/ShowValue 可見性屬性，Summary 顯示字串）
- 新增：`MachinePageDesigner/Views/EventsPanel.xaml` + `.cs`（3 層 Master-Detail：事件清單 → 詳情 → 動作清單 → 動作詳情，_suppressHandlers 防回饋迴圈）
- 修改：`MachinePageDesigner/ViewModels/DesignerItemViewModel.cs`（加 Events ObservableCollection + AddEvent/RemoveEvent + BuildEventsCollection 同步）
- 修改：`MachinePageDesigner/Views/PropertyPanel.xaml`（最外層 ScrollViewer → TabControl，"屬性" 頁 + "事件 ⚡" 頁）

**已完成的 commits：**
| Commit | 內容 |
|---|---|
| `b0e424d` | B1+B2：5 個控件遷移 PlcControlBase、PlcEventContext 事件匯流 |
| `70b919f` | B3：IShellStrategy + 三策略 + DesignRuntime 接線 |
| `4a8cc13` | B4：BehaviorEvent/Condition/Action POCO + events[] |
| `e497a93` | docs：B4 designer-system.md + PROGRESS + devlog |
| `34d9c1f` | B5：BehaviorEngine + Handlers + SecuredButton click + DesignRuntime 接線 |

**下一步：** 重構完成，合併分支至 master 或繼續 MachinePageDesigner / DesignRuntime 功能開發。

---

## 已知規矩 / 用戶偏好（重構期間）

- ✅ **可以移除舊控件**：不必為 UbiDemo / 測試專案編譯買單
- ✅ **底層先完成**：B0-B3 都處理底層，B4 之後才做上層
- 🛑 **每階段 stop-and-report**：除非明確授權連跑
- 🛑 **不要動 `docs/kb/` 中途版本**：B0 例外（目的就是改它），B1–B7 產出放 `docs/refactor/`，B8 才統一回灌

---

## ⚠️ 未解問題

> 記錄已知但暫未處理的問題。解決後移除該行，不要留空區塊。

| 問題 | 所在專案 | 優先度 | 備註 |
|---|---|---|---|
| JSON 熱更新（修改 JSON 後自動重新載入畫布） | DesignRuntime | 中 | 重構後尚未實作 |
| PLC 斷線重連穩定性 | DesignRuntime | 高 | 長時間連線偶發掉線未處理 |
| MachinePageDesigner 下一個功能待確認 | MachinePageDesigner | 低 | 等用戶決定方向 |
