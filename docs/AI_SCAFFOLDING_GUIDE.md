# 🤖 Stackdose AI 專案製作指南 (Scaffolding Spec)

> **核心哲學：** JSON 驅動、腳本先行、零硬編碼。
> 本文件旨在指導 AI 如何自動化地從零構建一個符合 Stackdose 標準的 WPF 設備專案。

## 1. 專案初始化規範 (Scaffolding Phase)

AI 在接到「建立新專案」任務時，應優先執行自動化腳本，而非手動建立目錄。

### 1.1 執行指令範本 (強烈推薦)
使用 `-AutoFullPack` 參數可以一鍵產生包含 Log、Alarm、Sensor 預設佈局的完整專案：

*   **生產線終端 (無邊框全螢幕 - Full Pack):**
    `powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "ProjectName" -Mode Dashboard -AutoFullPack`

*   **標準多頁應用 (含側邊導航):**
    `powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "ProjectName" -Mode Standard -AutoFullPack`

### 1.2 預設環境規範
*   **編碼格式:** 所有產出檔案必須為 **UTF-8 with BOM** (使用 `[System.Text.UTF8Encoding]::new($true)`)。
*   **PLC 預設連線:** `127.0.0.1:3000` (用於初始測試)。
*   **硬體支援:** 若提及「噴頭」或「PrintHead」，必須加上 `-IncludePrintHead` 參數。

---

## 2. 畫布佈局自動化規則 (UI Design Phase)

若不使用 `-AutoFullPack` 而需手動產生 `.machinedesign.json`，AI 應遵循以下視覺規範：

### 2.1 佈局黃金比例 (1280x800 基準)
1.  **System Log (底部):** 
    - `type: "LiveLog"`, `x: 10, y: 590, width: 1260, height: 200`
2.  **Alarm/Sensor Viewer (右側):**
    - `AlarmViewer`: `x: 870, y: 40, width: 400, height: 240`
    - `SensorViewer`: `x: 870, y: 290, width: 400, height: 290`
3.  **Command Area (左側):**
    - 使用 `Spacer` (GroupBox) 作為容器：`x: 10, y: 40, width: 300, height: 540`
4.  **Main Title (中央):**
    - `StaticLabel`: `staticFontSize: 32`

### 2.2 視覺 Token 規範 (嚴禁硬編碼)
AI 在產生任何自訂 XAML 或修改樣式時，必須使用以下定義：
*   **邊框色:** `{DynamicResource Log.Border}`
*   **背景色:** `{DynamicResource Log.Bg.Main}`
*   **標題背景:** `{DynamicResource Log.Bg.Header}`
*   **圓角:** 統一使用 `6` (Viewer/TabPanel) 或 `4` (Button/Spacer)。

---

## 3. AI 任務執行檢查清單 (Self-Checklist)

製作完成後，AI 應確認：
- [ ] **BOM 確認:** 檢查 `Config/*.json` 是否皆有 UTF-8 BOM 頭（解決 VS 中文亂碼）。
- [ ] **Factory 補完:** 確認 `RuntimeControlFactory.cs` 已包含 `viewerTitle` 讀取邏輯。
- [ ] **路徑正確:** `app-config.json` 的 `designFile` 是否指向 `Config/M1.machinedesign.json`。
- [ ] **樣式同步:** 檢查 `Spacer` 是否為半透明亮藍風格 (`#186C8EEF`)。

---

## 4. 範例任務：建立一個噴印機主控台
> 「幫我建立一個名為 ModelF 的 Dashboard 專案，包含噴頭控制功能。」

**AI 應執行的步驟：**
1. `powershell -NoProfile -File .\scripts\new-app.ps1 -AppName "ModelF" -Mode Dashboard -IncludePrintHead -AutoFullPack`
2. 檢查 `ModelF/Config/feiyang_head1.json` 是否正確產生。
3. 提示用戶將波形檔 (.data) 放入 `ModelF/Config/waves/`。
