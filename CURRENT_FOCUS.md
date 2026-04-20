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

**4/20 完成項目：Dashboard 模式**

- `layout.mode: "Dashboard"` — 設計器 Layout 下拉新增 Dashboard 選項
- **DesignPlayer** 載入 Dashboard 設計稿時自動切換：
  - 無邊框視窗（`ResizeMode=CanMinimize`，只允許最小化）
  - 視窗固定尺寸 = canvas 寬 × (canvas 高 + 32px)，置中顯示
  - 極簡 DashboardTopBar（32px）：PlcStatusIndicator + 設備名稱 + 時鐘 + 最小化 / 關閉
  - 完全隱藏：MainShell（Header + LeftNav + BottomBar）/ 頁籤列
  - Canvas 直接填滿剩餘空間，無 ScrollViewer padding、無縮放
- **DesignRuntime** 同步支援 Dashboard 模式預覽：
  - 偵測 Dashboard layout 自動進入預覽模式
  - 黃色 Banner：「🙈 隱藏工具列」切換鈕 + 「✕ 退出預覽」
  - 隱藏工具列：PlcConfigBar 收起 + 標題列移除，純 Canvas 視圖，Banner 可拖曳移動視窗
  - 顯示工具列：還原 PlcConfigBar + 標題列，視窗尺寸重新計算
  - 退出預覽：完整還原所有工具列與視窗狀態

**4/20 完成項目（續）：一鍵封裝 Dashboard App**

- **MachinePageDesigner** 新增「📦 封裝 App」按鈕（僅 Dashboard 模式 + 已儲存時啟用）
- 點擊開啟 `PublishDashboardWindow` 對話框：
  - 執行檔名稱（預設 = MachineId）
  - 輸出資料夾（預設桌面 `{machineId}-app/`，可瀏覽選擇）
  - app-config.json 路徑（必填，記住上次路徑）
  - DesignPlayer.csproj 路徑（自動從 BaseDirectory 往上搜尋，可手動覆蓋）
- 封裝流程：`dotnet publish --self-contained true /p:PublishReadyToRun=false` → 複製 .machinedesign.json 至 `Config/` → 複製 app-config.json 至 `Config/` → 顯示 streaming log
- 成功後出現「📂 開啟資料夾」按鈕
- 路徑設定持久化至 `%AppData%\Stackdose\Designer\publish-settings.json`
- **Bug 修復（4/20 下午）**：
  - dll 不 rename（apphost binary baked-in 原始 dll 名稱）
  - app-config.json 複製至 `Config/` 子目錄（App 讀取路徑一致）
  - 改 self-contained 解決目標機器缺少 .NET Runtime 問題

**4/20 完成項目（續二）：設計器體驗改善**

- 畫布最小尺寸：Width 400→100 / Height 300→100
- 鍵盤方向鍵移動控件：Arrow=1px、Shift+Arrow=10px（多選同步 + Undo/Redo 整合）

**下一步：待規劃**

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
