using Stackdose.App.ModelB.Pages;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Templates.Pages;
using Stackdose.UI.Templates.Shell;
using System.Windows;
using System.Windows.Media;

namespace Stackdose.App.ModelB
{
    public partial class MainWindow : Window
    {
        private HomePage? _homePage;
        private MachineControlPage? _machineControlPage;
        private LogViewerPage? _logViewerPage;
        private UserManagementPage? _userManagementPage;
        private MainContainer? _mainContainer;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化頁面
            InitializePages();

            Loaded += OnMainWindowLoaded;
            
            // 記錄啟動日誌
            ComplianceContext.LogSystem("Model-B 3D Printing Tablet Device Started", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (_mainContainer == null)
            {
                _mainContainer = FindMainContainer(this);
            }

            if (_mainContainer == null)
            {
                ComplianceContext.LogSystem("Model-B 初始化失敗：找不到 MainContainer", Stackdose.Abstractions.Logging.LogLevel.Error, showInUi: true);
                return;
            }

            NavigateToHome();
        }

        private MainContainer? FindMainContainer(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is MainContainer container)
                {
                    return container;
                }

                var result = FindMainContainer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void InitializePages()
        {
            // 創建首頁
            _homePage = new HomePage();
            _homePage.MachineSelected += OnMachineSelected;
            
            // 創建機器控制頁面
            _machineControlPage = new MachineControlPage();
            _machineControlPage.BackRequested += OnBackToHome;

            _logViewerPage = new LogViewerPage();
            _userManagementPage = new UserManagementPage();
        }

        private void NavigateToHome()
        {
            if (_mainContainer != null && _homePage != null)
            {
                _mainContainer.SetContent(_homePage, "Home - Machine Overview");
            }
        }

        private void NavigateToMachineControl(string machineId)
        {
            if (_mainContainer != null && _machineControlPage != null)
            {
                _machineControlPage.SetMachineId(machineId);
                _mainContainer.SetContent(_machineControlPage, $"{machineId} - Detail Control");
            }
        }

        private void NavigateToLogs()
        {
            if (_mainContainer != null && _logViewerPage != null)
            {
                _mainContainer.SetContent(_logViewerPage, "Logs - System Events");
            }
        }

        private void NavigateToUsers()
        {
            if (_mainContainer != null && _userManagementPage != null)
            {
                _mainContainer.SetContent(_userManagementPage, "Users - Access Control");
            }
        }

        private void OnMachineSelected(object? sender, string machineId)
        {
            NavigateToMachineControl(machineId);
        }

        private void OnBackToHome(object? sender, EventArgs e)
        {
            NavigateToHome();
        }

        private void OnNavigate(object sender, string target)
        {
            switch (target)
            {
                case "Home":
                    NavigateToHome();
                    break;
                    
                case "Machine1":
                    NavigateToMachineControl("Model-B-01");
                    break;
                    
                case "Machine2":
                    NavigateToMachineControl("Model-B-02");
                    break;
                    
                case "Logs":
                    NavigateToLogs();
                    break;
                    
                case "Users":
                    NavigateToUsers();
                    break;
                    
                default:
                    ComplianceContext.LogSystem($"未知的導航目標: {target}", Stackdose.Abstractions.Logging.LogLevel.Warning, showInUi: true);
                    break;
            }
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ComplianceContext.LogSystem("User logged out", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
                SecurityContext.Logout();
                this.Close();
            }
        }

        private void OnClose(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to close the application?", "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ComplianceContext.LogSystem("Application closed", Stackdose.Abstractions.Logging.LogLevel.Info, showInUi: true);
                this.Close();
            }
        }

        private void OnMinimize(object? sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
