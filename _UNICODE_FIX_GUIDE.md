# Unicode 圖示顯示問題完整解決方案

## 問題原因
在 XAML 中直接使用 Unicode 字元（如 `?`, `●`, `○` 等）時，如果檔案編碼不正確，會顯示為 `?` 問號。

## 解決方案

### **方法 1: 使用 Unicode 實體編碼（推薦）** ?

將 Unicode 字元轉換為 `&#xHEXCODE;` 格式：

```xml
<!-- ? 錯誤：直接使用 Unicode 字元 -->
<TextBlock Text="? Switch"/>

<!-- ? 正確：使用 Unicode 實體編碼 -->
<TextBlock Text="&#x21BB; Switch" FontFamily="Segoe UI Symbol"/>
```

### **方法 2: 使用 Segoe UI Symbol/Segoe MDL2 Assets 字體**

```xml
<TextBlock Text="&#xE72C;" FontFamily="Segoe MDL2 Assets"/>
```

---

## 常用圖示對照表

### **箭頭類**
| 字元 | Unicode | 實體編碼 | 說明 |
|------|---------|----------|------|
| ? | U+21BB | `&#x21BB;` | 順時針箭頭（Switch/Refresh） |
| ? | U+21BA | `&#x21BA;` | 逆時針箭頭 |
| ? | U+25B6 | `&#x25B6;` | 右三角形（Play） |
| ? | U+25C0 | `&#x25C0;` | 左三角形 |
| ▲ | U+25B2 | `&#x25B2;` | 上三角形 |
| ▼ | U+25BC | `&#x25BC;` | 下三角形 |
| ? | U+23F8 | `&#x23F8;` | Pause |
| ? | U+23F9 | `&#x23F9;` | Stop |

### **圓形/點類**
| 字元 | Unicode | 實體編碼 | 說明 |
|------|---------|----------|------|
| ● | U+25CF | `&#x25CF;` | 實心圓（狀態燈） |
| ○ | U+25CB | `&#x25CB;` | 空心圓 |
| ? | U+25C9 | `&#x25C9;` | 中心點圓 |
| ? | U+2B24 | `&#x2B24;` | 大實心圓 |

### **星號/重要標記**
| 字元 | Unicode | 實體編碼 | 說明 |
|------|---------|----------|------|
| ★ | U+2605 | `&#x2605;` | 實心星星 |
| ☆ | U+2606 | `&#x2606;` | 空心星星 |
| ? | U+26A0 | `&#x26A0;` | 警告符號 |
| ? | U+2713 | `&#x2713;` | 勾選 |
| ? | U+2717 | `&#x2717;` | 叉叉 |

### **其他常用**
| 字元 | Unicode | 實體編碼 | 說明 |
|------|---------|----------|------|
| ? | U+2699 | `&#x2699;` | 設定齒輪 |
| ?? | U+1F50D | `&#x1F50D;` | 放大鏡 |
| ?? | U+1F512 | `&#x1F512;` | 鎖頭 |
| ?? | U+1F513 | `&#x1F513;` | 開鎖 |

---

## 已修復的檔案

### 1. AppHeader.xaml ?
```xml
<!-- Switch User 按鈕圖示 -->
<TextBlock Text="&#x21BB;" 
           FontSize="13"
           FontFamily="Segoe UI Symbol"/>
```

---

## 需要修復的檔案

### 2. PrintHeadController.xaml
多處使用了 Unicode 字元（●, ?, ■ 等）

**修復建議**：
```xml
<!-- 狀態指示燈 -->
<TextBlock Text="&#x25CF;" FontFamily="Segoe UI Symbol"/>  <!-- ● -->

<!-- 播放按鈕 -->
<TextBlock Text="&#x25B6;" FontFamily="Segoe UI Symbol"/>  <!-- ? -->

<!-- 停止按鈕 -->
<TextBlock Text="&#x25A0;" FontFamily="Segoe UI Symbol"/>  <!-- ■ -->
```

### 3. LogViewerPage.xaml
使用了圓形符號

**修復建議**：
```xml
<TextBlock Text="&#x25CF;" FontFamily="Segoe UI Symbol"/>
```

---

## 自動化修復腳本

```powershell
# PowerShell 批次修復腳本
$files = Get-ChildItem -Path "D:\工作區\Project\Stackdose.UI.Core" -Include *.xaml -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Encoding UTF8 -Raw
    
    # 替換常見 Unicode 字元
    $content = $content -replace '?', '&#x21BB;'
    $content = $content -replace '●', '&#x25CF;'
    $content = $content -replace '○', '&#x25CB;'
    $content = $content -replace '?', '&#x25B6;'
    $content = $content -replace '■', '&#x25A0;'
    $content = $content -replace '★', '&#x2605;'
    $content = $content -replace '☆', '&#x2606;'
    
    # 儲存為 UTF-8 with BOM
    [System.IO.File]::WriteAllText($file.FullName, $content, [System.Text.UTF8Encoding]::new($true))
}

Write-Host "修復完成！" -ForegroundColor Green
```

---

## Visual Studio 設定

### **確保 XAML 檔案使用正確編碼**：

1. **File → Advanced Save Options...**
2. **Encoding**: UTF-8 with signature (UTF-8 BOM)
3. **Line endings**: Windows (CR LF)

### **設定預設編碼**：

1. **Tools → Options**
2. **Environment → Documents**
3. ? 勾選 "Save documents as Unicode when data cannot be saved in codepage"

---

## 建置前檢查清單

- [ ] 所有 XAML 檔案使用 UTF-8 with BOM 編碼
- [ ] Unicode 字元改為實體編碼 `&#xHEXCODE;`
- [ ] 指定 `FontFamily="Segoe UI Symbol"` 或 `"Segoe MDL2 Assets"`
- [ ] 建置成功且無警告
- [ ] 執行時圖示正確顯示

---

## 快速修復步驟

1. **開啟 PowerShell**
2. **執行自動化腳本** 或 **手動替換**
3. **重新建置專案**
4. **測試運行**

完成後，所有 Unicode 圖示將正確顯示！
