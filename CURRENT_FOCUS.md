# 目前開發焦點

> 每次開始工作前更新這個檔案。讓自己、主管、AI 都能立刻知道現在在做什麼。
> 完整功能歷史見 `docs/STATUS.md`

---

## 目前狀態

**專案整體進度：✅ 所有主要功能完成，UI 視覺優化完成**

所有路線圖功能（含設計器核心、量產部署、功能缺口補強、Template Gallery、Command Sequence DSL）及 13 項架構優化均已完成。  
2026-04-17 進行第二輪 UI 視覺優化（MachinePageDesigner 主題色階、Viewer Header、PlcStatusIndicator 重設計）。

**下一步方向（尚未啟動）：**
- 真實客戶場景驗證（選一個設備，實際跑完 Designer → DesignPlayer 全流程）
- 客戶端文件（操作手冊、部署 SOP）
- 需要時再開新功能 Sprint

---

## 方案對照表（要改哪裡就開哪個）

| 工作內容 | 開啟方案 | Startup Project |
|---|---|---|
| 設計器 / 量產 App / 預覽 | `Stackdose.Designer.sln` | `Stackdose.App.DesignPlayer` |
| 設計器 + 開發驗證（PLC 連線） | `Stackdose.Designer.sln` | `Stackdose.App.DesignRuntime` |
| 框架核心 / DeviceFramework | `Stackdose.UI.Core.sln` | `Stackdose.App.UbiDemo` |
| 專案產生器 | `Stackdose.UI.Core.sln` | `Stackdose.Tools.ProjectGeneratorUI` |
| 全局修改 / 跨多個專案 | `Stackdose.UI.Core.sln` | — |

---

## 各專案狀態一覽

| 專案 | 說明 | 狀態 |
|---|---|---|
| `Stackdose.UI.Core` | WPF 控制項庫 + Context 系統 + 日誌 | ✅ 穩定 |
| `Stackdose.UI.Templates` | AppHeader / LeftNav / BottomBar Shell 布局 | ✅ 穩定 |
| `Stackdose.App.ShellShared` | 多 App 共用的 Shell 設定載入服務 | ✅ 穩定 |
| `Stackdose.App.DeviceFramework` | JSON 驅動設備 App 組裝框架（核心產品） | ✅ 穩定 |
| `Stackdose.App.UbiDemo` | UBI 烤箱完整範例（DeviceFramework 最佳實踐） | 維護 |
| `Stackdose.App.DesignRuntime` | 開發驗證用：手動 PLC + JSON 開啟 + 熱更新 | ✅ 完整 |
| `Stackdose.App.DesignPlayer` | 量產交付：JSON 設定 + PLC + Shell UI + 登入管控 | ✅ 完整 |
| `Stackdose.Tools.MachinePageDesigner` | 自由畫布拖曳設計器，輸出 .machinedesign.json | ✅ 完整 |
| `Stackdose.Tools.DesignViewer` | 拖入 JSON 即時預覽，不需 PLC | ✅ 完整 |
| `Stackdose.Tools.ProjectGeneratorUI` | GUI 版一鍵產生新設備 App 專案 | ✅ 穩定 |
| `Stackdose.Tools.ProjectGenerator` | CLI 版專案產生器（ProjectGeneratorUI 的核心 lib） | ✅ 穩定 |
