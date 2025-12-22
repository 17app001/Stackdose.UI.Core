using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Recipe 載入器控制項
    /// 支援自動載入和手動載入,符合 FDA 21 CFR Part 11 規範
    /// </summary>
    public partial class RecipeLoader : UserControl
    {
        public RecipeLoader()
        {
            InitializeComponent();

            // 訂閱 Recipe 狀態變更事件
            RecipeContext.RecipeLoaded += OnRecipeLoaded;

            // 初始化顯示
            UpdateDisplay();

            // ? 預設選擇 Recipe 1
            _selectedRecipeNumber = 1;
            UpdateRecipeButtonStates();

            // 控制項卸載時取消訂閱
            this.Unloaded += (s, e) => RecipeContext.RecipeLoaded -= OnRecipeLoaded;
        }

        #region Dependency Properties

        /// <summary>
        /// Recipe 檔案路徑
        /// </summary>
        public static readonly DependencyProperty RecipeFilePathProperty =
            DependencyProperty.Register("RecipeFilePath", typeof(string), typeof(RecipeLoader), new PropertyMetadata("Recipe.json", OnRecipeFilePathChanged));

        public string RecipeFilePath
        {
            get => (string)GetValue(RecipeFilePathProperty);
            set => SetValue(RecipeFilePathProperty, value);
        }

        /// <summary>
        /// 是否在啟動時自動載入
        /// </summary>
        public static readonly DependencyProperty AutoLoadOnStartupProperty =
            DependencyProperty.Register("AutoLoadOnStartup", typeof(bool), typeof(RecipeLoader), new PropertyMetadata(false));

        public bool AutoLoadOnStartup
        {
            get => (bool)GetValue(AutoLoadOnStartupProperty);
            set => SetValue(AutoLoadOnStartupProperty, value);
        }

        /// <summary>
        /// 手動載入按鈕所需權限等級
        /// </summary>
        public static readonly DependencyProperty RequiredAccessLevelProperty =
            DependencyProperty.Register("RequiredAccessLevel", typeof(AccessLevel), typeof(RecipeLoader), new PropertyMetadata(AccessLevel.Instructor));

        public AccessLevel RequiredAccessLevel
        {
            get => (AccessLevel)GetValue(RequiredAccessLevelProperty);
            set => SetValue(RequiredAccessLevelProperty, value);
        }

        /// <summary>
        /// 是否顯示詳細資訊
        /// </summary>
        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register("ShowDetails", typeof(bool), typeof(RecipeLoader), new PropertyMetadata(true, OnShowDetailsChanged));

        public bool ShowDetails
        {
            get => (bool)GetValue(ShowDetailsProperty);
            set => SetValue(ShowDetailsProperty, value);
        }

        /// <summary>
        /// 標題文字
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(RecipeLoader), new PropertyMetadata("Recipe 配方管理"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// 當前選擇的 Recipe 編號（1, 2, 3）
        /// </summary>
        private int _selectedRecipeNumber = 1;

        #endregion

        #region 事件處理

        private void OnRecipeLoaded(object? sender, Recipe recipe)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDisplay();
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
            });
        }

        private void OnRecipeLoadFailed(object? sender, string errorMessage)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDisplay();
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            });
        }

        private void OnRecipeChanged(object? sender, Recipe recipe)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDisplay();
            });
        }

        private static void OnRecipeFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecipeLoader loader)
            {
                RecipeContext.DefaultRecipeFilePath = (string)e.NewValue;
            }
        }

        private static void OnShowDetailsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RecipeLoader loader)
            {
                loader.DetailsPanel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region 按鈕事件

        private void Recipe1Button_Click(object sender, RoutedEventArgs e)
        {
            _selectedRecipeNumber = 1;
            UpdateRecipeButtonStates();
            StatusText.Text = "Recipe 1 selected";
            StatusText.Foreground = new SolidColorBrush(Colors.Cyan);
        }

        private void Recipe2Button_Click(object sender, RoutedEventArgs e)
        {
            _selectedRecipeNumber = 2;
            UpdateRecipeButtonStates();
            StatusText.Text = "Recipe 2 selected";
            StatusText.Foreground = new SolidColorBrush(Colors.Cyan);
        }

        private void Recipe3Button_Click(object sender, RoutedEventArgs e)
        {
            _selectedRecipeNumber = 3;
            UpdateRecipeButtonStates();
            StatusText.Text = "Recipe 3 selected";
            StatusText.Foreground = new SolidColorBrush(Colors.Cyan);
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadRecipeAsync();
        }

        /// <summary>
        /// 更新 Recipe 按鈕狀態（顯示選中狀態）
        /// </summary>
        private void UpdateRecipeButtonStates()
        {
            // 重置所有按鈕的 Theme
            Recipe1Button.Theme = _selectedRecipeNumber == 1 ? ButtonTheme.Success : ButtonTheme.Primary;
            Recipe2Button.Theme = _selectedRecipeNumber == 2 ? ButtonTheme.Success : ButtonTheme.Primary;
            Recipe3Button.Theme = _selectedRecipeNumber == 3 ? ButtonTheme.Success : ButtonTheme.Primary;
        }

        #endregion

        #region 載入方法

        private async Task LoadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "載入中...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            // ? 根據選擇的 Recipe 編號決定檔案路徑
            string recipeFile = $"Recipe{_selectedRecipeNumber}.json";

            // 1. 載入 Recipe JSON 檔案
            bool success = await RecipeContext.LoadRecipeAsync(
                recipeFile,
                isAutoLoad: false,
                setAsActive: true
            );

            if (!success)
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                CyberMessageBox.Show(
                    RecipeContext.LastLoadMessage,
                    "Recipe 載入失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // 2. 檢查 PLC 是否已連線，如果連線則自動下載
            var plcStatus = Helpers.PlcContext.GlobalStatus;
            if (plcStatus?.CurrentManager != null && plcStatus.CurrentManager.IsConnected)
            {
                StatusText.Text = "下載 Recipe 到 PLC 中...";
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

                int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcStatus.CurrentManager);

                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (downloadCount > 0)
                {
                    StatusText.Text = $"Recipe {_selectedRecipeNumber} 載入並下載成功: {downloadCount} 個參數";
                    StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                    CyberMessageBox.Show(
                        $"Recipe {_selectedRecipeNumber} loaded and downloaded successfully!\n\n" +
                        $"{downloadCount} parameters written to PLC.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    StatusText.Text = $"Recipe {_selectedRecipeNumber} 載入成功，但下載失敗";
                    StatusText.Foreground = new SolidColorBrush(Colors.Orange);

                    CyberMessageBox.Show(
                        "Recipe loaded but download to PLC failed. Check logs for details.",
                        "Partial Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
            else
            {
                // PLC 未連線，只載入 Recipe
                LoadingIndicator.Visibility = Visibility.Collapsed;
                StatusText.Text = $"Recipe {_selectedRecipeNumber} 載入成功 (PLC 未連線)";
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                CyberMessageBox.Show(
                    $"Recipe {_selectedRecipeNumber} loaded successfully.\n\n" +
                    "Note: PLC is not connected. Recipe will be downloaded when PLC connects.",
                    "Recipe Loaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        #endregion

        #region 顯示更新

        private void UpdateDisplay()
        {
            if (RecipeContext.HasActiveRecipe && RecipeContext.CurrentRecipe != null)
            {
                var recipe = RecipeContext.CurrentRecipe;

                RecipeNameText.Text = recipe.RecipeName;
                RecipeVersionText.Text = $"v{recipe.Version}";
                RecipeIdText.Text = $"ID: {recipe.RecipeId}";
                ItemCountText.Text = $"{recipe.EnabledItemCount} items";
                LastLoadTimeText.Text = RecipeContext.LastLoadTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

                StatusText.Text = RecipeContext.LastLoadMessage;
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                NoRecipePanel.Visibility = Visibility.Collapsed;
                RecipeInfoPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusText.Text = "No Recipe loaded";
                StatusText.Foreground = new SolidColorBrush(Colors.Gray);

                NoRecipePanel.Visibility = Visibility.Visible;
                RecipeInfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
