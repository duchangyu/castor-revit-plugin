using CastorPlugin.Core;
using CastorPlugin.ViewModels.Pages;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using Wpf.Ui.Controls;
using System.Windows.Controls;
using CastorPlugin.UserControls;
using System;
using System.Threading.Tasks;

namespace CastorPlugin.Views.Pages
{
    /// <summary>
    /// DigView.xaml 的交互逻辑
    /// </summary>
    public partial class DigView : Page
    {
        public DigView(DigViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;

            // Initialize WebView2 when the page is loaded
            Loaded += DigView_Loaded;
            Unloaded += DigView_Unloaded;
        }

        public DigViewModel ViewModel { get; }

        private async void DigView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            await ViewModel.UpdateDigCounts();
        }

        private void DigView_Unloaded(object sender, RoutedEventArgs e)
        {
            // 通知 ViewModel WebView 不再可用
            ViewModel.SetWebViewInitialized(false);
        }

        private async Task InitializeWebView()
        {
            try
            {
                // 等待 WebView2 控件初始化完成
                if (webView != null)
                {
                    // 确保 WebView2 控件已初始化
                    await webView.InitializeAsync();
                    
                    // 注册导航完成事件以确保我们知道何时 WebView 已完全加载
                    webView.WebView.NavigationCompleted += WebView_NavigationCompleted;
                    
                    // 通知 ViewModel WebView 已初始化
                    ViewModel.SetWebViewInitialized(true);

                    // 此时 WebView 已准备好接收 URL 更新
                    Log.Information("WebView initialized successfully");
                }
                else
                {
                    Log.Warning("WebView control is null during initialization");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"WebView initialization failed: {ex.Message}");
                // 可能需要向用户显示错误通知
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // 处理导航完成事件
            if (e.IsSuccess)
            {
                Log.Information($"WebView navigation completed successfully to: {webView.WebView.Source}");
            }
            else
            {
                Log.Warning($"WebView navigation failed. Error code: {e.WebErrorStatus}");
            }
        }

        private void WebView_WebMessageReceived(object sender, WebMessageReceivedEventArgs e)
        {
            // 处理接收到的消息
            try
            {
                string message = e.Message;
                string source = e.Source;
                
                Log.Information($"Received web message from {source}: {message}");
                
                // 根据需要处理消息
                // ViewModel.HandleWebMessage(message, source);
            }
            catch (Exception ex)
            {
                Log.Error($"Error processing web message: {ex.Message}");
            }
        }
    }
}
