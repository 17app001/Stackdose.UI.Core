using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; // 用來做時鐘跳動

namespace Stackdose.UI.Core.Controls // 記得確認 namespace 對不對
{
    public partial class CyberFrame : UserControl
    {
        public CyberFrame()
        {
            InitializeComponent();

            // 啟動時鐘，每秒更新一次時間
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => { TimeText.Text = DateTime.Now.ToString("HH:mm:ss"); };
            timer.Start();
        }

        // ==========================================
        // 1. 定義 "Title" 屬性
        // ==========================================
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(CyberFrame), new PropertyMetadata("BOHUI SYSTEM"));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // ==========================================
        // 2. 定義 "ShowTime" 屬性 (是否顯示時間)
        // ==========================================
        public static readonly DependencyProperty ShowTimeProperty =
            DependencyProperty.Register("ShowTime", typeof(bool), typeof(CyberFrame), new PropertyMetadata(true));

        public bool ShowTime
        {
            get { return (bool)GetValue(ShowTimeProperty); }
            set { SetValue(ShowTimeProperty, value); }
        }
    }
}