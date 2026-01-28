using Stackdose.UI.Templates.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Stackdose.UI.Templates.Shell
{
    /// <summary>
    /// 主容器（Shell）：包含固定的 Header、Navigation、BottomBar
    /// 中間內容由「使用者專案(App)」決定並塞入
    /// </summary>
    public partial class MainContainer : UserControl
    {
        /// <summary>
        /// 導航請求：把 NavigationTarget 丟出去，讓 App 決定要顯示哪個 Page
        /// </summary>
        public event EventHandler<string>? NavigationRequested;

        /// <summary>
        /// Header 的登出請求（可選）
        /// </summary>
        public event EventHandler? LogoutRequested;

        /// <summary>
        /// 視窗控制請求（可選；也可直接在這裡處理）
        /// </summary>
        public event EventHandler? CloseRequested;
        public event EventHandler? MinimizeRequested;

        public MainContainer()
        {
            InitializeComponent();
        }


        private void Header_DragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                Window.GetWindow(this)?.DragMove();
        }
        /// <summary>
        /// 外部（App）設定主內容與標題
        /// </summary>
        public void SetContent(object content, string title)
        {
            ContentArea.Content = content;
            AppHeaderControl.PageTitle = title;
        }

        private void OnLogout(object sender, RoutedEventArgs e)
        {
            // Templates 不負責「登出後做什麼」，丟給 App 決定
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            // 兩種作法擇一：
            // A) 直接在 Templates 做（你原本的方式）
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
                return;
            }

            // B) 丟給 App（保留給未來）
            MinimizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            // 這種「文案」通常會想客製，所以建議丟出去
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnNavigate(object sender, NavigationItem e)
        {
            // Templates 不認識任何 Page，只傳 key
            NavigationRequested?.Invoke(this, e.NavigationTarget);
        }
    }
}
