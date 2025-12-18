using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Secured Button with Permission Control and Theme Support
    /// </summary>
    public partial class SecuredButton : UserControl
    {
        public SecuredButton()
        {
            InitializeComponent();
            
            // ?? 檢查是否在設計模式
            bool isDesignMode = System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            
            if (!isDesignMode)
            {
                // 執行時：訂閱權限變更事件
                SecurityContext.AccessLevelChanged += OnAccessLevelChanged;
                
                // 初始化權限狀態
                UpdateAuthorization();
                
                // 當控制項卸載時取消訂閱
                this.Unloaded += (s, e) => SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
            }
            else
            {
                // 設計時：強制設定為已授權（讓所有按鈕都可見）
                IsAuthorized = true;
            }
            
            // ?? 無論設計時或執行時，都要初始化主題顏色
            UpdateThemeColors();
            
            // ?? 訂閱 Loaded 事件，確保在 XAML 屬性設定後再次更新
            this.Loaded += (s, e) => UpdateThemeColors();
        }

        #region Dependency Properties

        /// <summary>
        /// 按鈕內容
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(SecuredButton), 
                new PropertyMetadata("Button"));
        
        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// 所需權限等級
        /// </summary>
        public static readonly DependencyProperty RequiredLevelProperty =
            DependencyProperty.Register("RequiredLevel", typeof(AccessLevel), typeof(SecuredButton),
                new PropertyMetadata(AccessLevel.Operator, OnRequiredLevelChanged));

        public AccessLevel RequiredLevel
        {
            get => (AccessLevel)GetValue(RequiredLevelProperty);
            set => SetValue(RequiredLevelProperty, value);
        }

        /// <summary>
        /// 是否已授權（自動計算）
        /// </summary>
        public static readonly DependencyProperty IsAuthorizedProperty =
            DependencyProperty.Register("IsAuthorized", typeof(bool), typeof(SecuredButton), 
                new PropertyMetadata(false));

        public bool IsAuthorized
        {
            get => (bool)GetValue(IsAuthorizedProperty);
            private set => SetValue(IsAuthorizedProperty, value);
        }

        /// <summary>
        /// 按鈕顏色主題
        /// </summary>
        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(ButtonTheme), typeof(SecuredButton),
                new PropertyMetadata(ButtonTheme.Normal, OnThemeChanged));

        public ButtonTheme Theme
        {
            get => (ButtonTheme)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        /// <summary>
        /// 工具提示文字（自動生成）
        /// </summary>
        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register("TooltipText", typeof(string), typeof(SecuredButton), 
                new PropertyMetadata(string.Empty));

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            private set => SetValue(TooltipTextProperty, value);
        }

        /// <summary>
        /// 操作名稱（用於日誌記錄）
        /// </summary>
        public static readonly DependencyProperty OperationNameProperty =
            DependencyProperty.Register("OperationName", typeof(string), typeof(SecuredButton), 
                new PropertyMetadata(string.Empty));

        public string OperationName
        {
            get => (string)GetValue(OperationNameProperty);
            set => SetValue(OperationNameProperty, value);
        }

        /// <summary>
        /// 背景畫刷（基於主題）
        /// </summary>
        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register("BackgroundBrush", typeof(Brush), typeof(SecuredButton), 
                new FrameworkPropertyMetadata(
                    new SolidColorBrush(Color.FromRgb(0x60, 0x7D, 0x8B)), 
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnBackgroundBrushChanged));

        public Brush BackgroundBrush
        {
            get => (Brush)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        private static void OnBackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            #if DEBUG
            if (d is SecuredButton button)
            {
                System.Diagnostics.Debug.WriteLine($"[SecuredButton] BackgroundBrush Changed: {e.NewValue}");
            }
            #endif
        }

        /// <summary>
        /// 滑鼠懸停畫刷（基於主題）
        /// </summary>
        public static readonly DependencyProperty HoverBrushProperty =
            DependencyProperty.Register("HoverBrush", typeof(Brush), typeof(SecuredButton), 
                new FrameworkPropertyMetadata(
                    new SolidColorBrush(Color.FromRgb(0x45, 0x5A, 0x64)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush HoverBrush
        {
            get => (Brush)GetValue(HoverBrushProperty);
            set => SetValue(HoverBrushProperty, value);
        }

        #endregion

        #region 事件

        /// <summary>
        /// 點擊事件（只有在有權限時才會觸發）
        /// </summary>
        public event RoutedEventHandler? Click;

        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            // 更新活動時間（防止自動登出）
            SecurityContext.UpdateActivity();

            // 再次檢查權限（雙重保險）
            if (!SecurityContext.HasAccess(RequiredLevel))
            {
                string opName = !string.IsNullOrEmpty(OperationName) ? OperationName : Content?.ToString() ?? "此操作";
                SecurityContext.CheckAccess(RequiredLevel, opName);
                return;
            }

            // 觸發外部 Click 事件
            Click?.Invoke(this, e);
        }

        #endregion

        #region 權限與主題更新

        /// <summary>
        /// 當權限等級變更時觸發
        /// </summary>
        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateAuthorization);
        }

        /// <summary>
        /// 當所需權限等級變更時觸發
        /// </summary>
        private static void OnRequiredLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SecuredButton button)
                button.UpdateAuthorization();
        }

        /// <summary>
        /// 當主題變更時觸發
        /// </summary>
        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SecuredButton button)
            {
                // ?? 立即更新顏色（設計時也會執行）
                button.UpdateThemeColors();
            }
        }

        /// <summary>
        /// 更新授權狀態
        /// </summary>
        private void UpdateAuthorization()
        {
            bool hasAccess = SecurityContext.HasAccess(RequiredLevel);
            IsAuthorized = hasAccess;

            // 更新工具提示
            if (hasAccess)
            {
                TooltipText = $"權限等級: {RequiredLevel} ?";
            }
            else
            {
                string currentLevel = SecurityContext.CurrentSession.CurrentLevel.ToString();
                TooltipText = $"需要權限: {RequiredLevel}\n當前權限: {currentLevel} ?";
            }
        }

        /// <summary>
        /// 更新主題顏色
        /// </summary>
        private void UpdateThemeColors()
        {
            try
            {
                var (background, hover) = Theme switch
                {
                    ButtonTheme.Normal => ("#607D8B", "#455A64"),      // Blue Grey
                    ButtonTheme.Primary => ("#2196F3", "#1976D2"),     // Blue
                    ButtonTheme.Success => ("#4CAF50", "#388E3C"),     // Green
                    ButtonTheme.Warning => ("#FF9800", "#F57C00"),     // Orange
                    ButtonTheme.Error => ("#F44336", "#D32F2F"),       // Red
                    ButtonTheme.Info => ("#00BCD4", "#0097A7"),        // Cyan
                    _ => ("#607D8B", "#455A64")
                };

                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SecuredButton] UpdateThemeColors: Theme={Theme}, Background={background}");
                #endif

                // ?? 使用 SetValue 而不是屬性 setter（更適合 DependencyProperty）
                SetValue(BackgroundBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)));
                SetValue(HoverBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hover)));
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[SecuredButton] UpdateThemeColors Error: {ex.Message}");
                #endif
            }
        }

        #endregion
    }
}
