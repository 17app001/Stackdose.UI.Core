# 專案進度簡報

> 適用對象：技術主管快速了解現況
> 更新日期：2026-04-15

---

## 這個專案是什麼

**Stackdose.UI.Core** 是一套工業設備操作介面開發框架。
目標：讓設備廠商不用從零開始寫 UI，只要設定 JSON 就能快速上線符合 FDA 合規要求的設備介面。

```
傳統做法：每台設備重新開發 UI  →  3~6 個月
使用本框架：設定 JSON + 視覺拖曳設計  →  數天
```

---

## 整體架構（一張圖）

```
┌─────────────────────────────────────────────────────────────┐
│                    設備廠商使用層                             │
│                                                             │
│   ProjectGeneratorUI          MachinePageDesigner           │
│   （一鍵產生新設備專案）       （視覺化拖曳設計介面）           │
│           ↓                           ↓                     │
│           └──────────── JSON 設定檔 ──┘                     │
│                              ↓                              │
│              DeviceFramework（組裝框架）                     │
│     RuntimeHost → DeviceContextMapper → DynamicDevicePage  │
│                              ↓                              │
├──────────────────────────────────────────────────────────── ┤
│                    核心基礎層                                 │
│   UI.Core：控制項 + 安全 + 日誌 + 主題                       │
│   UI.Templates：Shell 導航布局                               │
├──────────────────────────────────────────────────────────── ┤
│                    硬體驅動層（Platform）                     │
│   IPlcManager（Mitsubishi FX3U）  IPrintHead（Feiyang）      │
└─────────────────────────────────────────────────────────────┘
```

---

## 目前完成功能

### 核心框架（穩定可用）

| 功能 | 說明 |
|---|---|
| PLC 通訊 | Mitsubishi FX3U，支援讀寫、批次掃描、自動重連 |
| FDA 合規日誌 | 稽核軌跡 / 操作記錄 / 事件日誌，非同步批次寫入 SQLite |
| 使用者權限 | 6 等級（Guest → SuperAdmin），支援 AD 驗證 |
| JSON 驅動 UI | 設定 IP / Tags / Commands → 自動生成設備頁面，零程式碼 |
| 動態主題 | Dark / Light 主題切換，語意 Token 系統 |
| Shell 導航 | Overview / Detail / Log / User / Settings 標準頁面 |
| 列印頭控制 | Feiyang PrintHead SDK 整合 |
| 一鍵產生專案 | CLI + GUI 工具，含 CSV Spec 轉換 |

### 視覺化設計器系統（主力開發中）

**設計流程：**

```
MachinePageDesigner  ──輸出→  .machinedesign.json  ──載入→  DesignRuntime
（拖曳設計）                  （設計描述檔）               （真實PLC執行）
                                     ↓
                              DesignViewer
                              （即時預覽，不需PLC）
```

**MachinePageDesigner 目前已完成：**

| 功能 | 狀態 |
|---|---|
| 自由畫布拖曳定位 | ✅ |
| Snap 格線對齊 | ✅ |
| 控制項 Z-Order 調整 | ✅ |
| 框選多選 | ✅ |
| 控制項鎖定 | ✅ |
| 複製 / 貼上 | ✅ |
| GroupBox 群組化 | ✅ |
| 對齊 / 均等分配 | ✅ |
| 畫布尺寸設定 | ✅ |
| Undo / Redo | ✅ |
| 儲存 / 載入 JSON | ✅ |

---

## 開發時間軸（2026 年）

```
3月上旬   建立 DeviceFramework，UbiDemo 遷移至框架架構
3月下旬   新增 ProjectGeneratorUI（GUI 版專案產生器）
          Module 系統、DataEvent 監聽、LiveLog 過濾
4月01日   抽出 PlcDataGridPanel 通用元件
          MachinePageDesigner 專案初始化（Zone制）
4月08日   切換為自由畫布（FreeCanvas）模式，完成拖曳基礎功能
4月13日   ★ 自由畫布全功能整合
          新增 DesignViewer（即時預覽工具）
          新增 DesignRuntime（真實 PLC 連線執行環境）
4月15日   DesignRuntime 新增模擬器模式 + 亂數測試注入功能
          文件系統整理（知識庫建立）
```

---

## 接下來的方向

**近期（進行中）：**
1. **DesignRuntime 完善** — PLC 連線穩定性、JSON 熱更新
2. **設計器 ↔ 執行環境整合** — 設計完直接在 DesignRuntime 驗證

**中期目標：**
3. 更多控制項支援（趨勢圖、報警棒狀圖等）
4. 多頁面設計支援

**長期方向：**
5. 客戶端工具包裝（讓終端設備廠商可自行調整介面）

---

## 專案現況數字

| 項目 | 數量 |
|---|---|
| 方案內專案數 | 11 個（含 3 個外部 Platform 引用） |
| 自定義 WPF 控制項 | 26+ 個 |
| 程式碼 commit 數 | 50+ 個（2025/11 起） |
| 技術棧 | .NET 8 / WPF / SQLite / Mitsubishi PLC |
