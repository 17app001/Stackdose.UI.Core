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

            // 檢查是否在設計模式
            bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);

            if (!isDesignMode)
            {
                // 訂閱 Recipe 事件
                RecipeContext.RecipeLoaded += OnRecipeLoaded;
                RecipeContext.RecipeLoadFailed += OnRecipeLoadFailed;
                RecipeContext.RecipeChanged += OnRecipeChanged;

                // 控制項卸載時取消訂閱
                this.Unloaded += (s, e) =>
                {
                    RecipeContext.RecipeLoaded -= OnRecipeLoaded;
                    RecipeContext.RecipeLoadFailed -= OnRecipeLoadFailed;
                    RecipeContext.RecipeChanged -= OnRecipeChanged;
                };
            }

            // Loaded 事件處理
            this.Loaded += async (s, e) =>
            {
                if (!isDesignMode && AutoLoadOnStartup && !RecipeContext.IsInitialized)
                {
                    await LoadRecipeAsync();
                }

                UpdateDisplay();
            };
        }

        #region Dependency Properties

        /// <summary>
        /// Recipe 檔案路徑
        /// </summary>
        public static readonly DependencyProperty RecipeFilePathProperty =
            DependencyProperty.Register(nameof(RecipeFilePath), typeof(string), typeof(RecipeLoader),
                new PropertyMetadata("Recipe.json", OnRecipeFilePathChanged));

        public string RecipeFilePath
        {
            get => (string)GetValue(RecipeFilePathProperty);
            set => SetValue(RecipeFilePathProperty, value);
        }

        /// <summary>
        /// 是否在啟動時自動載入
        /// </summary>
        public static readonly DependencyProperty AutoLoadOnStartupProperty =
            DependencyProperty.Register(nameof(AutoLoadOnStartup), typeof(bool), typeof(RecipeLoader),
                new PropertyMetadata(true));

        public bool AutoLoadOnStartup
        {
            get => (bool)GetValue(AutoLoadOnStartupProperty);
            set => SetValue(AutoLoadOnStartupProperty, value);
        }

        /// <summary>
        /// 手動載入按鈕所需權限等級
        /// </summary>
        public static readonly DependencyProperty RequiredAccessLevelProperty =
            DependencyProperty.Register(nameof(RequiredAccessLevel), typeof(AccessLevel), typeof(RecipeLoader),
                new PropertyMetadata(AccessLevel.Instructor));

        public AccessLevel RequiredAccessLevel
        {
            get => (AccessLevel)GetValue(RequiredAccessLevelProperty);
            set => SetValue(RequiredAccessLevelProperty, value);
        }

        /// <summary>
        /// 是否顯示詳細資訊
        /// </summary>
        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register(nameof(ShowDetails), typeof(bool), typeof(RecipeLoader),
                new PropertyMetadata(true, OnShowDetailsChanged));

        public bool ShowDetails
        {
            get => (bool)GetValue(ShowDetailsProperty);
            set => SetValue(ShowDetailsProperty, value);
        }

        /// <summary>
        /// 標題文字
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(RecipeLoader),
                new PropertyMetadata("Recipe 管理"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region 事件處理

        private void OnRecipeLoaded(object? sender, Recipe recipe)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateDisplay();
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                // ?? Recipe 載入後，如果 PlcStatus 已經連線，則自動啟動監控
                var plcStatus = PlcContext.GlobalStatus;
                if (plcStatus?.CurrentManager != null && plcStatus.CurrentManager.IsConnected)
                {
                    int registeredCount = RecipeContext.StartMonitoring(plcStatus.CurrentManager, autoStart: true);
                    if (registeredCount > 0)
                    {
                        Helpers.ComplianceContext.LogSystem(
                            $"[Recipe] Auto-started monitoring: {registeredCount} parameters",
                            Models.LogLevel.Success,
                            showInUi: true
                        );
                    }
                }
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

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadRecipeAsync();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecipeContext.HasActiveRecipe)
            {
                await ReloadRecipeAsync();
            }
            else
            {
                CyberMessageBox.Show(
                    "No Recipe is currently loaded",
                    "Cannot Reload",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        #endregion

        #region 載入方法

        private async Task LoadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "載入中...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            bool success = await RecipeContext.LoadRecipeAsync(
                RecipeFilePath,
                isAutoLoad: false,
                setAsActive: true
            );

            LoadingIndicator.Visibility = Visibility.Collapsed;

            if (!success)
            {
                CyberMessageBox.Show(
                    RecipeContext.LastLoadMessage,
                    "Recipe 載入失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async Task ReloadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "重新載入中...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            bool success = await RecipeContext.ReloadCurrentRecipeAsync();

            LoadingIndicator.Visibility = Visibility.Collapsed;

            if (success)
            {
                CyberMessageBox.Show(
                    "Recipe 已成功重新載入",
                    "重新載入成功",
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
