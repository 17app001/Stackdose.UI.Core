# PrintHeadController 權限控制

## 概述
為 PrintHeadController 添加了權限控制功能，限制圖片操作功能僅供 Engineer 等級以上的用戶使用。

## 權限需求

### Engineer 權限要求
以下三個圖片操作功能需要 **Engineer** 權限：

1. **讀取圖片** (BrowseImageButton)
   - 打開檔案選擇對話框
   - 載入圖片預覽和資訊

2. **載入任務** (LoadImageButton)
   - 將圖片載入到所有已連接的噴頭
   - 執行實際的列印任務

3. **取消任務** (CancelTaskButton)
   - 取消當前的列印任務

## 實作細節

### 1. 訂閱權限變更事件
```csharp
// 在 Loaded 事件中訂閱
SecurityContext.AccessLevelChanged += OnAccessLevelChanged;

// 在 Unloaded 事件中取消訂閱
SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
```

### 2. 更新按鈕狀態
```csharp
private void UpdateButtonPermissions()
{
    bool hasEngineerAccess = SecurityContext.HasAccess(AccessLevel.Engineer);

    // 圖片操作按鈕只有 Engineer 可以使用
    BrowseImageButton.IsEnabled = hasEngineerAccess;
    LoadImageButton.IsEnabled = hasEngineerAccess;
    CancelTaskButton.IsEnabled = hasEngineerAccess;

    // 設置提示文字
    if (!hasEngineerAccess)
    {
        string tooltip = $"需要 Engineer 權限\n目前權限: {SecurityContext.CurrentSession.CurrentLevel}";
        BrowseImageButton.ToolTip = tooltip;
        LoadImageButton.ToolTip = tooltip;
        CancelTaskButton.ToolTip = tooltip;
    }
}
```

### 3. 執行時權限檢查
在按鈕點擊事件中進行二次驗證：
```csharp
private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
{
    // 更新活動時間
    SecurityContext.UpdateActivity();

    // 檢查權限
    if (!SecurityContext.CheckAccess(AccessLevel.Engineer, "讀取圖片"))
    {
        return;
    }

    // ... 執行功能
}
```

## 權限等級說明

| 等級 | 名稱 | 權限 |
|------|------|------|
| 0 | Guest | 未登入，無權限 |
| 1 | Operator | 操作員，日常生產操作 |
| 2 | Instructor | 指導員，可處理警報和查看日誌 |
| 3 | Supervisor | 主管，可管理 Level 1-2 帳號 |
| 4 | Engineer | 工程師，最高權限，可修改製程參數 |

## 使用體驗

### 無權限時
- 按鈕呈現半透明、禁用狀態（Opacity: 0.5）
- 滑鼠懸停時顯示提示：
  ```
  需要 Engineer 權限
  目前權限: Operator
  ```
- 點擊按鈕時會彈出權限不足對話框

### 有權限時
- 按鈕正常顯示，可點擊
- 滑鼠懸停時顯示功能提示
- 執行操作時記錄到 Audit Trail

## 設計考量

### 1. 雙重驗證
- UI 層面：按鈕 IsEnabled 控制
- 邏輯層面：點擊事件中再次檢查權限
- 防止通過程式碼直接調用繞過權限

### 2. 即時更新
- 訂閱 `AccessLevelChanged` 事件
- 登入/登出時自動更新按鈕狀態
- 權限變更立即生效

### 3. 活動追蹤
- 所有操作都調用 `SecurityContext.UpdateActivity()`
- 防止自動登出（閒置 15 分鐘後登出）

### 4. Audit Trail
- 權限檢查失敗時自動記錄到 Audit Trail
- 包含用戶 ID、操作名稱、失敗原因
- 符合 FDA 21 CFR Part 11 要求

## 測試場景

### 場景 1：操作員登入
```
1. 以 operator 身份登入（密碼：1234）
2. 開啟 PrintHeadController
3. 觀察：圖片操作三個按鈕呈現半透明禁用狀態
4. 嘗試點擊：彈出權限不足對話框
```

### 場景 2：工程師登入
```
1. 以 engineer 身份登入（密碼：1234）
2. 開啟 PrintHeadController
3. 觀察：所有按鈕正常顯示，可點擊
4. 成功執行圖片操作功能
```

### 場景 3：權限切換
```
1. 以 engineer 身份登入
2. 開啟 PrintHeadController（按鈕可用）
3. 登出
4. 觀察：按鈕立即變為禁用狀態
5. 以 operator 身份登入
6. 觀察：按鈕保持禁用狀態
```

## 未來擴展

### 可能的增強功能
1. **細粒度權限**
   - 為每個功能設定獨立權限
   - 例如：讀取圖片需要 Supervisor，載入任務需要 Engineer

2. **權限委派**
   - 臨時提升權限（需要更高權限用戶授權）
   - 時間限制的權限提升

3. **權限審計報告**
   - 統計各用戶的權限使用情況
   - 分析權限不足的嘗試次數

## 相關檔案
- `PrintHeadController.xaml` - UI 定義
- `PrintHeadController.xaml.cs` - 權限控制實作
- `SecurityContext.cs` - 安全上下文管理
- `AccessLevel.cs` - 權限等級定義

## 符合標準
? FDA 21 CFR Part 11  
? 電子簽章規範  
? Audit Trail 記錄  
? 權限分級控制
