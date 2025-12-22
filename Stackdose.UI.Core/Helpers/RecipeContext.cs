using Stackdose.UI.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
    }
}
