# FeiyangSDK 整合檢查工具
# 用途：檢查專案是否正確設定 FeiyangSDK DLL 複製

param(
    [string]$ProjectPath = "."
)

Write-Host "?? 檢查 FeiyangSDK 整合狀態..." -ForegroundColor Cyan
Write-Host ""

# 顏色定義
$colorSuccess = "Green"
$colorWarning = "Yellow"
$colorError = "Red"
$colorInfo = "Cyan"

# 檢查函數
function Test-FeiyangIntegration {
    param([string]$CsprojPath)
    
    $projectName = Split-Path $CsprojPath -Leaf
    Write-Host "?? 專案: $projectName" -ForegroundColor $colorInfo
    Write-Host "   路徑: $CsprojPath" -ForegroundColor Gray
    
    # 讀取專案檔
    if (-not (Test-Path $CsprojPath)) {
        Write-Host "   ? 專案檔不存在" -ForegroundColor $colorError
        return
    }
    
    [xml]$csproj = Get-Content $CsprojPath
    
    # 檢查是否參考 FeiyangWrapper 或相關專案
    $hasFeiyangRef = $false
    $hasPrintHeadRef = $false
    $hasUIRef = $false
    
    foreach ($ref in $csproj.Project.ItemGroup.ProjectReference) {
        if ($ref.Include -like "*FeiyangWrapper*") { $hasFeiyangRef = $true }
        if ($ref.Include -like "*Stackdose.PrintHead*") { $hasPrintHeadRef = $true }
        if ($ref.Include -like "*Stackdose.UI.Core*") { $hasUIRef = $true }
    }
    
    # 檢查是否有 PostBuild Target
    $hasPostBuild = $false
    $hasFeiyangSDKCopy = $false
    
    foreach ($target in $csproj.Project.Target) {
        if ($target.Name -eq "PostBuild" -or $target.AfterTargets -eq "PostBuildEvent") {
            $hasPostBuild = $true
            foreach ($exec in $target.Exec) {
                if ($exec.Command -like "*FeiyangSDK*") {
                    $hasFeiyangSDKCopy = $true
                }
            }
        }
    }
    
    # 判斷是否需要 PostBuild
    $needsPostBuild = $hasFeiyangRef -or $hasPrintHeadRef -or $hasUIRef
    
    Write-Host ""
    Write-Host "   ?? 專案參考:" -ForegroundColor $colorInfo
    if ($hasFeiyangRef) { Write-Host "      ? FeiyangWrapper (直接參考)" -ForegroundColor $colorSuccess }
    if ($hasPrintHeadRef) { Write-Host "      ? Stackdose.PrintHead" -ForegroundColor $colorSuccess }
    if ($hasUIRef) { Write-Host "      ? Stackdose.UI.Core" -ForegroundColor $colorSuccess }
    if (-not $needsPostBuild) { Write-Host "      ??  無 Feiyang 相關參考" -ForegroundColor Gray }
    
    Write-Host ""
    Write-Host "   ?? PostBuild 設定:" -ForegroundColor $colorInfo
    
    if ($needsPostBuild) {
        if ($hasFeiyangSDKCopy) {
            Write-Host "      ? 已正確設定 FeiyangSDK DLL 複製" -ForegroundColor $colorSuccess
        } else {
            Write-Host "      ? 缺少 FeiyangSDK DLL 複製設定！" -ForegroundColor $colorError
            Write-Host "      ??  執行時可能會出現 DllNotFoundException" -ForegroundColor $colorWarning
            Write-Host ""
            Write-Host "      ?? 請在 .csproj 中加入:" -ForegroundColor $colorInfo
            Write-Host '         <Target Name="PostBuild" AfterTargets="PostBuildEvent">' -ForegroundColor Gray
            Write-Host '           <Exec Command="if exist `"$(SolutionDir)FeiyangSDK-2.3.1\lib`" xcopy /Y /E /I `"$(SolutionDir)FeiyangSDK-2.3.1\lib\*`" `"$(TargetDir)`"" />' -ForegroundColor Gray
            Write-Host '         </Target>' -ForegroundColor Gray
        }
    } else {
        Write-Host "      ? 不需要 FeiyangSDK（無相關參考）" -ForegroundColor $colorSuccess
    }
    
    # 檢查輸出目錄
    $projectDir = Split-Path $CsprojPath -Parent
    $binDir = Join-Path $projectDir "bin\Debug\net8.0-windows"
    
    if (Test-Path $binDir) {
        Write-Host ""
        Write-Host "   ?? 輸出目錄檢查:" -ForegroundColor $colorInfo
        
        $njcsDll = Test-Path (Join-Path $binDir "NJCS.dll")
        $njcscDll = Test-Path (Join-Path $binDir "NJCSC.dll")
        $opencvDll = Test-Path (Join-Path $binDir "opencv_world420.dll")
        
        if ($needsPostBuild) {
            if ($njcsDll) { Write-Host "      ? NJCS.dll" -ForegroundColor $colorSuccess } 
            else { Write-Host "      ? NJCS.dll (缺少)" -ForegroundColor $colorError }
            
            if ($njcscDll) { Write-Host "      ? NJCSC.dll" -ForegroundColor $colorSuccess } 
            else { Write-Host "      ? NJCSC.dll (缺少)" -ForegroundColor $colorError }
            
            if ($opencvDll) { Write-Host "      ? opencv_world420.dll" -ForegroundColor $colorSuccess } 
            else { Write-Host "      ? opencv_world420.dll (缺少)" -ForegroundColor $colorError }
            
            if (-not ($njcsDll -and $njcscDll -and $opencvDll)) {
                Write-Host ""
                Write-Host "      ?? 請重新建置專案以複製 DLL" -ForegroundColor $colorWarning
            }
        } else {
            Write-Host "      ??  不需要 FeiyangSDK DLLs" -ForegroundColor Gray
        }
    } else {
        Write-Host ""
        Write-Host "   ??  尚未建置專案" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host ""
}

# 主程式
if ($ProjectPath -eq ".") {
    # 掃描所有 .csproj
    Write-Host "?? 掃描目前目錄的所有專案..." -ForegroundColor $colorInfo
    Write-Host ""
    
    $projects = Get-ChildItem -Recurse -Filter "*.csproj" | 
                Where-Object { $_.FullName -notlike "*\obj\*" -and $_.FullName -notlike "*\bin\*" }
    
    if ($projects.Count -eq 0) {
        Write-Host "? 找不到任何專案檔" -ForegroundColor $colorError
        exit 1
    }
    
    Write-Host "找到 $($projects.Count) 個專案" -ForegroundColor $colorInfo
    Write-Host ""
    Write-Host "????????????????????????????????????????" -ForegroundColor DarkGray
    Write-Host ""
    
    foreach ($proj in $projects) {
        Test-FeiyangIntegration -CsprojPath $proj.FullName
    }
} else {
    # 檢查指定專案
    Test-FeiyangIntegration -CsprojPath $ProjectPath
}

Write-Host "? 檢查完成" -ForegroundColor $colorSuccess
Write-Host ""
Write-Host "?? 詳細文件請參閱: Stackdose.UI.Core\Docs\FeiyangSDK-Integration-Guide.md" -ForegroundColor $colorInfo
