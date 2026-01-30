# Login Dialog Loading Indicator

## 功能說明

在 `LoginDialog` 中添加了「登入中」的載入提示功能。

## 實作內容

### 1. **XAML 變更**

在 `LoginDialog.xaml` 中添加了 `LoadingPanel`：

- **位置**: Grid.Row="2"（與 ErrorPanel 共用位置）
- **外觀**: 
  - 青色邊框（#00BCD4）
  - 旋轉的圓形載入動畫
  - "Logging in..." 文字提示
- **動畫**: 使用 Storyboard 實現圓圈持續旋轉（1.5秒一圈）

### 2. **C# 變更**

#### `LoginButton_Click` 方法：
- 改為 `async` 方法
- 點擊登入時：
  1. 隱藏錯誤訊息
  2. 顯示載入提示
  3. 禁用輸入欄位（避免重複操作）
  4. 非同步執行登入驗證
  5. 最少顯示 500ms 的載入動畫
  6. 完成後隱藏載入提示

#### `ShowLoading` 方法：
```csharp
private void ShowLoading(bool show)
```
- **功能**: 控制載入面板的顯示/隱藏
- **show = true**: 顯示載入動畫，禁用輸入欄位
- **show = false**: 隱藏載入動畫，啟用輸入欄位

## 使用流程

1. 使用者輸入帳號密碼
2. 點擊 "Login" 按鈕
3. **立即顯示**：
   - 旋轉的青色圓圈動畫
   - "Logging in..." 文字
   - 輸入欄位變為禁用狀態
4. 背景執行登入驗證（至少 500ms）
5. 完成後：
   - **成功**: 關閉對話框
   - **失敗**: 隱藏載入提示，顯示錯誤訊息

## 視覺效果

```
┌─────────────────────────────────┐
│        User Login               │
│   SQLite User Authentication    │
├─────────────────────────────────┤
│ User ID: [admin01            ]  │
│ Password: [**********        ]  │
├─────────────────────────────────┤
│  ?  Logging in...              │ ← 載入提示
├─────────────────────────────────┤
│  [Login]        [Cancel]        │
└─────────────────────────────────┘
```

## 技術細節

### 載入動畫實作：
- 使用 `RotateTransform` + `DoubleAnimation`
- 圓形使用 `StrokeDashArray` 創造缺口效果
- 動畫無限循環（`RepeatBehavior="Forever"`）

### 非同步登入：
```csharp
var loginTask = Task.Run(() => SecurityContext.Login(userId, password));
var delayTask = Task.Delay(500);
await Task.WhenAll(loginTask, delayTask);
```

### UI 鎖定機制：
- 登入過程中禁用 `UserIdTextBox` 和 `PasswordBox`
- 避免使用者重複點擊或修改輸入
- 完成後自動恢復

## 改進建議

1. ? 已實作最少顯示時間（500ms）
2. ? 已實作 UI 鎖定機制
3. ? 已實作旋轉動畫
4. ? 已實作非同步處理

## 測試要點

- [ ] 點擊 Login 按鈕後立即顯示載入動畫
- [ ] 載入期間無法修改輸入欄位
- [ ] 載入動畫持續旋轉
- [ ] 登入成功後正常關閉
- [ ] 登入失敗後顯示錯誤訊息
- [ ] 載入時按 Enter/Escape 不會重複觸發
