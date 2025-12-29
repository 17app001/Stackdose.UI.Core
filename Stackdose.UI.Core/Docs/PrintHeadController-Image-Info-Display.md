# PrintHead Controller - Image Info Display 功能

## ?? 概述

在 PrintHeadController 的 Image Control 區域新增了圖片資訊顯示功能，當使用者選擇圖片後會自動顯示圖片的尺寸和 DPI 資訊。

## ? 新增功能

### 1. 圖片資訊顯示區域

在 "Browse..." 按鈕上方新增了兩個資訊顯示框：

#### **尺寸顯示 (Size)**
- 位置：左側顯示框
- 格式：`寬 × 高` (例如：`1024 × 768`)
- 顏色：青色 (#00d4ff)
- 預設值：`-` (未選擇圖片時)

#### **DPI 顯示**
- 位置：右側顯示框
- 格式：`數值 DPI` (例如：`300 DPI`)
- 顏色：青色 (#00d4ff)
- 預設值：`-` (未選擇圖片時)

### 2. UI 佈局

```
┌─────────────────────────────────────────────┐
│ Image Control                                │
├─────────────────────────────────────────────┤
│ ┌──────────┐   ┌──────────┐                │
│ │   Size   │   │   DPI    │                │
│ │ 1024×768 │   │ 300 DPI  │                │
│ └──────────┘   └──────────┘                │
│                                             │
│ [image.bmp path...] [Browse...]             │
│                                             │
│ [Load Image to All Heads]                   │
└─────────────────────────────────────────────┘
```

## ?? 實作細節

### XAML 變更

新增了圖片資訊顯示區域：

```xaml
<Grid Grid.Row="2">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="5"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- Size Display -->
    <Border Grid.Column="0" ...>
        <StackPanel>
            <TextBlock Text="Size" FontSize="8" Foreground="Gray"/>
            <TextBlock x:Name="ImageSizeText" Text="-" 
                       FontSize="11" FontWeight="Bold" Foreground="#00d4ff"/>
        </StackPanel>
    </Border>

    <!-- DPI Display -->
    <Border Grid.Column="2" ...>
        <StackPanel>
            <TextBlock Text="DPI" FontSize="8" Foreground="Gray"/>
            <TextBlock x:Name="ImageDpiText" Text="-" 
                       FontSize="11" FontWeight="Bold" Foreground="#00d4ff"/>
        </StackPanel>
    </Border>
</Grid>
```

### C# 程式碼變更

更新 `BrowseImageButton_Click` 方法：

```csharp
private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
{
    var dialog = new OpenFileDialog { ... };

    if (dialog.ShowDialog() == true)
    {
        ImagePathBox.Text = dialog.FileName;
        
        // ?? 讀取並顯示圖片資訊
        try
        {
            using (var image = System.Drawing.Image.FromFile(dialog.FileName))
            {
                // 顯示寬x高
                ImageSizeText.Text = $"{image.Width} × {image.Height}";
                
                // 顯示 DPI (取水平 DPI)
                ImageDpiText.Text = $"{(int)image.HorizontalResolution} DPI";
                
                ComplianceContext.LogSystem(
                    $"[PrintHeadController] Image loaded: {Path.GetFileName(dialog.FileName)} " +
                    $"({image.Width}×{image.Height}, {(int)image.HorizontalResolution} DPI)",
                    LogLevel.Info,
                    showInUi: false
                );
            }
        }
        catch (Exception ex)
        {
            // ?? 讀取失敗時重設顯示
            ImageSizeText.Text = "-";
            ImageDpiText.Text = "-";
            
            ComplianceContext.LogSystem(
                $"[PrintHeadController] Failed to read image info: {ex.Message}",
                LogLevel.Error,
                showInUi: true
            );
        }
    }
}
```

## ?? 顯示邏輯

### 成功讀取圖片
1. 使用 `System.Drawing.Image.FromFile()` 載入圖片
2. 讀取 `Width` 和 `Height` 屬性
3. 讀取 `HorizontalResolution` 屬性作為 DPI
4. 格式化顯示在 UI 上
5. 記錄日誌（不顯示在 UI）

### 讀取失敗
1. 捕捉例外
2. 重設顯示為 `-`
3. 記錄錯誤日誌（顯示在 UI）

## ?? 樣式設計

### 資訊框樣式
- 背景：`{DynamicResource Plc.Bg.Dark}`
- 邊框：`{DynamicResource Plc.Border}`
- 圓角：3px
- 內邊距：5px, 2px
- 邊框粗細：1px

### 文字樣式
- **標題** (Size/DPI)
  - 字體大小：8
  - 顏色：Gray
  - 對齊：Center

- **數值**
  - 字體大小：11
  - 字重：Bold
  - 顏色：#00d4ff (青色)
  - 對齊：Center

## ?? 使用範例

### 基本使用流程

1. 點擊 "Browse..." 按鈕
2. 選擇圖片檔案 (支援 .bmp, .png, .jpg)
3. 自動顯示圖片資訊：
   - Size: `1920 × 1080`
   - DPI: `300 DPI`
4. 點擊 "Load Image to All Heads" 載入到所有噴頭

### 支援的圖片格式
- BMP (Bitmap)
- PNG (Portable Network Graphics)
- JPG/JPEG (Joint Photographic Experts Group)

## ?? 錯誤處理

### 可能的錯誤情況
1. **檔案不存在**：顯示 `-`，記錄錯誤
2. **檔案格式不支援**：顯示 `-`，記錄錯誤
3. **檔案損毀**：顯示 `-`，記錄錯誤
4. **權限不足**：顯示 `-`，記錄錯誤

### 錯誤時的 UI 狀態
```
┌──────────┐   ┌──────────┐
│   Size   │   │   DPI    │
│    -     │   │    -     │
└──────────┘   └──────────┘
```

## ?? 測試建議

### 測試案例

1. **正常圖片測試**
   - 選擇標準 BMP/PNG/JPG 圖片
   - 驗證尺寸和 DPI 顯示正確

2. **不同 DPI 測試**
   - 測試 72 DPI (螢幕解析度)
   - 測試 300 DPI (列印解析度)
   - 測試 600 DPI (高解析度)

3. **不同尺寸測試**
   - 小圖：100×100
   - 中圖：1024×768
   - 大圖：4096×2160

4. **錯誤情況測試**
   - 選擇無效檔案
   - 選擇非圖片檔案
   - 選擇損毀的圖片

## ?? 最佳實踐

1. **選擇合適的圖片**
   - 使用與噴頭解析度相符的 DPI
   - 確保圖片尺寸符合列印需求

2. **檢查顯示資訊**
   - 載入圖片前先確認尺寸和 DPI
   - 避免載入過大或過小的圖片

3. **效能考量**
   - 圖片資訊讀取很快速
   - 不會影響 UI 回應

## ?? 版本資訊

- **實作日期**: 2024年
- **版本**: 1.0
- **狀態**: ? 已完成並測試

## ?? 相關文件

- [PrintHeadController-Guide.md](./PrintHeadController-Guide.md) - 主要使用指南
- [FeiyangSDK-Integration-Guide.md](./FeiyangSDK-Integration-Guide.md) - SDK 整合說明

---

**提示**：此功能讓使用者在載入圖片前能夠確認圖片的規格，避免載入不適合的圖片到噴頭中。
