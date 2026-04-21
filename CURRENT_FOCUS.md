# 目前開發焦點

> 每次開始工作前更新這個檔案。讓自己、主管、AI 都能立刻知道現在在做什麼。

---

## 今日焦點

**分支：** `refactor/foundation-and-behavior`（✅ B0–B8 重構全部完成）
**下一方向：** MachinePageDesigner 功能 + DesignRuntime 穩定性

### 重構最終主旨
> 讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 21 CFR Part 11 稽核要求的監控介面。

---

## 重構成果（B0–B8 ✅ 全部完成）

| 階段 | Commit | 重點 |
|---|---|---|
| B0 底層校正 | `01a903c` | 盤點文件，0 動程式碼 |
| B1+B2 基類+事件匯流 | `b0e424d` | PlcControlBase + PlcEventContext.ControlValueChanged |
| B3 Shell 策略化 | `70b919f` | IShellStrategy + FreeCanvas/SinglePage/Standard |
| B4 Behavior Schema | `4a8cc13` | BehaviorEvent/Condition/Action POCO + events[] |
| B5 Behavior Engine | `34d9c1f` | BehaviorEngine + 6 Handler |
| B6 Designer UI | `f314dcf` | PropertyPanel → TabControl + EventsPanel |
| B7 Standard 多頁導覽 | `d7c185a` | PageDefinition + LeftNav 接線 + Navigator |
| B8 docs 對齊 | pending | kb/ 全面更新 |

---

## 暫停的工作（重構完再回來）

- [ ] DesignRuntime：JSON 熱更新（修改 JSON 後自動重新載入畫布）
- [ ] DesignRuntime：PLC 連線斷線重連穩定性
- [ ] MachinePageDesigner：待確認下一個功能

---

## 方案對照表（要改哪裡就開哪個）

| 工作內容 | 開啟方案 | Startup Project |
|---|---|---|
| 設計器 / 執行環境 / 預覽（**目前**） | `Stackdose.Designer.sln` | `Stackdose.App.DesignRuntime` |
| 框架核心 / DeviceFramework | `Stackdose.UI.Core.sln` | `Stackdose.App.UbiDemo` |
| 專案產生器 | `Stackdose.UI.Core.sln` | `Stackdose.Tools.ProjectGeneratorUI` |
| 全局修改 / 跨多個專案 | `Stackdose.UI.Core.sln` | — |

---

## 各專案一句話說明

| 專案 | 一句話 | 狀態 |
|---|---|---|
| `Stackdose.UI.Core` | WPF 控制項庫 + Context 系統 + 日誌 | 穩定 |
| `Stackdose.UI.Templates` | AppHeader / LeftNav / BottomBar Shell 布局 | 穩定 |
| `Stackdose.App.ShellShared` | 多 App 共用的 Shell 設定載入服務 | 穩定 |
| `Stackdose.App.DeviceFramework` | JSON 驅動設備 App 組裝框架（核心產品） | 穩定 |
| `Stackdose.App.UbiDemo` | UBI 烤箱完整範例（DeviceFramework 最佳實踐） | 維護 |
| `Stackdose.App.DesignRuntime` | 真實 PLC 連線 + 載入設計 JSON 執行 | **開發中** |
| `Stackdose.Tools.MachinePageDesigner` | 自由畫布拖曳設計器，輸出 .machinedesign.json | **主力** |
| `Stackdose.Tools.DesignViewer` | 拖入 JSON 即時預覽，不需 PLC | **開發中** |
| `Stackdose.Tools.ProjectGeneratorUI` | GUI 版一鍵產生新設備 App 專案 | 穩定 |
| `Stackdose.Tools.ProjectGenerator` | CLI 版專案產生器（ProjectGeneratorUI 的核心 lib） | 穩定 |
