# 部署 SOP — DesignPlayer 部署工程師指南

> **適用對象：** 負責部署機台監控系統的工程師  
> **版本：** 2026-04  
> **相關專案：** `Stackdose.App.DesignPlayer`

---

## 1. 系統需求

| 項目 | 需求 |
|---|---|
| 作業系統 | Windows 10 / Windows 11（64-bit） |
| .NET 執行環境 | .NET 8 Desktop Runtime（x64） |
| 螢幕解析度 | 建議 1280×720 以上（依設計稿尺寸） |
| 網路 | 與 PLC 同網段（或點對點連線） |
| PLC | FX3U 系列（Mitsubishi）或相容 Modbus 協定 |

### 1.1 .NET 8 執行環境安裝

若目標機台未安裝 .NET 8：

```
https://dotnet.microsoft.com/download/dotnet/8.0
→ 下載「.NET Desktop Runtime 8.x」（x64）
→ 安裝後重新開機
```

---

## 2. 部署目錄結構

將以下檔案複製到目標機台（建議路徑：`C:\Stackdose\DesignPlayer\`）：

```
DesignPlayer\
├── Stackdose.App.DesignPlayer.exe    ← 主執行檔
├── *.dll                              ← 相依 DLL（全部複製）
├── FeiyangWrapper.dll                 ← PrintHead 原生 DLL（若有用到）
├── Config\
│   ├── app-config.json               ← 主設定檔（必須修改）
│   └── monitor.machinedesign.json    ← 設計稿（從設計師取得）
└── logs\                              ← 日誌目錄（自動建立）
```

---

## 3. 設定 `Config/app-config.json`

這是唯一需要修改的設定檔，所有執行時參數均在此設定。

```json
{
  "appTitle": "機台監控系統",
  "headerDeviceName": "烤箱 #1",
  "loginRequired": false,
  "plc": {
    "ip": "192.168.1.100",
    "port": 3000,
    "pollIntervalMs": 500,
    "autoConnect": true
  },
  "designFile": "Config/monitor.machinedesign.json"
}
```

### 3.1 各參數說明

| 參數 | 說明 | 預設值 |
|---|---|---|
| `appTitle` | 視窗標題 | `Stackdose Monitor` |
| `headerDeviceName` | 頁首顯示的機台名稱 | `MONITOR` |
| `loginRequired` | 是否啟用登入管控 | `false` |
| `plc.ip` | PLC IP 地址 | `192.168.1.100` |
| `plc.port` | PLC TCP 連接埠 | `3000` |
| `plc.pollIntervalMs` | PLC 輪詢間隔（毫秒） | `500` |
| `plc.autoConnect` | 啟動時自動連線 | `true` |
| `designFile` | 設計稿路徑（相對於 exe） | — |

> **提醒：** `designFile` 路徑為相對路徑，相對於 exe 所在目錄。

---

## 4. 設計稿部署

### 4.1 從設計師取得設計稿

設計師交付：
- `monitor.machinedesign.json`（必要）
- `alarms.json`（若有 AlarmViewer）
- `sensors.json`（若有 SensorViewer）
- `monitor.tags-report.txt`（Tags 驗證報告，供工程師確認）

### 4.2 放置設計稿

將 `.machinedesign.json` 複製到 `Config/` 目錄，並確認 `app-config.json` 中 `designFile` 路徑正確。

### 4.3 確認 Tags 驗證報告

開啟 `monitor.tags-report.txt`，確認：
- ✓ 所有 PLC 地址均已在 Tags 定義
- 無 ⚠ 警告（「使用中但未定義 Tag 的地址」）

```
# PLC Tags 使用報告
地址         名稱                     單位     使用中
----------------------------------------------------------
D100         爐溫                     °C       ✓
D101         壓力                     bar      ✓
M0           啟動狀態                          ✓

## 統計
   定義 Tags：3 筆（使用中 3，未使用 0）
   設計稿使用地址：3 筆（全部已對應）
