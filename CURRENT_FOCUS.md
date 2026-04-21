# 目前開發焦點

> 每次開始工作前更新這個檔案。讓自己、主管、AI 都能立刻知道現在在做什麼。

---

## 今日焦點

**分支：** `refactor/foundation-and-behavior`
**主要工作：** 底層基礎 + Behavior Engine 重構（B0–B8）
**當前階段：** B0 底層現況校正（進行中 → 完成後停手等 B1 許可）
**路線圖：** 見 [`docs/refactor/README.md`](docs/refactor/README.md)

### 重構最終主旨
> 讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 21 CFR Part 11 稽核要求的監控介面。

---

## 重構進行中（B0–B8）

接手 AI 先讀：
1. [`docs/refactor/HANDOFF.md`](docs/refactor/HANDOFF.md) — 接手指引
2. [`docs/refactor/PLAN.md`](docs/refactor/PLAN.md) — 完整 9 階段計畫
3. [`docs/refactor/PROGRESS.md`](docs/refactor/PROGRESS.md) — 目前進度 / 下一步

| 階段 | 狀態 |
|---|---|
| B0 底層現況校正 | 🟡 進行中 |
| B1 抽共用基類 | ⚪ 等 B0 回報後授權 |
| B2–B8 | ⚪ 待命 |

**鐵律：** 每階段完成後停手回報，不自行連跑下一階段。

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
