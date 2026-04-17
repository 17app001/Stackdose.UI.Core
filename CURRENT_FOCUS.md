# 目前開發焦點

> 每次開始工作前更新這個檔案。讓自己、主管、AI 都能立刻知道現在在做什麼。
> 完整功能歷史見 `docs/STATUS.md`

---

## 目前狀態

**專案整體進度：✅ 所有主要功能完成 + 4/17 修復與新功能**

4/17 完成項目：
- PlcLabel 顏色系統重設計（PlcFill.* Material 色彩 + Black enum + 補齊 Cyber.NeonRed/Green token）
- 修復設計器預覽 Opacity=0.5（DataTrigger 觸發導致數值顏色在設計時失真）
- App Config 編輯器（⚙ App Config 按鈕 → 一鍵產生 app-config.json）
- SensorViewer 多實例污染修復 + 路徑解析與 AlarmViewer 統一
- Pipeline 靜態稽核修復 3 個 bug（SecuredButton RequiredLevel、IndicatorLabelForeground 通知、SensorViewer 靜態 flag）

**下一步：Dashboard 模式（預計 2026-04-21 開始）**

規格確認：
- `layout.mode: "Dashboard"` — 設計器 Layout 下拉新增選項
- 無邊框視窗（`WindowStyle=None`, `ResizeMode=NoResize`）
- 極簡 TopBar（~32px）：PLC 狀態燈 + 設備名稱 + 時鐘 + 電源按鈕
- 完全隱藏：左側面板 / User Management / 頁面切換 / Main View 按鈕
- 視窗固定尺寸 = canvas 寬高 + TopBar 高度，只允許最小化
- Canvas 填滿剩餘空間，不縮放（不使用 Viewbox）

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
