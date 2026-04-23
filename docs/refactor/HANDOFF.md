# 接手指引 — 給下一位 AI / 下一次對話

> 你如果剛接手這個分支，從這份開始讀。讀完這份你就能繼續。

---

## 你是誰

你是 Claude（或其他 LLM），使用者請你接手 `refactor/foundation-and-behavior` 分支的工作。
**B0–B9 重構已全部完成**（2026-04-21～23）。現在是功能開發階段，不是重構。

---

## 讀檔順序（照這個順序，不要跳）

1. **`CLAUDE.md`**（根目錄）— 專案定位、技術棧、核心規則
2. **`docs/refactor/PROGRESS.md`**（底部「當前焦點」段落）— 上次做到哪、下一步是什麼
3. **`docs/devlog/2026-04.md`**（最上面的日期區塊）— 最近實際做了什麼
4. `git log --oneline -10` — 確認 commits 與文件一致

---

## 最終主旨

> 讓不懂 XAML 的工程師或設計師，也能為 PLC 工業機台做出符合 FDA 21 CFR Part 11 稽核要求的監控介面。

「設計師拖控件設反應 → 封裝 → 部署」這條路徑要持續順暢。

---

## 目前功能完成狀態（2026-04-23）

| 功能 | 狀態 |
|---|---|
| B0–B9 重構（基類/事件/Shell策略/BehaviorEngine） | ✅ 完成 |
| MachinePageDesigner FreeCanvas 全功能 | ✅ 完成 |
| BehaviorEngine + EventsPanel | ✅ 完成 |
| Dashboard Shell 模式（含 scaffold） | ✅ 完成 |
| Designer 方向鍵微調（Arrow=1px / Shift=10px） | ✅ 完成 |
| DesignRuntime 四種 Shell 策略 | ✅ 完成 |
| `new-app.ps1 -Mode Dashboard/Standard/SinglePage` | ✅ 完成 |
| 分支合併至 master | ⏳ 待執行 |

---

## 用戶偏好（已確認的）

1. **每階段完成後要停手回報**，不要自己連跑下一步
2. **文件要更新**：每次任務結束更新 `PROGRESS.md` + `devlog`
3. **commit 前要確認沒帶入不相關檔案**（`.sln` 測試專案、`context.md`、本地 txt）
4. **不要加未被要求的功能或抽象層**
5. **push 前主動告知，不擅自 force push**

---

## 不可違反的鐵律

1. 🚫 **不改動 `../Stackdose.Platform/`** 或 FeiyangWrapper，除非用戶授權
2. 🚫 **不擅自 merge 到 master** 或 push 到 origin（除非用戶明確說可以）
3. 🚫 **不在 `Controls/*.xaml` 寫硬編碼色碼**，用語意 Token
4. 🚫 **不繞過 `ComplianceContext`** 散落寫日誌
5. 🚫 **XAML template 裡不用非 ASCII Unicode 字元**（如 ✕）— 會導致 PS 生成檔編碼損壞（前例：commit `100fe98`、本次 Dashboard X 按鈕）

---

## 遇到問題時

| 情況 | 做法 |
|---|---|
| 編譯失敗 | 先確認 `../Stackdose.Platform/` 各專案與 `FeiyangWrapper.dll` 存在 |
| `dotnet build Stackdose.Designer.sln` 有 MSB4278 | 預期失敗（C++ vcxproj 需 VS MSBuild），不是我們造成的 |
| 不確定規格或設計決策 | 問用戶，不要自行發明 |
| 發現 feature/copilot 有功能未合入 | 用 `git show <sha>` 確認 diff，cherry-pick 或手動 port，不要整分支 merge |

---

## 架構關鍵知識

- `DesignDocument.ShellMode` = `Layout.Mode`（同一欄位）→ 傳給 `ShellStrategyFactory.Select()` 決定策略
- `DashboardShellStrategy.Wrap()` 直接回傳 viewport（不包 Shell Chrome）
- Dashboard 自動連線靠 `DesignMeta.PlcIp / PlcPort / ScanInterval`
- Designer 工具列 PLC 欄位只在 `IsDashboardMode = true` 時顯示
- `EnableLiveRecord` 預設 `true`，JSON 裡沒這個欄位 = 啟用（正常）
- `scripts/new-app.ps1` 是 `init-shell-app.ps1 -JsonDrivenApp` 的薄包裝
