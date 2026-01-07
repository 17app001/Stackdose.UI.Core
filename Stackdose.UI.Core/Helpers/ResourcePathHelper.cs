using System.IO;
using Stackdose.UI.Core.Models;

namespace Stackdose.UI.Core.Helpers
{
    /// <summary>
    /// 資源檔案路徑輔助類別
    /// 用途：統一管理 Resources 目錄內的配置檔案路徑
    /// </summary>
    public static class ResourcePathHelper
    {
        #region 靜態屬性

        /// <summary>
        /// Resources 目錄的完整路徑
        /// </summary>
        public static string ResourcesDirectory { get; private set; }

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public static bool IsInitialized { get; private set; }

        #endregion

        #region 靜態建構子

        static ResourcePathHelper()
        {
            Initialize();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化 Resources 目錄路徑
        /// </summary>
        /// <param name="customResourcesPath">自訂 Resources 路徑（選填）</param>
        public static void Initialize(string? customResourcesPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customResourcesPath))
            {
                // 使用自訂路徑
                ResourcesDirectory = customResourcesPath;
            }
            else
            {
                // 預設路徑：應用程式根目錄下的 Resources 目錄
                ResourcesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            }

            // 確保目錄存在
            if (!Directory.Exists(ResourcesDirectory))
            {
                try
                {
                    Directory.CreateDirectory(ResourcesDirectory);
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] Created Resources directory: {ResourcesDirectory}",
                        LogLevel.Info,
                        showInUi: false
                    );
                }
                catch (Exception ex)
                {
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] Failed to create Resources directory: {ex.Message}",
                        LogLevel.Error,
                        showInUi: true
                    );
                }
            }

            IsInitialized = true;

            #if DEBUG
            System.Diagnostics.Debug.WriteLine($"[ResourcePathHelper] Initialized - Resources directory: {ResourcesDirectory}");
            #endif
        }

        #endregion

        #region 路徑解析

        /// <summary>
        /// 取得資源檔案的完整路徑
        /// </summary>
        /// <param name="fileName">檔案名稱（例如：Recipe1.json, Sensors.json）</param>
        /// <returns>完整檔案路徑</returns>
        public static string GetResourceFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            return Path.Combine(ResourcesDirectory, fileName);
        }

        /// <summary>
        /// 取得資源檔案的完整路徑（支援子目錄）
        /// </summary>
        /// <param name="subDirectory">子目錄名稱（選填）</param>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>完整檔案路徑</returns>
        public static string GetResourceFilePath(string subDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(subDirectory))
            {
                return GetResourceFilePath(fileName);
            }

            string subDirPath = Path.Combine(ResourcesDirectory, subDirectory);

            // 確保子目錄存在
            if (!Directory.Exists(subDirPath))
            {
                try
                {
                    Directory.CreateDirectory(subDirPath);
                }
                catch (Exception ex)
                {
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] Failed to create subdirectory '{subDirectory}': {ex.Message}",
                        LogLevel.Error,
                        showInUi: false
                    );
                }
            }

            return Path.Combine(subDirPath, fileName);
        }

        /// <summary>
        /// 檢查資源檔案是否存在
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>true 表示檔案存在</returns>
        public static bool FileExists(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string fullPath = GetResourceFilePath(fileName);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// 檢查資源檔案是否存在（支援子目錄）
        /// </summary>
        /// <param name="subDirectory">子目錄名稱（選填）</param>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>true 表示檔案存在</returns>
        public static bool FileExists(string? subDirectory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string fullPath = string.IsNullOrWhiteSpace(subDirectory)
                ? GetResourceFilePath(fileName)
                : GetResourceFilePath(subDirectory, fileName);

            return File.Exists(fullPath);
        }

        /// <summary>
        /// 取得 Resources 目錄下所有 JSON 檔案清單
        /// </summary>
        /// <param name="subDirectory">子目錄名稱（選填）</param>
        /// <param name="searchPattern">搜尋模式（預設：*.json）</param>
        /// <returns>JSON 檔案路徑清單</returns>
        public static string[] GetJsonFiles(string? subDirectory = null, string searchPattern = "*.json")
        {
            try
            {
                string searchPath = string.IsNullOrWhiteSpace(subDirectory)
                    ? ResourcesDirectory
                    : Path.Combine(ResourcesDirectory, subDirectory);

                if (!Directory.Exists(searchPath))
                {
                    return Array.Empty<string>();
                }

                return Directory.GetFiles(searchPath, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[ResourcePathHelper] Failed to get JSON files: {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return Array.Empty<string>();
            }
        }

        #endregion

        #region JSON 讀取輔助

        /// <summary>
        /// 讀取 JSON 檔案內容（UTF-8 編碼）
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>JSON 字串內容，失敗則返回 null</returns>
        public static string? ReadJsonFile(string fileName)
        {
            try
            {
                string fullPath = GetResourceFilePath(fileName);

                if (!File.Exists(fullPath))
                {
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] JSON file not found: {fullPath}",
                        LogLevel.Warning,
                        showInUi: false
                    );
                    return null;
                }

                // ?? 強制使用 UTF-8 編碼讀取（支援中文）
                return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[ResourcePathHelper] Failed to read JSON file '{fileName}': {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return null;
            }
        }

        /// <summary>
        /// 讀取 JSON 檔案內容（UTF-8 編碼，支援子目錄）
        /// </summary>
        /// <param name="subDirectory">子目錄名稱（選填）</param>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>JSON 字串內容，失敗則返回 null</returns>
        public static string? ReadJsonFile(string? subDirectory, string fileName)
        {
            try
            {
                string fullPath = string.IsNullOrWhiteSpace(subDirectory)
                    ? GetResourceFilePath(fileName)
                    : GetResourceFilePath(subDirectory, fileName);

                if (!File.Exists(fullPath))
                {
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] JSON file not found: {fullPath}",
                        LogLevel.Warning,
                        showInUi: false
                    );
                    return null;
                }

                // ?? 強制使用 UTF-8 編碼讀取（支援中文）
                return File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[ResourcePathHelper] Failed to read JSON file '{fileName}': {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return null;
            }
        }

        /// <summary>
        /// 非同步讀取 JSON 檔案內容（UTF-8 編碼）
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <returns>JSON 字串內容，失敗則返回 null</returns>
        public static async Task<string?> ReadJsonFileAsync(string fileName)
        {
            try
            {
                string fullPath = GetResourceFilePath(fileName);

                if (!File.Exists(fullPath))
                {
                    ComplianceContext.LogSystem(
                        $"[ResourcePathHelper] JSON file not found: {fullPath}",
                        LogLevel.Warning,
                        showInUi: false
                    );
                    return null;
                }

                // ?? 強制使用 UTF-8 編碼讀取（支援中文）
                return await File.ReadAllTextAsync(fullPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ComplianceContext.LogSystem(
                    $"[ResourcePathHelper] Failed to read JSON file '{fileName}': {ex.Message}",
                    LogLevel.Error,
                    showInUi: false
                );
                return null;
            }
        }

        #endregion

        #region 統計資訊

        /// <summary>
        /// 取得 Resources 目錄統計資訊
        /// </summary>
        /// <returns>統計資訊字串</returns>
        public static string GetStatistics()
        {
            try
            {
                if (!Directory.Exists(ResourcesDirectory))
                {
                    return "Resources directory not found";
                }

                var jsonFiles = Directory.GetFiles(ResourcesDirectory, "*.json", SearchOption.AllDirectories);
                var allFiles = Directory.GetFiles(ResourcesDirectory, "*.*", SearchOption.AllDirectories);
                var directories = Directory.GetDirectories(ResourcesDirectory, "*", SearchOption.AllDirectories);

                return $"Resources Directory: {ResourcesDirectory}\n" +
                       $"JSON Files: {jsonFiles.Length}\n" +
                       $"Total Files: {allFiles.Length}\n" +
                       $"Subdirectories: {directories.Length}";
            }
            catch (Exception ex)
            {
                return $"Failed to get statistics: {ex.Message}";
            }
        }

        #endregion
    }
}
