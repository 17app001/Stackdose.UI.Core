using Stackdose.UI.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Stackdose.Abstractions.Hardware;
using System.Text.RegularExpressions;
using Stackdose.Abstractions.Logging;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// Recipe 管理引擎 (Recipe Management Context)
    /// 用途: 載入、管理和監控 Recipe 配方,符合 FDA 21 CFR Part 11 審計追蹤
    /// </summary>
    public static class RecipeContext
    {
        #region 靜態屬性

        /// <summary>
        /// 當前活動的 Recipe
        /// </summary>
        public static Recipe? CurrentRecipe { get; private set; }

        /// <summary>
        /// 當前選擇的 Recipe 檔案名稱（例如：Recipe1.json, Recipe2.json, Recipe3.json）
        /// </summary>
        public static string CurrentRecipeFileName { get; private set; } = "Recipe1.json";

        /// <summary>
        /// 所有已載入的 Recipe 集合
        /// </summary>
        public static Dictionary<string, Recipe> LoadedRecipes { get; } = new Dictionary<string, Recipe>();

        /// <summary>
        /// 預設 Recipe 檔案路徑
        /// </summary>
        public static string DefaultRecipeFilePath { get; set; } = "Recipe1.json";

        /// <summary>
        /// Recipe 目錄路徑 (用於掃描多個 Recipe 檔案)
        /// </summary>
        public static string RecipeDirectory { get; set; } = "Recipes";

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// 最後載入時間
        /// </summary>
        public static DateTime? LastLoadTime { get; private set; }

        /// <summary>
        /// 最後載入結果訊息
        /// </summary>
        public static string LastLoadMessage { get; private set; } = string.Empty;

        #endregion

        #region 事件定義

        /// <summary>
        /// Recipe 載入成功事件
        /// </summary>
        public static event EventHandler<Recipe>? RecipeLoaded;

        /// <summary>
        /// Recipe 載入失敗事件
        /// </summary>
        public static event EventHandler<string>? RecipeLoadFailed;

        /// <summary>
        /// Recipe 切換事件
        /// </summary>
        public static event EventHandler<Recipe>? RecipeChanged;

        /// <summary>
        /// Recipe 項目更新事件
        /// </summary>
        public static event EventHandler<(Recipe recipe, RecipeItem item)>? RecipeItemUpdated;

        #endregion

        #region 初始化與載入

        /// <summary>
        /// Initialize Recipe system (called on application startup)
        /// </summary>
        /// <param name="autoLoad">Whether to auto-load default Recipe</param>
        public static async Task<bool> InitializeAsync(bool autoLoad = true)
        {
            try
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Initializing Recipe system...",
                    LogLevel.Info,
                    showInUi: true
                );

                IsInitialized = true;

                if (autoLoad)
                {
                    return await LoadRecipeAsync(DefaultRecipeFilePath, isAutoLoad: true);
                }

                return true;
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] Initialization failed: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
                return false;
            }
        }

        /// <summary>
        /// 載入 Recipe 檔案
        /// </summary>
        /// <param name="filePath">Recipe 檔案名稱（例如：Recipe1.json，會自動從 Resources 目錄載入）</param>
        /// <param name="isAutoLoad">是否為自動載入</param>
        /// <param name="setAsActive">是否設定為當前活動 Recipe</param>
        public static async Task<bool> LoadRecipeAsync(
            string filePath, 
            bool isAutoLoad = false, 
            bool setAsActive = true)
        {
            string loadMode = isAutoLoad ? "Auto-Load" : "Manual-Load";
            string userName = ComplianceContext.CurrentUser ?? "System";

            try
            {
                // ?? 使用 ResourcePathHelper 統一管理路徑
                string fullPath;
                
                if (Path.IsPathRooted(filePath) && File.Exists(filePath))
                {
                    // 支援絕對路徑（向下相容）
                    fullPath = filePath;
                }
                else
                {
                    // 優先使用 ResourcePathHelper
                    fullPath = ResourcePathHelper.GetResourceFilePath(filePath);
                }

                // 1. Check if file exists
                if (!File.Exists(fullPath))
                {
                    string errorMsg = $"Recipe file not found: {fullPath}";
                    LastLoadMessage = errorMsg;

                    ComplianceContext.LogSystem(
                        $"[Recipe] {errorMsg}",
                        LogLevel.Warning,
                        showInUi: true
                    );

                    // FDA Audit Trail: Record load failure
                    ComplianceContext.LogAuditTrail(
                        "Recipe Load",
                        filePath,
                        "N/A",
                        "Failed - File not found",
                        $"{loadMode} by {userName}",
                        showInUi: false
                    );

                    RecipeLoadFailed?.Invoke(null, errorMsg);
                    return false;
                }

                // 2. Read and parse JSON file
                // ?? 使用 UTF-8 編碼讀取（支援中文）
                string jsonContent = await File.ReadAllTextAsync(fullPath, System.Text.Encoding.UTF8);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var recipe = JsonSerializer.Deserialize<Recipe>(jsonContent, options);

                if (recipe == null)
                {
                    string errorMsg = "Recipe parsing failed: Invalid JSON format";
                    LastLoadMessage = errorMsg;

                    ComplianceContext.LogSystem(
                        $"[Recipe] {errorMsg}",
                        LogLevel.Error,
                        showInUi: true
                    );

                    ComplianceContext.LogAuditTrail(
                        "Recipe Load",
                        filePath,
                        "N/A",
                        "Failed - JSON parse error",
                        $"{loadMode} by {userName}",
                        showInUi: false
                    );

                    RecipeLoadFailed?.Invoke(null, errorMsg);
                    return false;
                }

                // 3. Validate Recipe data
                var (isValid, errors) = recipe.Validate();
                if (!isValid)
                {
                    string errorMsg = $"Recipe validation failed: {string.Join("; ", errors)}";
                    LastLoadMessage = errorMsg;

                    ComplianceContext.LogSystem(
                        $"[Recipe] {errorMsg}",
                        LogLevel.Warning,
                        showInUi: true
                    );

                    ComplianceContext.LogAuditTrail(
                        "Recipe Load",
                        filePath,
                        "N/A",
                        $"Failed - Validation error: {errors.First()}",
                        $"{loadMode} by {userName}",
                        showInUi: false
                    );

                    RecipeLoadFailed?.Invoke(null, errorMsg);
                    return false;
                }

                // 4. Store in collection
                LoadedRecipes[recipe.RecipeId] = recipe;

                // 5. Set as current active Recipe
                if (setAsActive)
                {
                    CurrentRecipe = recipe;
                    CurrentRecipeFileName = Path.GetFileName(fullPath); // ? 保存當前檔案名稱
                    LastLoadTime = DateTime.Now;
                    RecipeChanged?.Invoke(null, recipe);
                }

                // 6. Update status
                LastLoadMessage = $"Successfully loaded Recipe: {recipe.RecipeName} v{recipe.Version} ({recipe.EnabledItemCount} parameters)";

                // 7. Log success message
                ComplianceContext.LogSystem(
                    $"[Recipe] {LastLoadMessage}",
                    LogLevel.Success,
                    showInUi: true
                );

                // 8. FDA Audit Trail: Record successful load
                ComplianceContext.LogAuditTrail(
                    "Recipe Load",
                    filePath,
                    CurrentRecipe?.RecipeId ?? "N/A",
                    $"{recipe.RecipeId} v{recipe.Version}",
                    $"{loadMode} by {userName} - {recipe.EnabledItemCount} items",
                    showInUi: false
                );

                // 9. Trigger event
                RecipeLoaded?.Invoke(null, recipe);

                return true;
            }
            catch (JsonException jsonEx)
            {
                string errorMsg = $"JSON parsing error: {jsonEx.Message}";
                LastLoadMessage = errorMsg;

                ComplianceContext.LogSystem(
                    $"[Recipe] {errorMsg}",
                    LogLevel.Error,
                    showInUi: true
                );

                ComplianceContext.LogAuditTrail(
                    "Recipe Load",
                    filePath,
                    "N/A",
                    $"Failed - {errorMsg}",
                    $"{loadMode} by {userName}",
                    showInUi: false
                );

                RecipeLoadFailed?.Invoke(null, errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Load failed: {ex.Message}";
                LastLoadMessage = errorMsg;

                ComplianceContext.LogSystem(
                    $"[Recipe] {errorMsg}",
                    LogLevel.Error,
                    showInUi: true
                );

                ComplianceContext.LogAuditTrail(
                    "Recipe Load",
                    filePath,
                    "N/A",
                    $"Failed - {errorMsg}",
                    $"{loadMode} by {userName}",
                    showInUi: false
                );

                RecipeLoadFailed?.Invoke(null, errorMsg);
                return false;
            }
        }

        /// <summary>
        /// Reload current Recipe
        /// </summary>
        public static async Task<bool> ReloadCurrentRecipeAsync()
        {
            if (CurrentRecipe == null)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Cannot reload: No active Recipe",
                    LogLevel.Warning,
                    showInUi: true
                );
                return false;
            }

            return await LoadRecipeAsync(DefaultRecipeFilePath, isAutoLoad: false);
        }

        #endregion

        #region Recipe Query and Switch

        /// <summary>
        /// Switch to specified Recipe
        /// </summary>
        public static bool SwitchRecipe(string recipeId)
        {
            if (LoadedRecipes.TryGetValue(recipeId, out var recipe))
            {
                string userName = ComplianceContext.CurrentUser ?? "System";
                string oldRecipeId = CurrentRecipe?.RecipeId ?? "None";

                CurrentRecipe = recipe;

                ComplianceContext.LogSystem(
                    $"[Recipe] Switch Recipe: {recipe.RecipeName} v{recipe.Version}",
                    LogLevel.Info,
                    showInUi: true
                );

                ComplianceContext.LogAuditTrail(
                    "Recipe Switch",
                    recipeId,
                    oldRecipeId,
                    recipe.RecipeId,
                    $"Switch by {userName}",
                    showInUi: false
                );

                RecipeChanged?.Invoke(null, recipe);
                return true;
            }

            ComplianceContext.LogSystem(
                $"[Recipe] Recipe not found: {recipeId}",
                LogLevel.Warning,
                showInUi: true
            );
            return false;
        }

        /// <summary>
        /// Get parameter value by name
        /// </summary>
        public static string? GetParameterValue(string parameterName)
        {
            return CurrentRecipe?.GetItem(parameterName)?.Value;
        }

        /// <summary>
        /// Get parameter value by address
        /// </summary>
        public static string? GetParameterValueByAddress(string address)
        {
            return CurrentRecipe?.GetItemByAddress(address)?.Value;
        }

        #endregion

        #region Status Query

        /// <summary>
        /// Check if there is an active Recipe
        /// </summary>
        public static bool HasActiveRecipe => CurrentRecipe != null;

        /// <summary>
        /// Get current Recipe summary information
        /// </summary>
        public static string GetSummary()
        {
            if (CurrentRecipe == null)
            {
                return "No active Recipe";
            }

            return $"{CurrentRecipe.RecipeName} v{CurrentRecipe.Version} - {CurrentRecipe.EnabledItemCount} parameters";
        }

        #endregion

        #region Recipe Monitoring

        /// <summary>
        /// 生成 Recipe 監控位址字串 (用於 PlcStatus 自動註冊)
        /// 格式: "D100:1,D102:2,D104:1,M100:1,R2002:1,..."
        /// </summary>
        /// <returns>監控位址配置字串</returns>
        public static string GenerateMonitorAddresses()
        {
            if (CurrentRecipe == null || !CurrentRecipe.Items.Any())
                return string.Empty;

            var addresses = new List<string>();
            var processedWords = new HashSet<string>(); // 追蹤已處理的 Word 地址，避免重複

            foreach (var item in CurrentRecipe.Items.Where(x => x.IsEnabled))
            {
                // ? 處理 Word Bit（例如：R2002.5）
                var wordBitMatch = Regex.Match(item.Address, @"^([DRWM])(\d+)\.(\d+)$");
                if (wordBitMatch.Success && item.DataType?.Equals("Bit", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // 提取 Word 地址（例如：R2002）
                    string wordAddress = $"{wordBitMatch.Groups[1].Value}{wordBitMatch.Groups[2].Value}";
                    
                    // 如果這個 Word 還沒被註冊，就註冊它（長度為 1）
                    if (!processedWords.Contains(wordAddress))
                    {
                        addresses.Add($"{wordAddress}:1");
                        processedWords.Add(wordAddress);
                    }
                    continue;
                }

                // ? 處理純 Bit 裝置（例如：M100, X10, Y20）
                var pureBitMatch = Regex.Match(item.Address, @"^([MXY])(\d+)$");
                if (pureBitMatch.Success && item.DataType?.Equals("Bit", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // 純 Bit 裝置也註冊為長度 1
                    addresses.Add($"{item.Address}:1");
                    continue;
                }

                // ? 處理 Word/DWord 裝置（原有邏輯）
                var match = Regex.Match(item.Address, @"^([DR])(\d+)$");
                if (!match.Success)
                    continue;

                int length = 1;

                // DWord/Float 需要兩個連續暫存器
                if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
                    item.DataType?.Equals("Float", StringComparison.OrdinalIgnoreCase) == true ||
                    item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
                    item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
                {
                    length = 2;
                }

                addresses.Add($"{item.Address}:{length}");
            }

            return string.Join(",", addresses);
        }

        #endregion

        #region Recipe Download (Write to PLC)

        /// <summary>
        /// 下載 Recipe 到 PLC (將 Recipe 參數值寫入 PLC)
        /// </summary>
        /// <param name="plcManager">PLC Manager 實例</param>
        /// <param name="recipe">要下載的 Recipe (null 則使用 CurrentRecipe)</param>
        /// <returns>成功寫入的參數數量</returns>
        public static async Task<int> DownloadRecipeToPLCAsync(IPlcManager plcManager, Recipe? recipe = null)
        {
            recipe ??= CurrentRecipe;

            if (recipe == null)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Cannot download: No active Recipe",
                    LogLevel.Warning,
                    showInUi: true
                );
                return 0;
            }

            if (plcManager?.PlcClient == null || !plcManager.IsConnected)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Cannot download: PLC not connected",
                    LogLevel.Error,
                    showInUi: true
                );
                return 0;
            }

            string userName = ComplianceContext.CurrentUser ?? "System";
            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            try
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] Downloading Recipe to PLC: {recipe.RecipeName} v{recipe.Version}",
                    LogLevel.Info,
                    showInUi: true
                );

                foreach (var item in recipe.Items.Where(x => x.IsEnabled))
                {
                    try
                    {
                        bool writeSuccess = false;

                        // ? 檢查是否為 Bit 位址（例如：R2002.5, D100.3, M100）
                        var bitMatch = Regex.Match(item.Address, @"^([DRWM])(\d+)\.(\d+)$");
                        if (bitMatch.Success && item.DataType?.Equals("Bit", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Bit 位址格式：R2002.5 表示 R2002 的第 5 個 bit
                            string device = bitMatch.Groups[1].Value;
                            int address = int.Parse(bitMatch.Groups[2].Value);
                            int bitPos = int.Parse(bitMatch.Groups[3].Value);

                            if (bitPos < 0 || bitPos > 15)
                            {
                                errors.Add($"{item.Name}: Invalid bit position {bitPos} (must be 0-15)");
                                failCount++;
                                continue;
                            }

                            if (int.TryParse(item.Value, out int bitValue) && (bitValue == 0 || bitValue == 1))
                            {
                                await plcManager.PlcClient.WriteBitAsync(device, address, bitPos, bitValue);
                                writeSuccess = true;

                                ComplianceContext.LogSystem(
                                    $"[Recipe Download] {item.Name} ({item.Address}) = {bitValue}",
                                    LogLevel.Info,
                                    showInUi: false
                                );
                            }
                            else
                            {
                                errors.Add($"{item.Name}: Invalid Bit value '{item.Value}' (must be 0 or 1)");
                                failCount++;
                                continue;
                            }
                        }
                        // ? 檢查是否為純 Bit 裝置（例如：M100, X10, Y20）
                        else if (Regex.IsMatch(item.Address, @"^[MXY]\d+$") && 
                                 item.DataType?.Equals("Bit", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // 純 Bit 裝置（M, X, Y）
                            var pureBitMatch = Regex.Match(item.Address, @"^([MXY])(\d+)$");
                            if (pureBitMatch.Success)
                            {
                                string device = pureBitMatch.Groups[1].Value;
                                int address = int.Parse(pureBitMatch.Groups[2].Value);

                                if (int.TryParse(item.Value, out int bitValue) && (bitValue == 0 || bitValue == 1))
                                {
                                    await plcManager.PlcClient.WriteBitAsync(device, address, bitValue);
                                    writeSuccess = true;

                                    ComplianceContext.LogSystem(
                                        $"[Recipe Download] {item.Name} ({item.Address}) = {bitValue}",
                                        LogLevel.Info,
                                        showInUi: false
                                    );
                                }
                                else
                                {
                                    errors.Add($"{item.Name}: Invalid Bit value '{item.Value}' (must be 0 or 1)");
                                    failCount++;
                                    continue;
                                }
                            }
                        }
                        // ? Word/DWord 裝置（原有邏輯）
                        else
                        {
                            // 解析地址 (例如: D100, R200)
                            var match = Regex.Match(item.Address, @"^([DR])(\d+)$");
                            if (!match.Success)
                            {
                                errors.Add($"{item.Name}: Invalid address format {item.Address}");
                                failCount++;
                                continue;
                            }

                            string device = match.Groups[1].Value;
                            int address = int.Parse(match.Groups[2].Value);

                            // 根據 DataType 進行寫入
                            if (item.DataType?.Equals("Short", StringComparison.OrdinalIgnoreCase) == true ||
                                item.DataType?.Equals("Word", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // Short/Word: 單一暫存器
                                if (short.TryParse(item.Value, out short value))
                                {
                                    await plcManager.PlcClient.WriteWordAsync(device, address, value);
                                    writeSuccess = true;
                                }
                                else
                                {
                                    errors.Add($"{item.Name}: Invalid Short value '{item.Value}'");
                                    failCount++;
                                    continue;
                                }
                            }
                            else if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
                                     item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
                                     item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // DWord/Int: 兩個連續暫存器
                                if (int.TryParse(item.Value, out int value))
                                {
                                    await plcManager.PlcClient.WriteDWordAsync(device, address, value);
                                    writeSuccess = true;
                                }
                                else
                                {
                                    errors.Add($"{item.Name}: Invalid Int value '{item.Value}'");
                                    failCount++;
                                    continue;
                                }
                            }
                            else if (item.DataType?.Equals("Float", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                // Float: 兩個連續暫存器（將 float 轉為 int 表示，例如 3.5 -> 35，需要除以 10）
                                if (float.TryParse(item.Value, out float floatValue))
                                {
                                    int intValue = (int)(floatValue * 10);
                                    await plcManager.PlcClient.WriteDWordAsync(device, address, intValue);
                                    writeSuccess = true;
                                }
                                else
                                {
                                    errors.Add($"{item.Name}: Invalid Float value '{item.Value}'");
                                    failCount++;
                                    continue;
                                }
                            }
                            else
                            {
                                errors.Add($"{item.Name}: Unsupported DataType '{item.DataType}'");
                                failCount++;
                                continue;
                            }

                            if (writeSuccess)
                            {
                                ComplianceContext.LogSystem(
                                    $"[Recipe Download] {item.Name} ({item.Address}) = {item.Value} {item.Unit}",
                                    LogLevel.Info,
                                    showInUi: false
                                );
                            }
                        }

                        if (writeSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            errors.Add($"{item.Name}: Write failed");
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{item.Name}: {ex.Message}");
                        failCount++;
                    }
                }

                // 記錄結果
                if (failCount == 0)
                {
                    ComplianceContext.LogSystem(
                        $"[Recipe] Download completed successfully: {successCount}/{recipe.EnabledItemCount} parameters written",
                        LogLevel.Success,
                        showInUi: true
                    );
                }
                else
                {
                    ComplianceContext.LogSystem(
                        $"[Recipe] Download completed with errors: {successCount} success, {failCount} failed",
                        LogLevel.Warning,
                        showInUi: true
                    );

                    foreach (var error in errors.Take(3))
                    {
                        ComplianceContext.LogSystem(
                            $"[Recipe] Error: {error}",
                            LogLevel.Warning,
                            showInUi: false
                        );
                    }
                }

                // FDA Audit Trail: 記錄下載操作
                ComplianceContext.LogAuditTrail(
                    "Recipe Download",
                    recipe.RecipeId,
                    "N/A",
                    $"{recipe.RecipeName} v{recipe.Version}",
                    $"Downloaded by {userName} - {successCount} success, {failCount} failed",
                    showInUi: false
                );

                return successCount;
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] Download failed: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );

                ComplianceContext.LogAuditTrail(
                    "Recipe Download",
                    recipe.RecipeId,
                    "N/A",
                    "Failed",
                    $"Download by {userName} - Error: {ex.Message}",
                    showInUi: false
                );

                return successCount;
            }
        }

        #endregion
    }
}
