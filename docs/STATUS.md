# docs/STATUS.md — 功能完成狀態總覽

> 這份文件是功能歷史的單一來源。  
> 開發日誌（每日細節）見 `docs/devlog/2026-04.md`

---

## 核心設計器功能

### MachinePageDesigner ✅
- 自由畫布（FreeCanvas）：Snap、Z-Order、框選多選、對齊分配
- 右鍵 ContextMenu：複製/貼上/刪除/鎖定/Z-Order
- GroupBox 群組化、StaticLabel 靜態文字
- Undo/Redo per page（每頁獨立）
- **多頁面支援**（v2.0 JSON：`pages[]` 陣列 + 頁籤列 + 獨立 UndoRedo/Canvas）
- 所有 9 種控制項完整屬性面板
- PLC Tags 管理（地址↔名稱對照表，Toolbar「📌 Tags」入口）
- Tags 使用報告（存檔自動輸出 `{name}.tags-report.txt`）

### DesignPlayer ✅
- 多頁面切換（頁籤導航 + v2.0 JSON 格式）
- 真實 PLC 連線驗證（192.168.22.39:3000 ✓）
- Settings 頁面（免 JSON 修改 PLC 設定）
- JSON Hot-Reload（FileSystemWatcher，儲存後自動重載）
- 登入管控（SecurityContext 整合）
- 修復 StartupUri 崩潰 bug

### DesignRuntime ✅
- PLC 連線、模擬器、熱更新、斷線重連
- 多頁面切換（pages 陣列 + 頁籤列）
- Tags 狀態列（右下角：`✓ N Tags 全部對應` / `⚠ N 未對應`）

### DesignViewer ✅
- 拖入 JSON 靜態預覽
- 多頁面切換

---

## 功能缺口補強（全部完成）

| 項目 | 說明 |
|---|---|
| **AlarmViewer** | PropertyPanel 行內編輯器（新增/刪除/修改），存檔自動輸出 `alarms.json` |
| **SensorViewer** | 同 AlarmViewer 架構，支援 AND / OR / COMPARE 三種模式設定，自動輸出 `sensors.json` |
| **SecuredButton** | 新增 `writeValue`、`commandType`（write / pulse / toggle / sequence）、`pulseMs`。PropertyPanel 完整 UI |
| **控制項模板庫** | 9 個內建模板 + 自訂模板 CRUD（存於 `%LOCALAPPDATA%`）；ToolboxPanel 雙頁籤；分類篩選 + 搜尋；拖曳放置自動展開 |
| **Command Sequence DSL** | JSON 定義多步驟 PLC 指令，DesignPlayer / DesignRuntime 均可執行（見下方格式） |

---

## JSON 格式快速參考

### alarms.json
```json
{ "alarms": [
  { "group": "馬達", "device": "D200", "bit": 0, "operationDescription": "馬達過載" }
] }
```

### sensors.json（扁平陣列）
```json
[
  { "group": "粉槽狀態", "device": "D90", "bit": "2,3", "value": "0,0", "mode": "AND", "operationDescription": "粉槽_B無粉" }
]
```

### Command Sequence DSL（SecuredButton `commandType: sequence`）
```json
{
  "steps": [
    { "type": "write", "address": "D100", "value": 1 },
    { "type": "wait", "ms": 500 },
    { "type": "read", "address": "D101", "variable": "status" },
    { "type": "conditional", "variable": "status", "operator": "==", "value": 1,
      "then": [{ "type": "write", "address": "D102", "value": 1 }],
      "else": [{ "type": "write", "address": "D103", "value": 0 }] },
    { "type": "readWait", "address": "D104", "expected": 1, "timeoutMs": 5000, "pollIntervalMs": 200 }
  ],
  "rollback": [{ "type": "write", "address": "D100", "value": 0 }],
  "onError": "rollback"
}
```

步驟類型：`write` / `read`（存變數）/ `wait` / `conditional` / `readWait`（輪詢等待）  
`onError` 策略：`rollback`（執行回滾）/ `stop` / `continue`

---

## 架構優化（P0/P1/P2，2026-04）

| 優先 | 項目 | 說明 |
|---|---|---|
| P0 | SecurityContext 非同步化 | `Login` → `LoginAsync`，消除 `.Result` UI 阻塞 |
| P0 | ComplianceContext 強化 | `IsInitialized` 旗標 + EventLog 失敗寫入 |
| P1 | BaseRuntimeControlFactory | DesignPlayer/Runtime 共用 ~300 行工廠程式碼，各精簡至 14 行 |
| P1 | 共用 PLC 輪詢 Timer | BitIndicator：N 個 DispatcherTimer → 1 個 500ms 共用 Timer |
| P1 | 日誌匯出實作 | LogManagementPanel：Operation / Event / PeriodicData 三組匯出 |
| P2 | ViewModel 精簡 | DesignerItemViewModel CommitStr/CommitDbl 輔助（543→467 行，-14%） |
| P2 | ConfigureAwait(false) | Services/Helpers 58 處非 UI async 加入 ConfigureAwait(false) |
| P2 | 測試 SQLite 對齊 | 9.0.9 → 10.0.1（與主專案一致） |

---

## 控制項清單（26 個）

詳細參數見 `docs/kb/controls-reference.md`

| 分類 | 控制項 |
|---|---|
| PLC 資料顯示 | PlcLabel、PlcText、PlcStatusIndicator、BitIndicator |
| PLC 操作 | SecuredButton |
| 日誌 / 稽核 | LiveLog、LogManagementPanel、AlarmViewer、SensorViewer |
| 版面 | GroupBox、Spacer、StaticLabel |
| 進階 | CyberFrame、CyberTabControl、PrintHeadStatus（+ 其他 14 個） |
