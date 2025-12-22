# PlcEventTrigger 防重複觸發修補腳本

Write-Host "開始修補 PlcEventTrigger.xaml.cs..." -ForegroundColor Cyan

$filePath = "Stackdose.UI.Core\Controls\PlcEventTrigger.xaml.cs"

# 備份
Copy-Item $filePath "$filePath.backup" -Force
Write-Host "? 已備份原檔案" -ForegroundColor Green

# 讀取檔案
$content = Get-Content $filePath -Raw

# 修補 1: 添加 _isProcessing 欄位 (在 _lastValue 後面)
if ($content -notmatch "private bool _isProcessing") {
    $content = $content -replace "(private bool _lastValue = false;.*?\r?\n)", "`$1        private bool _isProcessing = false; // 防止重複觸發`r`n"
    Write-Host "? 已添加 _isProcessing 欄位" -ForegroundColor Green
}

# 修補 2: 在 OnMonitorBitChanged 添加檢查 (在 address 檢查後)
if ($content -notmatch "if \(_isProcessing\)") {
    $pattern = "(if \(address != _cachedAddress\)\s+return;)\s+"
    $replacement = @"
`$1

            // 防止重複處理
            if (_isProcessing)
            {
                ComplianceContext.LogSystem(
                    `$"[PlcEventTrigger] {_cachedEventName} ({address}) - Skipped (already processing)",
                    Models.LogLevel.Warning,
                    showInUi: true
                );
                return;
            }

"@
    $content = $content -replace $pattern, $replacement
    Write-Host "? 已添加 _isProcessing 檢查" -ForegroundColor Green
}

# 修補 3: 在 OnEventTriggered 設定和重置旗標
if ($content -notmatch "_isProcessing = true;") {
    # 在 OnEventTriggered 開頭添加
    $pattern = "(private async void OnEventTriggered\(bool value\)\s*{\s*var manager = _boundStatus\?\.CurrentManager;[\s\S]*?if \(manager == null\)\s+return;)\s+"
    $replacement = @"
`$1

            // 設定處理中旗標
            _isProcessing = true;

            try
            {
"@
    $content = $content -replace $pattern, $replacement
    Write-Host "? 已設定 _isProcessing = true" -ForegroundColor Green
    
    # 在 AutoClear 後添加 finally
    $pattern = "(\}\s*\}\s*)\s*(#endregion)"
    $replacement = @"
                }
            }
            finally
            {
                // 重置處理中旗標
                _isProcessing = false;
            }
        }

        `$2
"@
    $content = $content -replace $pattern, $replacement
    Write-Host "? 已添加 finally 區塊重置旗標" -ForegroundColor Green
}

# 儲存檔案
$content | Set-Content $filePath -Encoding UTF8 -NoNewline

Write-Host "`n? 修補完成！" -ForegroundColor Green
Write-Host "備份檔案: $filePath.backup" -ForegroundColor Yellow
Write-Host "`n請執行 'dotnet build' 測試建置" -ForegroundColor Cyan
