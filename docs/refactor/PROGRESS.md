# 重構進度追蹤

> 每完成一階段打勾 + 補日期 + 補 commit SHA。
> 接手 AI：先看最下面「當前焦點 / 下一步」區塊。

---

## 階段總覽

| 階段 | 狀態 | 日期 | Commit | 說明 |
|---|---|---|---|---|
| **B0** 底層現況校正 | ✅ 完成 | 2026-04-21 | `01a903c` | 盤點、修文件，不動程式碼 |
| **B1** 抽共用基類 | ✅ 完成 | 2026-04-21 | `b0e424d` | PlcLabel/Text/StatusIndicator/AlarmViewer/SensorViewer 全遷移 |
| **B2** 事件能力收斂 | ✅ 完成 | 2026-04-21 | `b0e424d` | PlcEventContext + ControlValueChanged event bus |
| **B3** Templates/Shell 策略化 | ✅ 完成 | 2026-04-21 | `70b919f` | IShellStrategy + FreeCanvas/SinglePage/Standard |
| **B4** Behavior Schema | ✅ 完成 | 2026-04-21 | `4a8cc13` | BehaviorEvent/Condition/Action POCO + events[] |
| **B5** Behavior Engine | ✅ 完成 | 2026-04-22 | `34d9c1f` | BehaviorEngine + 6 Handler + SecuredButton click |
| **B6** Designer UI | ✅ 完成 | 2026-04-22 | `f314dcf` | PropertyPanel → TabControl + EventsPanel 事件編輯 |
| **B7** Standard 模式收尾 | ✅ 完成 | 2026-04-21 | `d7c185a` | PageDefinition + pages[] + SetupMultiPageNavigation |
| **B8** docs 全全面對齊 | ✅ 完成 | 2026-04-21 | `b11398a` | kb/ 更新 behavior-system + base-classes 等 |
| **B9** 專案產生器強化 | ✅ 完成 | 2026-05-04 | `9456591` | 自動生成 Dashboard 專案結構與完整配置 |
| **B10** 設計器體驗優化 | ✅ 完成 | 2026-05-05 | `231a956` | Spacer 容器連動、TabPanel 強化、視窗尺寸補償 |

圖例：⚪ 待命 / 🟡 進行中 / ✅ 完成 / ⛔ 擱置

---

## 當前焦點 / 下一步

1. **PlcConfirmationHandler 實作**
   - 負責處理 PLC 觸發的確認對話框（含倒數自動關閉）。
   - 與 `machinedesign.json` 中的 `Events` 系統對接。

2. **ModelE 實機連線驗證**
   - 測試雙噴頭與四軸狀態在 Dashboard 上的即時反饋。
   - 驗證 `PrintHead` 初始化邏輯與 M-bit 事件連動。

---

## ⚠️ 未解問題

| 問題 | 所在專案 | 優先度 | 備註 |
|---|---|---|---|
| JSON 熱更新（修改 JSON 後自動重新載入畫布） | DesignRuntime | 中 | 重構後尚未實作 |
