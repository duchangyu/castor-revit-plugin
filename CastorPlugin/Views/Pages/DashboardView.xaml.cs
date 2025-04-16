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
using CastorPlugin.Core;
using CastorPlugin.UserControls;

namespace CastorPlugin.Views.Pages
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    public partial class DashboardView : Page
    {
        private bool _isWebViewInitialized;

        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
            Unloaded += DashboardView_Unloaded;
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            _isWebViewInitialized = false;
        }

        private async Task InitializeWebView()
        {
            try
            {
                if (webView != null && !_isWebViewInitialized)
                {
                    await webView.InitializeAsync();
                    _isWebViewInitialized = true;
                    Log.Information("Dashboard WebView initialized successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing Dashboard WebView: {ex.Message}");
                MessageBox.Show($"WebView initialization failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 测试按钮点击事件处理程序
        /// </summary>
        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isWebViewInitialized)
                {
                    MessageBox.Show("WebView not yet initialized. Please try again later.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 示例：向 WebView 发送消息
                await webView.SendMessageToWebViewAsync(new { type = "test", content = "这是一条测试消息" });
                Log.Information("Test message sent to WebView");
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending message to WebView: {ex.Message}");
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 处理从 WebView2 接收到的消息
        /// </summary>
        private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
        {
            try
            {
                // 这里处理从 Web 页面接收到的消息
                Log.Information($"Received message from {e.Source}: {e.Message}");
                MessageBox.Show($"收到来自 {e.Source} 的消息：{e.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing WebView message: {ex.Message}");
            }
        }
    }
}
