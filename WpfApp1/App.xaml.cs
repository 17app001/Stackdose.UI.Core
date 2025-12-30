using System.Configuration;
using System.Data;
using System.Windows;
using Stackdose.Hardware.Plc;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // 🔥 在靜態建構函數中設定，確保最早執行
        static App()
        {
            #if DEBUG
            PlcClientFactory.UseSimulator = true;
            System.Diagnostics.Debug.WriteLine("🤖 [App.Static] 開發模式：已啟用 PLC 模擬器");
            #endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            #if DEBUG
            // 再次確認（雙重保險）
            PlcClientFactory.UseSimulator = true;
            System.Diagnostics.Debug.WriteLine("🤖 [App.OnStartup] 開發模式：已啟用 PLC 模擬器");
            #endif

            base.OnStartup(e);
        }
    }
}
