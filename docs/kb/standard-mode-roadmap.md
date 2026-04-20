# Standard 模式封裝 — 完整規劃

> 目標：讓設計師能封裝出符合 FDA 21 CFR Part 11 要求的完整機台介面 App，
> 包含 Shell UI（Header / LeftNav / BottomBar）、登入管控、使用者權限、稽核日誌。

---

## 兩種封裝模式對比

| 項目 | Dashboard 模式（現有） | Standard 模式（本規劃） |
|---|---|---|
| 視窗外觀 | 無邊框小視窗，固定尺寸 | 一般視窗，可最大化 |
| Shell UI | 無（僅 32px TopBar） | 完整 AppHeader + LeftNav + BottomBar |
| 頁面導航 | 無（單一 Canvas） | LeftNav 自動對應 pages[] |
| 登入管控 | 無 | loginRequired 控制，整合 SecurityContext |
| 存取層級 | 無 | 每頁 requiredLevel（Guest / Operator / Supervisor / Admin） |
| 稽核日誌 | 無 | ComplianceContext SQLite + 日誌查看頁 |
| 使用者管理 | 無 | 內建 User Management 頁（Operator+） |
| 適用場景 | 數值監控面板 | 完整機台操作介面，客戶需 FDA 稽核 |

---

## Phase 1 — 打通基礎（Day 1，明天）

**目標：Standard 模式設計稿可封裝、部署、正常運行。**

### 1-1 封裝工具解鎖

- `PublishDashboardCmd` 條件：移除 `LayoutMode == "Dashboard"` 限制 → 改為「已儲存即可封裝」
- 封裝視窗標題改為「📦 封裝 App」（不再只是 Dashboard）
- 封裝 log 新增 layout mode 提示

### 1-2 DesignPlayer — Pages 自動對應 LeftNav

目前 LeftNav 硬編碼 3 個項目（Main View / Settings / User Management）。
Standard 模式改為：從 `machinedesign.json` 的 `pages[]` 自動產生 NavigationItems。

```
pages[0].name = "主畫面"  →  NavigationItem { Title="主畫面", Target="page:0", RequiredLevel=Guest }
pages[1].name = "參數設定" →  NavigationItem { Title="參數設定", Target="page:1", RequiredLevel=Guest }
固定項目（後段）：Settings（Operator+）、User Management（Operator+）
```

### 1-3 app-config.json 新增欄位

```json
{
  "layoutMode": "Standard",
  "loginRequired": true,
  "headerDeviceName": "M1",
  ...
}
```

DesignPlayer 啟動時根據 `layoutMode` 決定是否走 Dashboard 流程（現有）或 Standard 流程。

### 1-4 驗收標準

- [ ] Standard 模式設計稿可從設計器觸發封裝
- [ ] 封裝後 `M1.exe` 啟動顯示完整 Shell
- [ ] LeftNav 項目與 pages[] 名稱一致
- [ ] 在無 .NET 環境的測試機上正常執行

---

## Phase 2 — 存取控制整合（Day 2）

**目標：每個頁面可設定最低存取層級，DesignPlayer 根據登入使用者過濾導航。**

### 2-1 machinedesign.json DesignPage 新增 requiredLevel

```json
{
  "pageId": "...",
  "name": "參數設定",
  "requiredLevel": "Operator",
  ...
}
```

### 2-2 設計器 — PageTab 屬性設定

- 頁籤列右鍵 or 頁面屬性面板新增「存取層級」下拉（Guest / Operator / Supervisor / Admin）

### 2-3 DesignPlayer — 動態過濾 NavigationItems

- 登入後根據 `SecurityContext.CurrentUser.Level` 過濾頁面
- 登出後退到 Guest 視圖（只顯示 Guest 頁面）

### 2-4 驗收標準

- [ ] 未登入（Guest）只看到 Guest 頁面
- [ ] Operator 登入後看到 Operator+ 頁面
- [ ] 封裝 app 中權限管控正常運作

---

## Phase 3 — FDA 合規強化（Day 3）

**目標：稽核日誌路徑可設定、使用者資料持久化、提供日誌查看介面。**

### 3-1 SQLite 日誌路徑修正

目前 `ComplianceContext` 寫入 `AppContext.BaseDirectory/logs/`，
部署到 `C:\Program Files\` 可能因權限問題無法寫入。

- `app-config.json` 新增 `logPath`（預設 `%AppData%\{appTitle}\logs`）
- DesignPlayer 啟動時傳入 logPath 給 ComplianceContext

### 3-2 使用者帳號持久化

- 確認 `SecurityContext` 的 Users 是否已持久化（目前狀況 TBD）
- 如為 in-memory → 改為 JSON 檔案（`%AppData%\{appTitle}\users.json`）
- 封裝時可預設帶入初始管理員帳號設定

### 3-3 稽核日誌查看頁面

- DesignPlayer 新增「稽核日誌」NavigationItem（Supervisor+ 才看得到）
- 複用 `LogManagementPanel` 或新建簡化查看 UI
- 支援日期篩選、CSV 匯出

### 3-4 驗收標準

- [ ] 日誌寫入 AppData 路徑（非 exe 目錄）
- [ ] 使用者帳號重啟後保留
- [ ] Supervisor 可查看稽核日誌並匯出

---

## Phase 4 — 整合打磨（Day 4+）

### 4-1 電子簽名對話框（E-Signature）

FDA 21 CFR Part 11 核心要求：高權限操作（如改寫關鍵 PLC 參數）需要再次輸入密碼確認。

- `SecuredButton` `requiredSignature: true` 屬性
- 觸發時彈出「電子簽名」對話框（輸入使用者名稱 + 密碼 + 操作理由）
- 簽名紀錄寫入稽核日誌

### 4-2 首次啟動精靈

- 第一次啟動（users.json 不存在）→ 彈出設定精靈
- 步驟 1：設定 Admin 帳號密碼
- 步驟 2：輸入 PLC IP / Port
- 步驟 3：確認並啟動

### 4-3 DesignRuntime Standard 模式預覽

- `DesignRuntime` 新增 Standard 預覽選項（帶 Shell + 模擬導航）
- 讓設計師在發佈前看到完整 Shell 效果

### 4-4 驗收標準

- [ ] E-Signature 寫入稽核日誌，包含操作人員 + 時間 + 理由
- [ ] 首次啟動精靈完整流程可走通
- [ ] DesignRuntime Standard 預覽正確顯示 Shell

---

## 明天（Phase 1）實作順序

1. `MainViewModel.cs` — 移除封裝按鈕 Dashboard-only 限制
2. `PlayerAppConfig.cs` — 新增 `LayoutMode` 欄位
3. `App.xaml.cs` — 根據 `layoutMode` 分流 Dashboard / Standard 啟動
4. `MainWindow.xaml.cs` — `BuildNavFromPages()` 方法取代硬編碼 NavigationItems
5. `DesignFileService` / `DesignPage.cs` — 確認 `pages[]` 格式正確傳遞
6. 封裝 + 部署驗收
