# ModelE WinForms vs Stackdose.UI.Core WPF 功能對照

> 產出日期：2026-04-30
> 目的：評估現有 WPF 框架能否替代手工撰寫的 WinForms 版本，並標出差距與未來實作方向。

---

## 一、ModelE 功能清單（WinForms 基準）

| 模組 | 功能細項 |
|---|---|
| **PLC 通訊** | TCP 連線（IP/Port 可設）、位元/字元/DWord 讀寫、事件偵測（M1-M238）、即時掃描輪詢 |
| **噴頭控制** | 雙噴頭（Area A/B）、X/Z 軸位置、溫度/墨水監測、閃噴（頻率/工作/閒置時間/墨滴數）、校正參數（StartX、CaliMM） |
| **設備管理** | 初始化、清潔、供墨、校正指令、噴頭連線/斷線 |
| **列印工作流** | JSON 配方驅動、層數×圖像映射、進度追蹤、倒計時對話框確認 |
| **Sensor 監控** | 陣列偵測（事件驅動）、門檻確認對話框 |
| **日誌 & 警報** | CSV + SQLite 三層日誌、JSON 警報定義、即時 RichTextBox 顯示、狀態追蹤 |
| **設定檔** | `settings.json` / `plc_event_config.json` / `plc_alarm_config.json` / `plc_sensor_config.json` / `multi_head_profile.json` / `recipe-1.json` |
| **安全性** | 登入驗證（admin 帳號）、UI 執行緒保護 |
| **UI 主題** | MahApps.Metro 深色主題（固定，不可切換） |

---

## 二、WPF 框架對應能力

| 功能 | WPF 框架狀態 | 對應元件 |
|---|---|---|
| PLC TCP 連線 | ✅ 完整 | `PlcManager` / `PlcStatus` / `PlcLabel` |
| 位元/字元讀寫 | ✅ 完整 | `IPlcClient` (Stackdose.Hardware) |
| 事件偵測 | ✅ 完整 | `IPlcMonitor` 輪詢差異通知 |
| 噴頭狀態顯示 | ✅ 完整 | `PrintHeadStatus`（溫度/電壓/Encoder/Index） |
| 噴頭控制 UI | ✅ 完整 | `PrintHeadController`（閃噴/校正/連線） |
| 設備初始化/清潔 | ✅ 基礎 | `PrintHeadController` 指令按鈕 |
| Sensor 監控 | ✅ 有控件 | `SensorStatusPanel` |
| 警報顯示 | ✅ 有控件 | `AlarmViewer` |
| 日誌（SQLite） | ✅ 完整 | `SQLiteLogger` / `LiveLog` 控件 |
| JSON 設定驅動 | ✅ 完整 | DeviceFramework JSON 組裝架構 |
| 登入驗證 | ✅ 完整 | `ComplianceContext` / `LoginDialog` |
| Dark/Light 主題切換 | ✅ 完整（2026-04-30） | `ThemeManager.SwitchTheme()` |
| 列印工作流（配方） | ⚠️ 部分 | `PrintHeadController` 有圖片載入/任務送出，但**無配方 JSON 層數映射** |
| 進度條（傳圖） | ⚠️ 已實作，未壓測 | `PrintHeadController` 進度條 |
| Sensor 門檻對話框 | ❌ 缺 | 無 |
| 倒計時確認對話框 | ❌ 缺 | 無 |
| X/Z 軸位置控制 UI | ❌ 缺 | 無獨立軸控控件 |
| 雙噴頭獨立 A/B 區域 | ⚠️ 單頭架構，需擴展 | `PrintHeadStatus` / `PrintHeadController` 各一實例 |

---

## 三、差距摘要

### 視需求（目前未決定）
| 項目 | 說明 |
|---|---|
| **倒計時確認對話框** | 操作員在倒計時內確認才繼續流程（`PlcMessageForm` 對應功能）— 考量中 |
| **Sensor 門檻確認** | 偵測到特定 Sensor 狀態時彈出確認對話框阻擋流程 — 視需求 |
| ~~配方工作流引擎~~ | ~~recipe.json 層數映射~~ — **不需要** |

### 中優先（完整度不足）
| 項目 | 說明 |
|---|---|
| **X/Z 軸位置控制** | 目前無獨立的軸控 UI 控件（位置數值輸入 + PLC 寫入） |
| **雙噴頭 A/B 區域** | 框架支援多實例，但 JSON 設定與 UI 佈局尚未驗證雙頭並排 |
| **傳圖進度條壓力測試** | 大檔案情境未驗證 |

### 低優先（ModelE 有，框架不需要）
| 項目 | 說明 |
|---|---|
| 硬編碼登入 | ModelE 用硬編碼，框架已有完整 `ComplianceContext`，更好 |
| 固定深色主題 | ModelE 依賴 MahApps，框架已支援 Dark/Light 切換 |

---

## 四、未來實作方向

### Phase 1：對話框補齊（視需求）
```
- PlcMessageDialog UserControl（倒計時 + 確認/取消）— 考量中，未決定
- SensorGateDialog（等待 Sensor 狀態滿足條件才允許繼續）— 視需求
- RecipeEngine：不需要（框架走 JSON 設計器路線，不做配方引擎）
```

### Phase 2：軸控控件
```
1. AxisControl UserControl（目標位置輸入 + 移動按鈕 + 目前位置顯示）
2. 對應 PLC address 設定
```

### Phase 3：雙噴頭驗證
```
1. 用 MachinePageDesigner 建立雙 PrintHeadStatus + 雙 PrintHeadController 佈局
2. 確認 JSON 設定中兩個噴頭各自對應正確 IP/Port
3. 實機並排測試
```

---

## 五、結論

> **WPF 框架已覆蓋 ModelE 約 75% 的功能**，且在主題切換、FDA 稽核日誌、設計器可視化等面向顯著超越。
> RecipeEngine 不列入計畫；PlcMessageDialog 考量中。目前最接近的缺口是 **X/Z 軸控控件** 與**雙噴頭佈局驗證**。
