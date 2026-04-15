# Core UI 設計標準（v1）

本標準定義 `Stackdose.UI.Core` 控制項的基準 Token 與樣式規則。

## 適用範圍

- 適用於 `Stackdose.UI.Core/Controls/*` 與 `Stackdose.UI.Core/Themes/*`
- `Stackdose.UI.Templates` 應消費這些 Token，不應引入平行的色彩語意

## Token 層次

### 1）舊有領域 Token（相容保留）

- `Cyber.*`
- `Plc.*`
- `Sensor.*`
- `Log.*`
- `Button.*`
- `Status.*`
- `PrintHead.*`

這些 Token 維持向下相容，不刪除。

### 2）語意別名 Token（新開發優先使用）

- 背景：`Surface.Bg.Page`、`Surface.Bg.Panel`、`Surface.Bg.Card`、`Surface.Bg.Control`
- 邊框：`Surface.Border.Default`、`Surface.Border.Strong`
- 文字：`Text.Primary`、`Text.Secondary`、`Text.Tertiary`
- 強調/操作：`Accent.Primary`、`Action.Primary`、`Action.Success`、`Action.Warning`、`Action.Error`、`Action.Info`

新控制項優先使用語意別名；只有需要領域專屬語意時才使用舊有 Domain Token。

## 命名規則

1. 使用 `Domain.Category.State` 三段命名格式
2. 禁止在控制項 XAML 中寫硬編碼十六進位色碼
3. 若同一顏色被 2 個以上控制項使用，必須定義 Token
4. Dark / Light 兩個字典必須同步維護每一個 Key

## 樣式規則

1. 互動控制項必須有 Hover / Pressed / Disabled 三種狀態
2. 輸入控制項必須有 Focus Border 狀態
3. 列表/資料檢視需有明確的空白狀態（Empty State）
4. 字型大小規範：
   - 標籤文字：11–13 px
   - 數值顯示：12–16 px
   - 標題文字：16 px 以上

## Core → Templates 遷移指引

1. Templates 應引用 Core 語意 Token，不得繞過
2. 共用按鈕/輸入樣式模式應移至 Core Theme Components
3. 品牌專屬色彩放在 Templates，但須透過 Core Token 對應

## 驗證清單

- 修改的 `Controls/*.xaml` 中無硬編碼十六進位色碼
- 新增的 Theme Token Key 同時存在於 `Colors.xaml`（Dark）與 `LightColors.xaml`
- 現有控制項行為與 Binding 未受影響
- 回歸測試通過（`Stackdose.UI.Core.Tests`）
