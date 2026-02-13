using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Helpers.UI;
using Stackdose.UI.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stackdose.UI.Core.Controls
{
    /// <summary>
    /// Secured button with permission checks and theme-aware colors.
    /// </summary>
    public partial class SecuredButton : UserControl
    {
        private readonly bool _isDesignMode;
        private bool _securityEventSubscribed;

        public SecuredButton()
        {
            InitializeComponent();

            _isDesignMode = ControlRuntime.IsDesignMode(this);

            Loaded += SecuredButton_Loaded;
            Unloaded += SecuredButton_Unloaded;

            if (_isDesignMode)
            {
                IsAuthorized = true;
            }
            else
            {
                SubscribeSecurityEvents();
                UpdateAuthorization();
            }

            UpdateThemeColors();
        }

        #region Dependency Properties

        public new static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(object), typeof(SecuredButton),
                new PropertyMetadata("Button"));

        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly DependencyProperty RequiredLevelProperty =
            DependencyProperty.Register("RequiredLevel", typeof(AccessLevel), typeof(SecuredButton),
                new PropertyMetadata(AccessLevel.Operator, OnRequiredLevelChanged));

        public AccessLevel RequiredLevel
        {
            get => (AccessLevel)GetValue(RequiredLevelProperty);
            set => SetValue(RequiredLevelProperty, value);
        }

        public static readonly DependencyProperty IsAuthorizedProperty =
            DependencyProperty.Register("IsAuthorized", typeof(bool), typeof(SecuredButton),
                new PropertyMetadata(false));

        public bool IsAuthorized
        {
            get => (bool)GetValue(IsAuthorizedProperty);
            private set => SetValue(IsAuthorizedProperty, value);
        }

        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(ButtonTheme), typeof(SecuredButton),
                new PropertyMetadata(ButtonTheme.Normal, OnThemeChanged));

        public ButtonTheme Theme
        {
            get => (ButtonTheme)GetValue(ThemeProperty);
            set => SetValue(ThemeProperty, value);
        }

        public static readonly DependencyProperty TooltipTextProperty =
            DependencyProperty.Register("TooltipText", typeof(string), typeof(SecuredButton),
                new PropertyMetadata(string.Empty));

        public string TooltipText
        {
            get => (string)GetValue(TooltipTextProperty);
            private set => SetValue(TooltipTextProperty, value);
        }

        public static readonly DependencyProperty OperationNameProperty =
            DependencyProperty.Register("OperationName", typeof(string), typeof(SecuredButton),
                new PropertyMetadata(string.Empty));

        public string OperationName
        {
            get => (string)GetValue(OperationNameProperty);
            set => SetValue(OperationNameProperty, value);
        }

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
            if (d is SecuredButton)
            {
                System.Diagnostics.Debug.WriteLine($"[SecuredButton] BackgroundBrush changed: {e.NewValue}");
            }
#endif
        }

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

        #region Events

        public event RoutedEventHandler? Click;

        private void SecuredButton_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThemeColors();

            if (_isDesignMode)
            {
                IsAuthorized = true;
                return;
            }

            SubscribeSecurityEvents();
            UpdateAuthorization();
        }

        private void SecuredButton_Unloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeSecurityEvents();
        }

        private void InnerButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityContext.UpdateActivity();

            if (!SecurityContext.HasAccess(RequiredLevel))
            {
                var operation = string.IsNullOrWhiteSpace(OperationName)
                    ? Content?.ToString() ?? "此操作"
                    : OperationName;

                SecurityContext.CheckAccess(RequiredLevel, operation);
                return;
            }

            Click?.Invoke(this, e);
        }

        private void OnAccessLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(UpdateAuthorization);
        }

        #endregion

        #region State Update

        private static void OnRequiredLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SecuredButton button)
            {
                return;
            }

            if (button._isDesignMode)
            {
                button.IsAuthorized = true;
                return;
            }

            button.UpdateAuthorization();
        }

        private static void OnThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SecuredButton button)
            {
                button.UpdateThemeColors();
            }
        }

        private void UpdateAuthorization()
        {
            if (_isDesignMode)
            {
                IsAuthorized = true;
                TooltipText = $"Design Mode - {RequiredLevel}";
                return;
            }

            var hasAccess = SecurityContext.HasAccess(RequiredLevel);
            IsAuthorized = hasAccess;

            if (hasAccess)
            {
                TooltipText = $"Permission: {RequiredLevel}";
                return;
            }

            var currentLevel = SecurityContext.CurrentSession.CurrentLevel;
            TooltipText = $"Required: {RequiredLevel}{Environment.NewLine}Current: {currentLevel}";
        }

        private void UpdateThemeColors()
        {
            try
            {
                var (background, hover) = Theme switch
                {
                    ButtonTheme.Normal => ("#607D8B", "#455A64"),
                    ButtonTheme.Primary => ("#2196F3", "#1976D2"),
                    ButtonTheme.Success => ("#4CAF50", "#388E3C"),
                    ButtonTheme.Warning => ("#FF9800", "#F57C00"),
                    ButtonTheme.Error => ("#F44336", "#D32F2F"),
                    ButtonTheme.Info => ("#00BCD4", "#0097A7"),
                    _ => ("#607D8B", "#455A64")
                };

                SetValue(BackgroundBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)));
                SetValue(HoverBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(hover)));
            }
            catch (Exception)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[SecuredButton] UpdateThemeColors error");
#endif
            }
        }

        private void SubscribeSecurityEvents()
        {
            if (_securityEventSubscribed)
            {
                return;
            }

            SecurityContext.AccessLevelChanged += OnAccessLevelChanged;
            _securityEventSubscribed = true;
        }

        private void UnsubscribeSecurityEvents()
        {
            if (!_securityEventSubscribed)
            {
                return;
            }

            SecurityContext.AccessLevelChanged -= OnAccessLevelChanged;
            _securityEventSubscribed = false;
        }

        #endregion
    }
}
