using CastorPlugin.Core;
using CastorPlugin.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace CastorPlugin.Views
{
    public partial class CastorPluginView
    {
        private bool _isWebViewInitialized;

        public CastorPluginView(CastorPluginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            Loaded += CastorPluginView_Loaded;
            Unloaded += CastorPluginView_Unloaded;
        }

        private async void CastorPluginView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
        }

        private void CastorPluginView_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupWebView();
        }

        private async Task InitializeWebView()
        {
            try
            {
                if (webView != null && !_isWebViewInitialized)
                {
                    // 设置 WebView2 用户数据文件夹
                    string tempPath = Path.Combine(Path.GetTempPath(), "WebView2UserData");
                    var env = await CoreWebView2Environment.CreateAsync(userDataFolder: tempPath);
                    await webView.EnsureCoreWebView2Async(env);

                    // 设置安全选项和事件处理
                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                        webView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                        webView.CoreWebView2.Settings.IsScriptEnabled = true;
                        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                        webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                        webView.NavigationCompleted += WebView_NavigationCompleted;
                    }

                    _isWebViewInitialized = true;
                    Log.Information("CastorPluginView WebView initialized successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing WebView in CastorPluginView: {ex.Message}");
            }
        }

        private void CleanupWebView()
        {
            try
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                    webView.NavigationCompleted -= WebView_NavigationCompleted;
                    
                    webView.CoreWebView2.Navigate("about:blank");
                }

                _isWebViewInitialized = false;
                Log.Information("WebView resources cleaned up");
            }
            catch (Exception ex)
            {
                Log.Error($"Error cleaning up WebView resources: {ex.Message}");
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // 阻止新窗口打开，在当前窗口中导航
            e.Handled = true;
            if (webView?.CoreWebView2 == null || string.IsNullOrWhiteSpace(e.Uri))
            {
                return;
            }

            webView.CoreWebView2.Navigate(e.Uri);
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                Log.Information($"Navigation completed successfully: {webView.Source}");
            }
            else
            {
                Log.Warning($"Navigation failed with error: {e.WebErrorStatus}");
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // 测试功能
            Log.Information("Test button clicked");
        }
    }
}
