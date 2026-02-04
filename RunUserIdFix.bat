@echo off
chcp 65001 > nul
cls
echo ========================================
echo  UserId 格式自動修正工具
echo  Auto-fix Tool for UserId Format
echo ========================================
echo.

echo 正在編譯工具程式...
dotnet build UserIdFixTool\UserIdFixTool.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ? 編譯失敗！
    pause
    exit /b 1
)

echo.
echo ? 編譯成功
echo.
echo 正在執行修正工具...
echo.

dotnet run --project UserIdFixTool\UserIdFixTool.csproj

pause
