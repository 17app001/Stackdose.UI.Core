# 控制項視覺 Token 收斂規範

## 目標

減少 `Stackdose.UI.Core/Controls/*.xaml` 中的硬編碼色碼，統一改用 Theme Token，確保 Dark / Light 主題一致且可維護。

## 現況快照

依目前 XAML 掃描（`#[0-9A-Fa-f]{3,8}`），殘留硬編碼色碼集中在：

- `Controls/LoginDialog.xaml`
- `Controls/PrintHeadStatus.xaml`
- `Controls/PrintHeadController.xaml`
- `Controls/PrintHeadPanel.xaml`
- `Controls/LiveLogViewer.xaml`
- `Controls/PlcDeviceEditor.xaml`
- `Controls/PlcStatus.xaml`

主要類型：

- **透明遮罩色**：`#CC000000`、`#2A000000`、`#22000000`、`#26000000`、`#2B000000`
- **中性灰色**：`#424242`、`#757575`、`#808080`、`#9E9E9E`、`#E0E0E0`
- **品牌/操作強調色**：`#00BCD4`、`#00A868`、`#007ACC`、`#FFA726`、`#FF6F00`
- **PrintHead 專屬強調色**：`#ff4757`、`#00d4ff`、`#ffd700`、`#00ff7f`、`#ff69b4`

## 收斂規則

1. 優先使用 `Themes/Colors.xaml` 與 `Themes/LightColors.xaml` 中已有的 Token
2. 只在語意確實缺失時才新增 Token（例如遮罩底色、PrintHead 遙測強調色）
3. 保留各控制項的視覺識別，但透過 Token 綁定，不寫字面值
4. 避免在控制項 Template 內部使用一次性十六進位色碼

## 建議新增 Token

請同步更新 Dark / Light 兩份字典：

**Overlay（遮罩）**
- `Overlay.Bg.Scrim`
- `Overlay.Bg.Strong`
- `Overlay.Bg.Medium`
- `Overlay.Bg.Light`

**PrintHead**
- `PrintHead.Accent.Temperature`
- `PrintHead.Accent.Voltage`
- `PrintHead.Accent.Encoder`
- `PrintHead.Accent.PrintIndex`
- `PrintHead.State.Active`
- `PrintHead.State.Idle`

## 替換計畫

### Phase 1（快速收益）

替換以下檔案中的透明遮罩與操作/中性色字面值：
- `Controls/LiveLogViewer.xaml`
- `Controls/PlcStatus.xaml`
- `Controls/PlcDeviceEditor.xaml`
- `Controls/PrintHeadPanel.xaml`

### Phase 2（登入對話框）

- 替換 `LoginDialog.xaml` 中的字面值，改用 Panel / Input / Error 語意 Token
- 保持外觀不變，只移除硬編碼色碼

### Phase 3（PrintHead 控制項）

- 替換 `PrintHeadStatus.xaml` 與 `PrintHeadController.xaml` 中的遙測/狀態色彩
- 透過 `Status.*` + `PrintHead.*` Token 保留語意強調

## 驗證清單

- 修改的控制項中無新增 `#` 色碼
- 新增的 Token Key 同時存在於 Dark / Light 兩份字典
- 現有事件 Hook、`x:Name` 目標與 Binding 未受影響
- 回歸測試：`Stackdose.UI.Core.Tests` 全綠
