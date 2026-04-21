# 重構進度追蹤

> 每完成一階段打勾 + 補日期 + 補 commit SHA。
> 接手 AI：先看最下面「當前焦點 / 下一步」區塊。

---

## 階段總覽

| 階段 | 狀態 | 日期 | Commit | 說明 |
|---|---|---|---|---|
| **B0** 底層現況校正 | 🟡 進行中 | 2026-04-21 起 | — | 盤點、修文件，不動程式碼 |
| **B1** 抽共用基類 | ⚪ 待命 | — | — | 等 B0 完成回報後用戶確認才開始 |
| **B2** 事件能力收斂 | ⚪ 待命 | — | — | — |
| **B3** Shell 策略化 | ⚪ 待命 | — | — | — |
| **B4** Behavior Schema | ⚪ 待命 | — | — | — |
| **B5** Behavior Engine | ⚪ 待命 | — | — | — |
| **B6** Designer UI | ⚪ 待命 | — | — | — |
| **B7** Standard 模式收尾 | ⚪ 待命 | — | — | — |
| **B8** docs 全面對齊 | ⚪ 待命 | — | — | — |

圖例：⚪ 待命 / 🟡 進行中 / ✅ 完成 / ⛔ 擱置

---

## B0 子任務 checklist

- [x] **B0.0** 建立 `docs/refactor/` 元文件（README / PLAN / PROGRESS / HANDOFF）— 2026-04-21
- [x] **B0.1** Core Controls DP 盤點 → `B0-control-inventory.md` — 2026-04-21
- [x] **B0.2** Templates Controls / Pages 盤點 — 2026-04-21
- [x] **B0.3** 文件 vs 實際差異表 → `B0-findings.md` — 2026-04-21
- [x] **B0.4** 修正 `docs/kb/controls-reference.md` — 2026-04-21
- [x] **B0.5** 修正 `docs/kb/architecture.md` + `docs/PROJECT_MAP.md` + `CLAUDE.md` — 2026-04-21
- [x] **B0.6** 更新 `CURRENT_FOCUS.md` + `devlog/2026-04.md`（`index.html` 不存在，跳過） — 2026-04-21
- [ ] **B0.7** Commit 所有 B0 變更 + 回報用戶、停手

---

## 當前焦點 / 下一步

> 接手 AI：從這裡開始做事。

**現在在做：** B0.7（commit B0 所有變更）

**B0 產出總覽：**
- 新增：`docs/refactor/` 六份文件（README / PLAN / PROGRESS / HANDOFF / B0-control-inventory / B0-findings）
- 修改：`docs/kb/controls-reference.md`（大改）、`docs/kb/architecture.md`（§2/§4/§5/§6）、`docs/PROJECT_MAP.md`（補 DesignPlayer）、`CLAUDE.md`（元件數量）、`CURRENT_FOCUS.md`（指向重構）、`docs/devlog/2026-04.md`（4/21 條目）

**下一步：** 執行 `git add` + commit；commit 完回報使用者並**停手等 B1 許可**，不要自己接著跑 B1。

---

## 已知規矩 / 用戶偏好（重構期間）

- ✅ **可以移除舊控件**：不必為 UbiDemo / 測試專案編譯買單
- ✅ **底層先完成**：B0-B3 都處理底層，B4 之後才做上層
- 🛑 **每階段 stop-and-report**：除非明確授權連跑
- 🛑 **不要動 `docs/kb/` 中途版本**：B0 例外（目的就是改它），B1–B7 產出放 `docs/refactor/`，B8 才統一回灌
