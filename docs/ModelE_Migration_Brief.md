# ModelE → Stackdose UI.Core 移植簡報

> **目的：** 讓 AI 或工程師能從零自動重建 ModelE WinForms 專案為 WPF Dashboard 應用。
> **參考原始碼：** `D:\工作區\Project\Stackdose.Solution.ModelE`

---

## 1. 專案概況

| 項目 | 原始 WinForms | 目標 WPF |
|---|---|---|
| 框架 | .NET 8 WinForms | Stackdose UI.Core (.NET 8 WPF) |
| 主畫面 | Form1.cs（1680 行） | Dashboard Shell + machinedesign.json |
| 設定驅動 | C# Hardcode + JSON config | 純 JSON 驅動 |
| 噴頭 | 雙噴頭 A/B（Feiyang SDK） | `-IncludePrintHead` scaffold |
| 軸系 | XA/XB/ZA/ZB 四軸顯示 | PlcLabel × 4 |

---

## 2. Scaffold 指令

```powershell
cd D:\工作區\Project\Stackdose.UI.Core
.\scripts\new-app.ps1 -AppName "ModelE" -Mode Dashboard -IncludePrintHead -AutoFullPack
```

產出路徑：`D:\工作區\Project\ModelE\`

---

## 3. 轉換後 Config 檔案

### 3.1 `Config/Machine1.alarms.json`

> 注意：`D2000` 供墨超時 與其他 D900 循環系統_A 用不同暫存器，原始設定如此，需現場確認是否為筆誤。

```json
{
  "alarms": [
    { "group": "循環系統_A", "device": "D2000", "bit": 0,  "operationDescription": "循環系統_A 供墨超時" },
    { "group": "循環系統_A", "device": "D900",  "bit": 1,  "operationDescription": "循環系統_A 液位過高" },
    { "group": "循環系統_A", "device": "D900",  "bit": 2,  "operationDescription": "循環系統_A 頭板通訊斷開" },
    { "group": "循環系統_A", "device": "D900",  "bit": 3,  "operationDescription": "循環系統_A 主板溫度過高" },
    { "group": "循環系統_A", "device": "D900",  "bit": 4,  "operationDescription": "循環系統_A 頭板溫度過高" },
    { "group": "循環系統_A", "device": "D900",  "bit": 5,  "operationDescription": "循環系統_A 壓力過高" },
    { "group": "循環系統_B", "device": "D900",  "bit": 10, "operationDescription": "循環系統_B 供墨超時" },
    { "group": "循環系統_B", "device": "D900",  "bit": 11, "operationDescription": "循環系統_B 液位過高" },
    { "group": "循環系統_B", "device": "D900",  "bit": 12, "operationDescription": "循環系統_B 頭板通訊斷開" },
    { "group": "循環系統_B", "device": "D900",  "bit": 13, "operationDescription": "循環系統_B 主板溫度過高" },
    { "group": "循環系統_B", "device": "D900",  "bit": 14, "operationDescription": "循環系統_B 頭板溫度過高" },
    { "group": "循環系統_B", "device": "D900",  "bit": 15, "operationDescription": "循環系統_B 壓力過高" },
    { "group": "噴頭連線",   "device": "D900",  "bit": 6,  "operationDescription": "噴頭_A 連線異常" },
    { "group": "噴頭連線",   "device": "D901",  "bit": 0,  "operationDescription": "噴頭_B 連線異常" },
    { "group": "軸驅動器",   "device": "D901",  "bit": 4,  "operationDescription": "X軸_A 驅動器異常" },
    { "group": "軸驅動器",   "device": "D901",  "bit": 5,  "operationDescription": "X軸_B 驅動器異常" },
    { "group": "軸驅動器",   "device": "D901",  "bit": 6,  "operationDescription": "Z軸_A 驅動器異常" },
    { "group": "軸驅動器",   "device": "D901",  "bit": 7,  "operationDescription": "Z軸_B 軸驅動器異常" },
    { "group": "原點復歸",   "device": "D901",  "bit": 14, "operationDescription": "X軸_A 原點復歸逾時" },
    { "group": "原點復歸",   "device": "D901",  "bit": 15, "operationDescription": "X軸_B 原點復歸逾時" },
    { "group": "原點復歸",   "device": "D902",  "bit": 0,  "operationDescription": "Z軸_A 原點復歸逾時" },
    { "group": "原點復歸",   "device": "D902",  "bit": 1,  "operationDescription": "Z軸_B 原點復歸逾時" },
    { "group": "軸極限警報", "device": "D902",  "bit": 8,  "operationDescription": "X軸_A L+ Alarm" },
    { "group": "軸極限警報", "device": "D902",  "bit": 9,  "operationDescription": "X軸_A L- Alarm" },
    { "group": "軸極限警報", "device": "D902",  "bit": 10, "operationDescription": "X軸_B L+ Alarm" },
    { "group": "軸極限警報", "device": "D902",  "bit": 11, "operationDescription": "X軸_B L- Alarm" },
    { "group": "軸極限警報", "device": "D902",  "bit": 12, "operationDescription": "Z軸_A L- Alarm" },
    { "group": "軸極限警報", "device": "D902",  "bit": 13, "operationDescription": "Z軸_B L- Alarm" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 1,  "operationDescription": "X軸_A 抬升異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 2,  "operationDescription": "X Axis_A 列印加熱上升異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 3,  "operationDescription": "X Axis_A 列印加熱下降異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 4,  "operationDescription": "X Axis_A 噴頭遮蔽開異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 5,  "operationDescription": "X Axis_A 噴頭遮蔽關異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 6,  "operationDescription": "吸嘴汽缸_A 上升異常" },
    { "group": "氣缸異常_A", "device": "D903",  "bit": 7,  "operationDescription": "吸嘴汽缸_A 下降異常" },
    { "group": "氣缸異常_B", "device": "D903",  "bit": 11, "operationDescription": "X軸_B 抬升異常" },
    { "group": "氣缸異常_B", "device": "D903",  "bit": 12, "operationDescription": "X Axis_B 列印加熱上升異常" },
    { "group": "氣缸異常_B", "device": "D903",  "bit": 13, "operationDescription": "X Axis_B 列印加熱下降異常" },
    { "group": "氣缸異常_B", "device": "D903",  "bit": 14, "operationDescription": "X Axis_B 噴頭遮蔽開異常" },
    { "group": "氣缸異常_B", "device": "D903",  "bit": 15, "operationDescription": "X Axis_B 噴頭遮蔽關異常" },
    { "group": "氣缸異常_B", "device": "D904",  "bit": 0,  "operationDescription": "吸嘴汽缸_B 上升異常" },
    { "group": "氣缸異常_B", "device": "D904",  "bit": 1,  "operationDescription": "吸嘴汽缸_B 下降異常" },
    { "group": "列印區氣缸", "device": "D904",  "bit": 5,  "operationDescription": "列印區治具更換汽缸上升異常" },
    { "group": "列印區氣缸", "device": "D904",  "bit": 6,  "operationDescription": "列印區治具更換汽缸下降異常" },
    { "group": "系統警告",   "device": "D920",  "bit": 0,  "operationDescription": "PC未連接" },
    { "group": "系統警告",   "device": "D920",  "bit": 1,  "operationDescription": "尚未進行初始化" },
    { "group": "系統警告",   "device": "D920",  "bit": 2,  "operationDescription": "尚未進行供墨循環" },
    { "group": "系統警告",   "device": "D920",  "bit": 3,  "operationDescription": "Z_A 盤面治具未安裝" },
    { "group": "系統警告",   "device": "D920",  "bit": 4,  "operationDescription": "Z_B 盤面治具未安裝" },
    { "group": "系統警告",   "device": "D920",  "bit": 5,  "operationDescription": "X_A 粉槽未安裝" },
    { "group": "系統警告",   "device": "D920",  "bit": 6,  "operationDescription": "X_B 粉槽未安裝" },
    { "group": "系統警告",   "device": "D920",  "bit": 7,  "operationDescription": "X_A 藥粉量不足" },
    { "group": "系統警告",   "device": "D920",  "bit": 8,  "operationDescription": "X_B 藥粉量不足" },
    { "group": "系統警告",   "device": "D920",  "bit": 9,  "operationDescription": "X_A 墨量不足" },
    { "group": "系統警告",   "device": "D920",  "bit": 10, "operationDescription": "X_B 墨量不足" },
    { "group": "聯鎖警告",   "device": "D921",  "bit": 3,  "operationDescription": "X軸_A 移動 InterLock" },
    { "group": "聯鎖警告",   "device": "D921",  "bit": 4,  "operationDescription": "X軸_B 移動 InterLock" },
    { "group": "聯鎖警告",   "device": "D921",  "bit": 5,  "operationDescription": "吸嘴_A 上升 InterLock" },
    { "group": "聯鎖警告",   "device": "D921",  "bit": 6,  "operationDescription": "吸嘴_B 上升 InterLock" }
  ]
}
```

### 3.2 `Config/Machine1.sensors.json`

> 格式與框架 `SensorConfig` 相容，直接 lowercase key 即可。

```json
[
  { "group": "粉槽狀態",       "device": "D90", "bit": "0,1", "value": "1,1", "mode": "OR",  "operationDescription": "粉槽_A有粉" },
  { "group": "粉槽狀態",       "device": "D90", "bit": "2,3", "value": "1,1", "mode": "OR",  "operationDescription": "粉槽_B有粉" },
  { "group": "列印區狀態",     "device": "D91", "bit": "0,1", "value": "1,1", "mode": "AND", "operationDescription": "列印區_A 治具在席" },
  { "group": "列印區狀態",     "device": "D91", "bit": "2,3", "value": "1,1", "mode": "AND", "operationDescription": "列印區_B 治具在席" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "0",   "value": "1",   "mode": "AND", "operationDescription": "第二墨盒液位_A" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "1",   "value": "1",   "mode": "AND", "operationDescription": "第二墨盒液位_B" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "2",   "value": "1",   "mode": "AND", "operationDescription": "供墨幫浦_A" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "3",   "value": "1",   "mode": "AND", "operationDescription": "供墨幫浦_B" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "4",   "value": "1",   "mode": "AND", "operationDescription": "排墨幫浦_A" },
  { "group": "循環供墨系統",   "device": "D94", "bit": "5",   "value": "1",   "mode": "AND", "operationDescription": "排墨幫浦_B" }
]
```

---

## 4. 畫布佈局建議（`Config/M1.machinedesign.json` 骨架，1280×800）

| 元件 | type | x | y | width | height | 備註 |
|---|---|---|---|---|---|---|
| 主標題 | `StaticLabel` | 390 | 8 | 500 | 30 | `staticFontSize: 24`, 文字「ModelE 噴印機控制台」 |
| 指令區 | `Spacer` | 10 | 40 | 280 | 540 | GroupBox 容器，含以下按鈕 |
| 初始化 A | `SecuredButton` | 20 | 60 | 120 | 36 | `plcWrite: M1` |
| 初始化 B | `SecuredButton` | 150 | 60 | 120 | 36 | `plcWrite: M3` |
| 停止 | `SecuredButton` | 20 | 106 | 250 | 36 | `plcWrite: M2`，`theme: Danger` |
| 啟動列印 | `SecuredButton` | 20 | 152 | 250 | 36 | `plcWrite: M9`，`theme: Success` |
| 噴頭A狀態 | `PrintHeadStatus` | 300 | 40 | 270 | 120 | `headIndex: 0` |
| 噴頭B狀態 | `PrintHeadStatus` | 580 | 40 | 270 | 120 | `headIndex: 1` |
| X_A 位置 | `PlcLabel` | 300 | 170 | 130 | 36 | `address: D65`, `dataType: DWord` |
| X_B 位置 | `PlcLabel` | 440 | 170 | 130 | 36 | `address: D67`, `dataType: DWord` |
| Z_A 位置 | `PlcLabel` | 580 | 170 | 130 | 36 | `address: D69`, `dataType: DWord` |
| Z_B 位置 | `PlcLabel` | 720 | 170 | 130 | 36 | `address: D71`, `dataType: DWord` |
| AlarmViewer | `AlarmViewer` | 860 | 40 | 410 | 240 | `configFile: Machine1.alarms.json` |
| SensorViewer | `SensorViewer` | 860 | 290 | 410 | 290 | `configFile: Machine1.sensors.json` |
| LiveLog | `LiveLog` | 10 | 590 | 1260 | 200 | |

> D65-D68 為 XA/XB/ZA/ZB 軸位置暫存器（原始 Form1.cs 中讀取）。請現場確認實際 D 號。

---

## 5. PLC 位址速查表

### 指令 Coil（M 繼電器）

| 功能 | 位址 | 說明 |
|---|---|---|
| 初始化_A | M1 | X_A 軸初始化 |
| 停止_A | M2 | 停止 A 軸 |
| 初始化_B | M3 | X_B 軸初始化 |
| 停止_B | M4 | 停止 B 軸 |
| X軸移動 | M5-M8 | 軸點動控制 |
| 啟動列印 | M9 | 開始列印循環 |
| 噴噴（Spit） | M10-M13 | 噴頭 A/B 噴噴操作 |
| 清洗（Wash） | M14-M17 | 噴頭 A/B 清洗 |
| 供墨循環 | M18-M23 | 循環供墨控制 |

### 狀態暫存器（D）

| 用途 | 位址 | 說明 |
|---|---|---|
| 軸位置 | D65, D67, D69, D71 | XA, XB, ZA, ZB（各佔 DWord = 2 暫存器） |
| 感測器狀態 | D90, D91, D94 | 粉槽/列印區/供墨系統 |
| 異常旗標 | D900–D904 | 16-bit 位元旗標 |
| 警告旗標 | D920–D921 | 16-bit 位元旗標 |

---

## 6. 移植工作分項

### AI 可自動產生（scaffold 後直接替換）

- [x] `Config/Machine1.alarms.json` — 本文第 3.1 節，57 筆完整轉換
- [x] `Config/Machine1.sensors.json` — 本文第 3.2 節，10 筆直接轉換
- [x] `Config/M1.machinedesign.json` — 依第 4 節佈局產生
- [x] `Config/app-config.json` — scaffold 自動產生（PLC IP 設為 127.0.0.1:3000）

### 需人工確認

| 項目 | 說明 |
|---|---|
| 軸位址已確認 | D65/D67/D69/D71，DWord 模式，已寫入 machinedesign.json ✅ |
| D2000 供墨超時 | 與 D900 循環系統_A 異暫存器，疑似原始設定錯誤 |
| PrintHead 波形檔 | 放入 `ModelE/Config/waves/*.data` |
| PLC IP | 生產環境實際 IP，替換 app-config.json 中的 127.0.0.1 |

### 原始功能現已不需要

| 原始功能 | 原因 |
|---|---|
| `PlcMessageForm`（倒數確認對話框） | 使用 `SecuredButton` 的 confirm 機制取代；視需求加回 |
| `AxisControl` UserControl | PlcLabel + suffix 已足夠顯示 X/Y/Z 數值 |
| WinForms 佈局程式碼 | 全由 machinedesign.json 取代 |

---

## 7. 給 AI 的執行步驟

```
1. 執行 scaffold 指令（第 2 節）
   → 驗收：ModelE/ 目錄存在，含 Config/ 與 RuntimeControlFactory.cs

2. 替換 Config/Machine1.alarms.json（第 3.1 節完整內容）
   → 驗收：執行 app 後 AlarmViewer 顯示 11 個群組

3. 替換 Config/Machine1.sensors.json（第 3.2 節完整內容）
   → 驗收：SensorViewer 顯示 3 個群組、10 筆感測器

4. 產生或替換 Config/M1.machinedesign.json（第 4 節佈局）
   → 驗收：DesignViewer 拖入 JSON 能看到完整佈局

5. 確認 Config/app-config.json 的 designFile 指向正確路徑
   → 驗收：DesignRuntime 能載入畫面

6. 現場確認 D65-D68 位址後，更新 machinedesign.json 中 PlcLabel 的 plcDevice
```
