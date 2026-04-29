# 接手指引 — 2026-04-24 噴頭整合交接

> 如果你剛接手這個分支，從這份開始讀。

---

## 當前進度摘要

1. **PrintHead 重構完成**：已將全系統 `dynamic` 替換為 `IPrintHead` 介面，並實作了傳圖進度回報與 Spit 邏輯集中化。
2. **設計器整合完成**：`MachinePageDesigner` 現在支援拖曳 `PrintHeadStatus` 與 `PrintHeadController` 並設定完整參數。
3. **自動化腳本穩固**：`new-app.ps1` 支援 `-IncludePrintHead` 參數，能產出標準的雙 IP 設定檔與 M1 佈局。

---

## 2026-04-25 待辦項目 (CRITICAL)

### 1. 修正 Viewer JSON 對應
- **現狀**：新產出的 App（如 MyApp）雖然包含 `Machine1.sensors.json` 等檔案，但 Viewer 控制項在 Runtime 有時無法正確讀取。
- **任務**：檢查 `RuntimeControlFactory` 與 `init-shell-app.ps1` 產出的代碼，確保 `ConfigFile` 路徑解析與 `AppDomain.CurrentDomain.BaseDirectory` 保持一致。

### 2. 噴頭硬體 DLL 整合
- **現狀**：系統報錯缺少 **`FeiyangWrapper.dll`**。
- **任務**：
  - 確認 DLL 的實體位置（通常應在 `../../Sdk/FeiyangWrapper/x64/Debug/`）。
  - 修改 `init-shell-app.ps1` 中的 `csproj` 生成範本，加入 `<None Update="...">` 確保該 DLL 與其依賴的 `NJCSC.dll` 會被自動複製到輸出目錄。

### 3. 多噴頭自動連線測試
- 在 `Dashboard` 模式下測試多個 `PrintHeadStatus` 勾選 `AutoConnect` 時的啟動穩定性（建議加入非同步 Delay 錯開連線時間）。

---

## 分支資訊
- **當前分支**：`feature/printhead-robustness`
- **遠端倉庫**：已 PUSH 至 GitHub (commit ef50f45 & 7a76f56)

---

## 鐵律
- **編碼**：所有 `.ps1` 指令輸出與檔案必須保持 **UTF-8 with BOM**。
- **日誌**：必須透過 `ComplianceContext` 記錄所有硬體動作。
