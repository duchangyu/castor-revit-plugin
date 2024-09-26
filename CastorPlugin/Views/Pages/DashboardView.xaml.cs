using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CastorPlugin.UserControls;

namespace CastorPlugin.Views.Pages
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardView : Page
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 测试按钮点击事件处理程序
        /// </summary>
        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            // 示例：向 WebView 发送消息
            await webView.SendMessageToWebViewAsync(new { type = "test", content = "这是一条测试消息" });
        }

        /// <summary>
        /// 处理从 WebView2 接收到的消息
        /// </summary>
        private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
        {
            // 这里处理从 Web 页面接收到的消息
            MessageBox.Show($"收到来自 {e.Source} 的消息：{e.Message}");
        }
    }
}
