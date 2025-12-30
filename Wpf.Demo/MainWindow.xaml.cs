using System.Windows;
using Stackdose.UI.Core.Helpers;
using Stackdose.UI.Core.Models;
using Stackdose.UI.Core.Controls;

namespace Wpf.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 🔑 一行程式切換權限（改這裡即可）
            SecurityContext.QuickLogin(AccessLevel.Admin);  // Guest / Operator / Instructor / Supervisor / Admin
        }      
    }
}