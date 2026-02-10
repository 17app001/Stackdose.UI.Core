using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Recipe ���J�����
    /// �䴩�۰ʸ��J�M��ʸ��J,�ŦX FDA 21 CFR Part 11 �W�d
    /// </summary>
    public partial class RecipeLoader : UserControl
    {
        public RecipeLoader()
        {
            InitializeComponent();

            // �q�\ Recipe ���A�ܧ�ƥ�
            RecipeContext.RecipeLoaded += OnRecipeLoaded;

            // ��l�����
            UpdateDisplay();

            // ? �w�]��� Recipe 1
            _selectedRecipeNumber = 1;
            UpdateRecipeButtonStates();

            // ��������ɨ����q�\
            this.Unloaded += (s, e) => RecipeContext.RecipeLoaded -= OnRecipeLoaded;
        }

        #region Dependency Properties

        /// <summary>
        /// Recipe �ɮ׸��|
        /// </summary>
        public static readonly DependencyProperty RecipeFilePathProperty =
            DependencyProperty.Register("RecipeFilePath", typeof(string), typeof(RecipeLoader), new PropertyMetadata("Recipe.json", OnRecipeFilePathChanged));

        public string RecipeFilePath
        {
            get => (string)GetValue(RecipeFilePathProperty);
            set => SetValue(RecipeFilePathProperty, value);
        }

        /// <summary>
        /// �O�_�b�Ұʮɦ۰ʸ��J
        /// </summary>
        public static readonly DependencyProperty AutoLoadOnStartupProperty =
            DependencyProperty.Register("AutoLoadOnStartup", typeof(bool), typeof(RecipeLoader), new PropertyMetadata(false));

        public bool AutoLoadOnStartup
        {
            get => (bool)GetValue(AutoLoadOnStartupProperty);
            set => SetValue(AutoLoadOnStartupProperty, value);
        }

        /// <summary>
        /// ��ʸ��J���s�һ��v������
        /// </summary>
        public static readonly DependencyProperty RequiredAccessLevelProperty =
            DependencyProperty.Register("RequiredAccessLevel", typeof(AccessLevel), typeof(RecipeLoader), new PropertyMetadata(AccessLevel.Instructor));

        public AccessLevel RequiredAccessLevel
        {
            get => (AccessLevel)GetValue(RequiredAccessLevelProperty);
            set => SetValue(RequiredAccessLevelProperty, value);
        }

        /// <summary>
        /// �O�_��ܸԲӸ�T
        /// </summary>
        public static readonly DependencyProperty ShowDetailsProperty =
            DependencyProperty.Register("ShowDetails", typeof(bool), typeof(RecipeLoader), new PropertyMetadata(true, OnShowDetailsChanged));

        public bool ShowDetails
        {
            get => (bool)GetValue(ShowDetailsProperty);
            set => SetValue(ShowDetailsProperty, value);
        }

        /// <summary>
        /// ���D��r
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(RecipeLoader), new PropertyMetadata("Recipe �t��޲z"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// ���e��ܪ� Recipe �s���]1, 2, 3�^
        /// </summary>
        private int _selectedRecipeNumber = 1;

        #endregion

        #region �ƥ�B�z

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

        #region ���s�ƥ�

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
        /// ��s Recipe ���s���A�]��ܿ襤���A�^
        /// </summary>
        private void UpdateRecipeButtonStates()
        {
            // ���m�Ҧ����s�� Theme
            Recipe1Button.Theme = _selectedRecipeNumber == 1 ? ButtonTheme.Success : ButtonTheme.Primary;
            Recipe2Button.Theme = _selectedRecipeNumber == 2 ? ButtonTheme.Success : ButtonTheme.Primary;
            Recipe3Button.Theme = _selectedRecipeNumber == 3 ? ButtonTheme.Success : ButtonTheme.Primary;
        }

        #endregion

        #region ���J��k

        private async Task LoadRecipeAsync()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            StatusText.Text = "���J��...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            // ? �ھڿ�ܪ� Recipe �s���M�w�ɮ׸��|
            string recipeFile = $"Recipe{_selectedRecipeNumber}.json";

            // 1. ���J Recipe JSON �ɮ�
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
                    "Recipe ���J����",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // 2. �ˬd PLC �O�_�w�s�u�A�p�G�s�u�h�۰ʤU��
            var plcStatus = Helpers.PlcContext.GlobalStatus;
            if (plcStatus?.CurrentManager != null && plcStatus.CurrentManager.IsConnected)
            {
                StatusText.Text = "�U�� Recipe �� PLC ��...";
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

                int downloadCount = await RecipeContext.DownloadRecipeToPLCAsync(plcStatus.CurrentManager);

                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (downloadCount > 0)
                {
                    StatusText.Text = $"Recipe {_selectedRecipeNumber} loaded and downloaded: {downloadCount} parameters";
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
                    StatusText.Text = $"Recipe {_selectedRecipeNumber} loaded, but PLC download failed";
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
                // PLC ���s�u�A�u���J Recipe
                LoadingIndicator.Visibility = Visibility.Collapsed;
                StatusText.Text = $"Recipe {_selectedRecipeNumber} loaded (PLC not connected)";
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

        #region ��ܧ�s

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
