using Stackdose.UI.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Stackdose.Abstractions.Hardware;
using System.Text.RegularExpressions;

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
        /// 所有已載入的 Recipe 集合
        /// </summary>
        public static Dictionary<string, Recipe> LoadedRecipes { get; } = new Dictionary<string, Recipe>();

        /// <summary>
        /// 預設 Recipe 檔案路徑
        /// </summary>
        public static string DefaultRecipeFilePath { get; set; } = "Recipe.json";

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

        /// <summary>
        /// Recipe 監控是否已啟動
        /// </summary>
        public static bool IsMonitoring { get; private set; } = false;

        /// <summary>
        /// Recipe 監控相關的 PLC Manager
        /// </summary>
        private static IPlcManager? _plcManager;

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

        /// <summary>
        /// Recipe 監控啟動事件
        /// </summary>
        public static event EventHandler<Recipe>? MonitoringStarted;

        /// <summary>
        /// Recipe 監控停止事件
        /// </summary>
        public static event EventHandler? MonitoringStopped;

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
        /// <param name="filePath">Recipe 檔案路徑</param>
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
                // 1. Check if file exists
                if (!File.Exists(filePath))
                {
                    string errorMsg = $"Recipe file not found: {filePath}";
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
                string jsonContent = await File.ReadAllTextAsync(filePath);
                
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
                    RecipeChanged?.Invoke(null, recipe);
                }

                // 6. Update status
                LastLoadTime = DateTime.Now;
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
        /// 啟動 Recipe 參數監控
        /// 將所有 Recipe 參數註冊到 PLC Monitor 服務
        /// </summary>
        /// <param name="plcManager">PLC Manager 實例</param>
        /// <param name="autoStart">是否自動啟動監控服務</param>
        /// <returns>成功註冊的參數數量</returns>
        public static int StartMonitoring(IPlcManager plcManager, bool autoStart = true)
        {
            if (CurrentRecipe == null)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Cannot start monitoring: No active Recipe",
                    LogLevel.Warning,
                    showInUi: true
                );
                return 0;
            }

            if (plcManager?.Monitor == null)
            {
                ComplianceContext.LogSystem(
                    "[Recipe] Cannot start monitoring: PlcManager or Monitor is null",
                    LogLevel.Error,
                    showInUi: true
                );
                return 0;
            }

            _plcManager = plcManager;
            int registeredCount = 0;
            var processedAddresses = new HashSet<string>();

            try
            {
                foreach (var item in CurrentRecipe.Items.Where(x => x.IsEnabled))
                {
                    // 解析地址 (例如: D100, R200)
                    var match = Regex.Match(item.Address, @"^([DR])(\d+)$");
                    if (!match.Success)
                    {
                        ComplianceContext.LogSystem(
                            $"[Recipe] Invalid address format: {item.Address} ({item.Name})",
                            LogLevel.Warning,
                            showInUi: false
                        );
                        continue;
                    }

                    string device = match.Groups[1].Value;
                    int startAddress = int.Parse(match.Groups[2].Value);
                    int length = 1; // 預設為 1 個暫存器

                    // 根據 DataType 決定需要監控的暫存器數量
                    // DWord/Float 需要兩個連續的暫存器
                    if (item.DataType?.Equals("DWord", StringComparison.OrdinalIgnoreCase) == true ||
                        item.DataType?.Equals("Float", StringComparison.OrdinalIgnoreCase) == true ||
                        item.DataType?.Equals("Int", StringComparison.OrdinalIgnoreCase) == true ||
                        item.DataType?.Equals("Int32", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        length = 2; // DWORD 佔用兩個連續暫存器
                    }

                    // 避免重複註冊
                    string addressKey = $"{device}{startAddress}:{length}";
                    if (processedAddresses.Contains(addressKey))
                        continue;

                    // 註冊到 Monitor
                    plcManager.Monitor.Register(item.Address, length);
                    processedAddresses.Add(addressKey);
                    registeredCount++;

                    ComplianceContext.LogSystem(
                        $"[Recipe Monitor] Registered: {item.Name} ({item.Address}) Length={length} Type={item.DataType}",
                        LogLevel.Info,
                        showInUi: false
                    );
                }

                // 啟動監控服務
                if (autoStart && !plcManager.Monitor.IsRunning)
                {
                    plcManager.Monitor.Start();
                }

                IsMonitoring = true;

                ComplianceContext.LogSystem(
                    $"[Recipe] Monitoring started: {registeredCount} parameters registered",
                    LogLevel.Success,
                    showInUi: true
                );

                // 觸發監控啟動事件
                MonitoringStarted?.Invoke(null, CurrentRecipe);

                return registeredCount;
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] Monitoring start failed: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
                return registeredCount;
            }
        }

        /// <summary>
        /// 停止 Recipe 監控
        /// </summary>
        public static void StopMonitoring()
        {
            if (!IsMonitoring)
            {
                return;
            }

            try
            {
                // 如果需要，停止 Monitor 服務
                // 注意：這裡不直接停止 Monitor，因為可能有其他控制項也在使用
                // 只標記狀態為已停止
                IsMonitoring = false;
                _plcManager = null;

                ComplianceContext.LogSystem(
                    "[Recipe] Monitoring stopped",
                    LogLevel.Info,
                    showInUi: true
                );

                // 觸發監控停止事件
                MonitoringStopped?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[Recipe] Monitoring stop failed: {ex.Message}",
                    LogLevel.Error,
                    showInUi: true
                );
            }
        }

        /// <summary>
        /// 生成 Recipe 監控位址字串 (用於 PlcStatus 自動註冊)
        /// 格式: "D100:1,D102:2,D104:1,..."
        /// </summary>
        /// <returns>監控位址配置字串</returns>
        public static string GenerateMonitorAddresses()
        {
            if (CurrentRecipe == null || !CurrentRecipe.Items.Any())
                return string.Empty;

            var addresses = new List<string>();

            foreach (var item in CurrentRecipe.Items.Where(x => x.IsEnabled))
            {
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
    }
}
