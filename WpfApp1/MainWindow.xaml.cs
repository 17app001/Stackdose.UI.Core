using System.Windows;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            // 🔥 設定 DataContext 為 ViewModel
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 🔥 不再需要在 CodeBehind 中訂閱事件，改用 XAML 附加行為
        }

        /// <summary>
        /// 視窗關閉時清理資源
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel.Cleanup();
        }
    }
}