```

若有 ⚠ 警告，請回設計師確認地址是否正確，或補充 Tags 定義後重新存檔。

---

## 5. FeiyangWrapper.dll 安裝（若需 PrintHead 控制）

若設計稿中包含 PrintHeadController，需放置 FeiyangWrapper.dll：

1. 從 SDK 目錄取得：`Sdk/FeiyangWrapper/x64/Release/FeiyangWrapper.dll`
2. 複製到 DesignPlayer.exe **同目錄**
3. 確認 `Sdk/FeiyangSDK-2.3.1/lib/` 中的 SDK DLL 也一併複製

> 若不需要 PrintHead，可略過此步驟。缺少 DLL 時僅 PrintHead 功能失效，其他功能正常。

---

## 6. 啟用登入管控（FDA 21 CFR Part 11）

若客戶需要使用者驗證與稽核日誌，設定 `"loginRequired": true`。

### 6.1 預設管理員帳號

首次啟動時系統自動建立預設管理員：

| 帳號 | 密碼 | 角色 |
|---|---|---|
| `admin` | `admin123` | Administrator |

> **強烈建議**首次登入後立即更改管理員密碼。

### 6.2 建立操作員帳號

1. 以 Administrator 登入
2. 左側導航 → 「使用者管理」
3. 新增使用者，設定角色：

| 角色 | 權限 |
|---|---|
| `Operator` | 執行 SecuredButton、查看日誌 |
| `Supervisor` | Operator + 使用者管理 |
| `Administrator` | 完整系統管理 |

### 6.3 日誌儲存位置

稽核日誌 SQLite 資料庫：`logs/compliance.db`

匯出 PDF：DesignPlayer → 「日誌管理」→ 選擇類型 → 「匯出」

---

## 7. 首次啟動驗證清單

### Step 1：基本啟動

- [ ] 執行 `Stackdose.App.DesignPlayer.exe`
- [ ] 應用程式正常啟動，無崩潰錯誤
- [ ] 頁首顯示正確的 `appTitle` 與 `headerDeviceName`
- [ ] 設計稿畫面正確載入（控制項位置/版面正確）

### Step 2：PLC 連線

- [ ] 頁首 PLC 狀態顯示 🟢 已連線
- [ ] PlcLabel 數值開始更新（每 500ms）
- [ ] PlcStatusIndicator 狀態燈正確反映 PLC 位元

### Step 3：功能驗證

- [ ] 多頁面切換正常（若有多頁）
- [ ] AlarmViewer 顯示正確（若有設定警報）
- [ ] SensorViewer 顯示正確（若有設定感測器）
- [ ] SecuredButton 觸發時寫入 PLC 正確值
- [ ] 日誌面板（LiveLog）有輸出

### Step 4：登入管控（若啟用）

- [ ] 啟動時顯示登入畫面
- [ ] 使用正確帳號登入成功
- [ ] 使用錯誤密碼登入失敗，且記錄到稽核日誌
- [ ] 登入後頁首顯示帳號名稱
- [ ] SecuredButton 點擊後要求確認（依設定）
- [ ] 登出功能正常

### Step 5：斷線重連

- [ ] 拔除 PLC 網路線，頁首顯示 🔴 斷線 / 🟡 重連中
- [ ] 重新接上網路線，系統自動重連（約 5~30 秒）
- [ ] 重連後數值恢復更新

---

## 8. 修改設定（不重新部署）

DesignPlayer Settings 頁面（左側導航 → 「Settings」）可在不修改 JSON 的情況下調整：

- PLC IP / Port / 輪詢間隔
- 設計稿檔案路徑
- 應用程式標題

修改後按「儲存」，系統自動套用（不需重啟）。

---

## 9. JSON 熱更新（設計稿更新流程）

設計師修改設計稿後，**不需重啟 DesignPlayer**：

1. 設計師存檔（`Ctrl+S`），產生新的 `.machinedesign.json`
2. 將新 JSON 複製覆蓋 `Config/` 目錄中的舊檔
3. DesignPlayer 偵測到檔案變更後，約 400~800ms 自動重載畫布
4. 重載時保留當前頁面與登入狀態

---

## 10. 常見問題

**Q: 啟動後立即崩潰？**
A: 確認 .NET 8 Desktop Runtime 已安裝。查看 Windows 事件日誌（`eventvwr.msc`）確認錯誤原因。

**Q: PLC 無法連線（一直顯示 🔴 斷線）？**
A: 確認：(1) PLC IP 設定正確；(2) 防火牆未封鎖 TCP 3000 埠；(3) 在同一台電腦執行 `ping {PLC IP}` 確認網路通；(4) PLC 已上電且通訊功能啟用。

**Q: 設計稿畫面空白或顯示錯誤？**
A: 確認 `app-config.json` 中的 `designFile` 路徑正確，且 JSON 檔案存在。可先用 DesignViewer 開啟該 JSON 確認格式正確。

**Q: AlarmViewer / SensorViewer 顯示空白？**
A: 確認 `alarms.json` / `sensors.json` 存在於**設計稿同目錄**（不是 Config 子目錄）。若使用自訂路徑，確認控制項的 `configFile` 屬性設定正確。

**Q: SecuredButton 點擊後無反應？**
A: 確認 PLC 已連線。若 PLC 連線正常但無反應，開啟 LiveLog 面板查看是否有錯誤訊息。也可確認 `commandAddress` 與 `commandType` 設定正確。

**Q: 熱更新不觸發？**
A: 確認更新的是 `app-config.json` 中 `designFile` 指定的那個 JSON 檔案。若路徑用絕對路徑，確認複製覆蓋的路徑完全一致。

**Q: 日誌 PDF 匯出失敗？**
A: 確認 `logs/` 目錄存在且有寫入權限。若部署在系統目錄（如 `C:\Program Files\`），建議改用使用者目錄或明確設定 `logs/` 路徑。

---

## 11. 升版流程

### 小版本更新（新設計稿）

1. 從設計師取得新的 `.machinedesign.json`
2. 複製覆蓋 `Config/` 目錄
3. 等待熱更新（或重啟應用程式）

### 程式版本更新（新 exe / DLL）

1. 關閉 DesignPlayer
2. 備份 `Config/` 目錄（保留設定與設計稿）
3. 覆蓋所有 `.exe` 與 `.dll` 檔案
4. 恢復 `Config/` 目錄（或確認設定無需變更）
5. 重新啟動並執行「首次啟動驗證清單」第 1~3 步

---

## 附錄：建置執行檔（開發環境）

```powershell
cd Stackdose.UI.Core
dotnet publish Stackdose.App.DesignPlayer/Stackdose.App.DesignPlayer.csproj `
  -c Release -r win-x64 --self-contained false `
  -o publish/DesignPlayer
```

輸出目錄：`publish/DesignPlayer/`，將整個目錄複製到目標機台即可。
