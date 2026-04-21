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
| **B3** Shell 策略化 | ⚪ 待命 | — | — | — |
| **B4** Behavior Schema | ⚪ 待命 | — | — | — |
| **B5** Behavior Engine | ⚪ 待命 | — | — | — |
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

**現在在做：** 🛑 **等待使用者授權** B3（B1+B2+sln 修復全部完成並已 commit）

**已完成的 commits：**
| Commit | 內容 |
|---|---|
| `b0e424d` | B1+B2：5 個控件遷移 PlcControlBase、PlcEventContext 事件匯流 |
| `8e363f7` | docs：PROGRESS.md + devlog B1+B2 記錄 |
| `623335f` | fix(sln)：VisualStudioVersion 18→17.14、移除 inline # 注釋、加入 App.Monitor 至 UI.Core.sln |
| `238881b` | docs：sln 修復記入 PROGRESS + devlog |
| `468b60c` | docs(CLAUDE.md)：加入重構 reading order 入口 |
| `6b6636e` | fix(runtime)：PlcLabel circle DataTriggers、StaticLabel 支援、connect 按鈕 disabled 樣式 |
| `c3d2ba5` | fix(runtime)：StaticLabel 正確 key、ClipToBounds、VerticalAlignment |
| `0d4a639` | fix(runtime)：PlcLabel factory 補齊 5 個 display props |
| `8494b36` | feat(runtime)：ValueChanged 事件監測 ToggleButton |

**B1+B2 產出總覽：**
- 新增：`Stackdose.UI.Core/Helpers/PlcValueChangedEventArgs.cs`
- 修改：`PlcControlBase.cs`（ValueChanged event bus、OnPlcConnected/OnPlcDataUpdated 抽象方法）
- 修改：`PlcEventContext.cs`（ControlValueChanged + PublishControlValueChanged）
- 遷移：PlcLabel / PlcText / PlcStatusIndicator / AlarmViewer / SensorViewer（全部改用 PlcControlBase 覆寫模式）
- 編譯：0 errors、0 warnings（UI.Core + DeviceFramework + Tests + MachinePageDesigner 全過）

**下一步（需使用者授權後才能做）：** B3 Shell 策略化。

---

## 已知規矩 / 用戶偏好（重構期間）

- ✅ **可以移除舊控件**：不必為 UbiDemo / 測試專案編譯買單
- ✅ **底層先完成**：B0-B3 都處理底層，B4 之後才做上層
- 🛑 **每階段 stop-and-report**：除非明確授權連跑
- 🛑 **不要動 `docs/kb/` 中途版本**：B0 例外（目的就是改它），B1–B7 產出放 `docs/refactor/`，B8 才統一回灌
