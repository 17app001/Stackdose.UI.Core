# Stackdose.UI.Core

> 讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 稽核要求的監控介面。

---

## 這在解決什麼問題？

工業設備廠商在開發 UI 時面臨三個痛點：

| 痛點 | 傳統做法的問題 |
|---|---|
| PLC 整合 | 每台設備地址不同，每次都要改程式碼，曠日費時 |
| FDA 21 CFR Part 11 合規 | 操作日誌、使用者驗證、稽核追蹤需要額外開發，容易遺漏 |
| 介面維護 | 改版型要找工程師，工程師不懂設計工具，往返溝通成本高 |

**Stackdose.UI.Core 把這三件事都解決了。**

---

## 核心工作流程

```
設計師                    工程師                    現場部署
   │                        │                         │
   ▼                        ▼                         ▼
MachinePageDesigner  →  DeviceFramework  →       DesignPlayer
（類 Unity 拖曳介面）     （JSON 設定複雜邏輯）    （量產 Shell App）
拖曳 PLC 控制項            綁定感測器 / 警報          讀 JSON 直接部署
設定 PLC 地址              自訂操作流程               無需改程式碼
輸出 .machinedesign.json                             符合 FDA 稽核
```

---

## 適用場景

### 場景 A：設計師主導（簡單監控頁面）
1. 開啟 **MachinePageDesigner**（操作介面類似 Unity Scene Editor）
2. 從左側工具箱拖曳控制項（PlcLabel、PlcStatusIndicator、SecuredButton…）
3. 在屬性面板填入 PLC 地址（如 `D100`、`M200`）
4. 支援**單頁**或**多頁面**佈局（頁籤切換）
5. Ctrl+S 儲存 → `.machinedesign.json`
6. 部署 **DesignPlayer**，指向該 JSON 檔，上線完成

### 場景 B：工程師主導（複雜設備邏輯）
1. 使用 **ProjectGeneratorUI** 一鍵產生新設備專案框架
2. 在 `device-config.json` 設定 PLC 地址、感測器、警報規則
3. 基於 **DeviceFramework** 加入客製化模組
4. 全程使用 JSON 設定，不用動 XAML

### 場景 C：上線前驗證
- 開啟 **DesignRuntime**，輸入 PLC IP，拖入 JSON 檔
- 即時看到真實 PLC 數值，確認控制項地址正確
- 修改設計 → 儲存 → 畫面自動熱更新（無需重啟）

---

## 專案結構

```
Stackdose.UI.Core/
│
├── 核心庫（穩定）
│   ├── Stackdose.UI.Core/           26 個 WPF 自訂控制項、Context 系統、SQLite 日誌
│   ├── Stackdose.UI.Templates/      AppHeader、LeftNavigation、AppBottomBar Shell 布局
│   └── Stackdose.App.ShellShared/   多 App 共用 Shell 服務
│
├── 框架與應用
│   ├── Stackdose.App.DeviceFramework/   JSON 驅動設備 App 組裝框架（核心產品）
│   ├── Stackdose.App.UbiDemo/           UBI 烤箱完整範例
│   ├── Stackdose.App.DesignRuntime/     開發驗證工具（PLC 連線 + JSON 熱更新）
│   └── Stackdose.App.DesignPlayer/      量產交付 Shell App
│
├── 設計器工具
│   ├── Stackdose.Tools.MachinePageDesigner/   視覺化拖曳設計器（主力開發中）
│   ├── Stackdose.Tools.DesignViewer/           靜態 JSON 預覽工具
│   ├── Stackdose.Tools.ProjectGenerator/       CLI 專案產生器
│   └── Stackdose.Tools.ProjectGeneratorUI/     GUI 專案產生器
│
└── 外部依賴
    ├── ../Stackdose.Platform/           IPlcManager、IPlcClient、ILogService 等介面
    └── ../../Sdk/FeiyangWrapper/        Feiyang PrintHead C++ SDK（條件引用）
```

---

## MachinePageDesigner 支援的控制項

| 控制項 | 分類 | 說明 |
|---|---|---|
| `PlcLabel` | PLC | 數值顯示，支援色彩主題 / 框形 / 除數換算 |
| `PlcText` | PLC | PLC 文字輸入顯示 |
| `PlcStatusIndicator` | PLC | 位元狀態指示燈（M/D 位址） |
| `SecuredButton` | 操作 | 需使用者權限驗證的操作按鈕 |
| `StaticLabel` | 版面 | 靜態文字標題 / 說明文字 |
| `Spacer (GroupBox)` | 版面 | 群組化框線 |
| `LiveLog` | 檢視 | 即時系統操作日誌 |
| `AlarmViewer` | 檢視 | 警報記錄列表 |
| `SensorViewer` | 檢視 | 感測器數值列表 |

---

## FDA 21 CFR Part 11 合規

所有應用程式內建：
- **操作日誌**：每個使用者操作自動寫入 SQLite，不可竄改
- **使用者驗證**：6 等級權限（Admin / Manager / Engineer / Operator / Viewer / Guest）
- **稽核追蹤**：時間戳、操作者、操作內容全記錄
- **`ComplianceContext`**：統一日誌介面，所有 App 共用

---

## 技術規格

- **.NET 8** Windows-only（x64）
- **WPF**（XAML + MVVM）
- **PLC 通訊**：Mitsubishi FX3U 系列（Ethernet，Port 3000）
- **JSON 格式**：`.machinedesign.json`（設計稿）、`app-config.json`（執行設定）
- **外部依賴**：`Stackdose.Platform`（Platform Repo，原始碼引用）

---

## 快速開始

```bash
# 開啟設計器
Stackdose.Designer.sln → Startup: MachinePageDesigner

# 開啟量產 App
Stackdose.Designer.sln → Startup: DesignPlayer

# 開發驗證
Stackdose.Designer.sln → Startup: DesignRuntime

# 完整框架開發
Stackdose.UI.Core.sln → Startup: UbiDemo
```

詳細說明請參考 [`docs/kb/`](docs/kb/) 知識庫。

---

*更新：2026-04-16*
