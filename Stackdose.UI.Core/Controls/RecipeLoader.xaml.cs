using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Recipe 更竟北兜
    /// や穿笆更㎝も笆更,才 FDA 21 CFR Part 11 砏絛
    /// </summary>
    public partial class RecipeLoader : UserControl
    {
        public RecipeLoader()
        {
            InitializeComponent();

            // 浪琩琌砞璸家Α
            bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);

            if (!isDesignMode)
            {
                // 璹綷 Recipe ㄆン
                RecipeContext.RecipeLoaded += OnRecipeLoaded;
                RecipeContext.RecipeLoadFailed += OnRecipeLoadFailed;
                RecipeContext.RecipeChanged += OnRecipeChanged;

                // 北兜更璹綷
                this.Unloaded += (s, e) =>
                {
                    RecipeContext.RecipeLoaded -= OnRecipeLoaded;
                    RecipeContext.RecipeLoadFailed -= OnRecipeLoadFailed;
                    RecipeContext.RecipeChanged -= OnRecipeChanged;
                };
            }

            // Loaded ㄆン矪瞶
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
        /// Recipe 郎隔畖
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
        /// 琌币笆笆更
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
        /// も笆更秙┮惠舦单
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
        /// 琌陪ボ冈灿戈癟
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
        /// 夹肈ゅ
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(RecipeLoader),
                new PropertyMetadata("Recipe 恨瞶"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region ㄆン矪瞶

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

        #region 秙ㄆン

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

        #region 更よ猭

        private async Task LoadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "更い...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            // 1. 更 Recipe JSON 郎
            bool success = await RecipeContext.LoadRecipeAsync(
                RecipeFilePath,
                isAutoLoad: false,
                setAsActive: true
            );

            if (!success)
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                CyberMessageBox.Show(
                    RecipeContext.LastLoadMessage,
                    "Recipe 更ア毖",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // 2. 浪琩 PLC 琌硈絬狦硈絬玥笆更
            var plcStatus = Helpers.PlcContext.GlobalStatus;
            if (plcStatus?.CurrentManager != null && plcStatus.CurrentManager.IsConnected)
            {
                StatusText.Text = "更 Recipe  PLC い...";
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

                int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcStatus.CurrentManager);

                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (downloadCount > 0)
                {
                    StatusText.Text = $"Recipe 更更Θ: {downloadCount} 把计";
                    StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                    CyberMessageBox.Show(
                        $"Recipe loaded and downloaded successfully!\n\n" +
                        $"{downloadCount} parameters written to PLC.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    StatusText.Text = "Recipe 更Θ更ア毖";
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
                // PLC ゼ硈絬更 Recipe
                LoadingIndicator.Visibility = Visibility.Collapsed;
                StatusText.Text = "Recipe 更Θ (PLC ゼ硈絬)";
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                CyberMessageBox.Show(
                    "Recipe loaded successfully.\n\n" +
                    "Note: PLC is not connected. Recipe will be downloaded when PLC connects.",
                    "Recipe Loaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private async Task ReloadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "穝更い...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            // 1. 穝更 Recipe
            bool success = await RecipeContext.ReloadCurrentRecipeAsync();

            if (!success)
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                return;
            }

            // 2. 浪琩 PLC 琌硈絬狦硈絬玥笆更
            var plcStatus = Helpers.PlcContext.GlobalStatus;
            if (plcStatus?.CurrentManager != null && plcStatus.CurrentManager.IsConnected)
            {
                StatusText.Text = "更 Recipe  PLC い...";

                int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcStatus.CurrentManager);

                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (downloadCount > 0)
                {
                    StatusText.Text = $"Recipe 穝更更Θ: {downloadCount} 把计";
                    StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                    CyberMessageBox.Show(
                        $"Recipe reloaded and downloaded successfully!\n\n" +
                        $"{downloadCount} parameters written to PLC.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    StatusText.Text = "Recipe 穝更Θ更ア毖";
                    StatusText.Foreground = new SolidColorBrush(Colors.Orange);
                }
            }
            else
            {
                LoadingIndicator.Visibility = Visibility.Collapsed;
                StatusText.Text = "Recipe 穝更Θ (PLC ゼ硈絬)";
                StatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);

                CyberMessageBox.Show(
                    "Recipe reloaded successfully.\n\n" +
                    "Note: PLC is not connected.",
                    "Recipe Reloaded",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        #endregion

        #region 陪ボ穝

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
