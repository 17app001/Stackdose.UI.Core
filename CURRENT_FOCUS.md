# 目前開發焦點

> 每次開始工作前更新這個檔案。讓自己、主管、AI 都能立刻知道現在在做什麼。

---

## 今日焦點

**開啟方案：** `Stackdose.Designer.sln`
**Startup Project：** `Stackdose.App.DesignPlayer`
**主要工作：** DesignPlayer 量產 Shell App — 功能完善 + 驗證

---

## 進行中的工作項目

- [x] DesignPlayer：建立量產 Shell App（完整 Shell UI + JSON 設定 + PLC 連線 + 登入管控）
- [x] DesignRuntime + DesignPlayer：JSON 熱更新（FileSystemWatcher，儲存後自動重載畫布）
- [ ] DesignPlayer：驗證在真實環境部署（需配合 PLC 測試）
- [ ] DesignRuntime：PLC 連線斷線重連穩定性
- [ ] MachinePageDesigner：下一個功能待確認（候選：多頁面支援、控制項屬性面板改善）

---

## 方案對照表（要改哪裡就開哪個）

| 工作內容 | 開啟方案 | Startup Project |
|---|---|---|
| 設計器 / 量產 App / 預覽（**目前**） | `Stackdose.Designer.sln` | `Stackdose.App.DesignPlayer` |
| 設計器 + 開發驗證 | `Stackdose.Designer.sln` | `Stackdose.App.DesignRuntime` |
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
| `Stackdose.App.DesignRuntime` | 開發驗證用：手動 PLC + JSON 開啟 + 熱更新 | **開發中** |
| `Stackdose.App.DesignPlayer` | 量產交付：JSON 設定 + PLC + Shell UI + 登入管控 | **開發中** |
| `Stackdose.Tools.MachinePageDesigner` | 自由畫布拖曳設計器，輸出 .machinedesign.json | **主力** |
| `Stackdose.Tools.DesignViewer` | 拖入 JSON 即時預覽，不需 PLC | **開發中** |
| `Stackdose.Tools.ProjectGeneratorUI` | GUI 版一鍵產生新設備 App 專案 | 穩定 |
| `Stackdose.Tools.ProjectGenerator` | CLI 版專案產生器（ProjectGeneratorUI 的核心 lib） | 穩定 |
