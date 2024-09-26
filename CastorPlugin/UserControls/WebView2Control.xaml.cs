using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CastorPlugin.UserControls
{
    /// <summary>
    /// WebView2Control 的交互逻辑
    /// </summary>
    public partial class WebView2Control : UserControl
    {
        /// <summary>
        /// Source 依赖属性，用于设置和获取 WebView2 的 URL
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(WebView2Control), 
                new PropertyMetadata("about:blank", OnSourceChanged));

        /// <summary>
        /// AllowedOrigins 依赖属性，用于设置允许的源域名列表
        /// </summary>
        public static readonly DependencyProperty AllowedOriginsProperty =
            DependencyProperty.Register("AllowedOrigins", typeof(IEnumerable<string>), typeof(WebView2Control), 
                new PropertyMetadata(null));

        /// <summary>
        /// 获取或设置 WebView2 的 URL
        /// </summary>
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value ?? "about:blank"); }
        }

        /// <summary>
        /// 获取或设置允许的源域名列表
        /// </summary>
        public IEnumerable<string> AllowedOrigins
        {
            get { return (IEnumerable<string>)GetValue(AllowedOriginsProperty); }
            set { SetValue(AllowedOriginsProperty, value); }
        }

        /// <summary>
        /// 当从 Web 页面接收到消息时触发的事件
        /// </summary>
        public event EventHandler<WebMessageReceivedEventArgs> WebMessageReceived;

        public WebView2Control()
        {
            InitializeComponent();
            Loaded += WebView2Control_Loaded;
        }

        /// <summary>
        /// 控件加载完成时初始化 WebView2
        /// </summary>
        private async void WebView2Control_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView2();
        }

        /// <summary>
        /// 初始化 WebView2 并设置安全选项
        /// </summary>
        private async Task InitializeWebView2()
        {
            if (InnerWebView.CoreWebView2 == null)
            {
                try
                {
                    // 检查 WebView2 运行时是否已安装
                    var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                    if (string.IsNullOrEmpty(version))
                    {
                        throw new WebView2RuntimeNotFoundException();
                    }

                    // 设置 WebView2 用户数据文件夹
                    string tempPath = Path.Combine(Path.GetTempPath(), "WebView2UserData");
                    var env = await CoreWebView2Environment.CreateAsync(userDataFolder: tempPath);
                    await InnerWebView.EnsureCoreWebView2Async(env);

                    // 设置安全选项
                    InnerWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false; // 禁用默认的脚本对话框
                    InnerWebView.CoreWebView2.Settings.AreDevToolsEnabled = false; // 禁用开发者工具
                    InnerWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false; // 禁止主机对象
                    InnerWebView.CoreWebView2.Settings.IsScriptEnabled = true; // 启用 JavaScript
                    InnerWebView.CoreWebView2.Settings.IsWebMessageEnabled = true; // 启用 Web 消息

                    // 设置事件处理程序
                    InnerWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                    InnerWebView.NavigationStarting += InnerWebView_NavigationStarting;
                    InnerWebView.WebMessageReceived += InnerWebView_WebMessageReceived;

                    //// 设置内容安全策略 (CSP)s
                    //InnerWebView.CoreWebView2.Settings.UserAgent += " CSP-Policy";
                    //InnerWebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                    //InnerWebView.CoreWebView2.WebResourceRequested += (sender, e) =>
                    //{
                    //    var csp = "default-src 'self'; script-src 'self'; style-src 'self';";
                    //    e.Response.Headers.AppendHeader("Content-Security-Policy", csp);
                    //};

                    // 如果设置了初始 URL，则导航到该 URL
                    if (!string.IsNullOrEmpty(Source))
                    {
                        InnerWebView.CoreWebView2.Navigate(Source);
                    }
                }
                catch (WebView2RuntimeNotFoundException)
                {
                    Log.Error("WebView2 运行时未安装。请安装 WebView2 运行时后再试。");
                 
                    //TODO： 可以在这里提供下载链接或进一步的指导
                }
                catch (Exception ex)
                {
                    Log.Error($"初始化 WebView2 时发生错误: {ex.Message}");
                   
                }
            }
        }

        /// <summary>
        /// 处理新窗口请求
        /// </summary>
        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true; // 阻止新窗口打开
            if (IsAllowedOrigin(e.Uri))
            {
                InnerWebView.CoreWebView2.Navigate(e.Uri); // 在当前 WebView 中导航
            }
        }

        /// <summary>
        /// 处理导航开始事件
        /// </summary>
        private void InnerWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!IsAllowedOrigin(e.Uri))
            {
                e.Cancel = true; // 如果不是允许的源，取消导航
                return;
            }
            Source = e.Uri;
        }

        /// <summary>
        /// 处理接收到的 Web 消息
        /// </summary>
        private void InnerWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (IsAllowedOrigin(e.Source))
            {
                string message = e.TryGetWebMessageAsString();
                WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs(message, e.Source));
            }
        }

        /// <summary>
        /// 检查给定的 URI 是否属于允许的源
        /// </summary>
        private bool IsAllowedOrigin(string uri)
        {
            if (AllowedOrigins == null || !AllowedOrigins.Any())
            {
                return true; // 如果未设置 AllowedOrigins，则允许所有源
            }

            return AllowedOrigins.Any(origin => uri.StartsWith(origin, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Source 属性变更时的处理方法
        /// </summary>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WebView2Control)d;
            control.NavigateToSource();
        }

        /// <summary>
        /// 导航到 Source 属性指定的 URL
        /// </summary>
        private void NavigateToSource()
        {
            if (InnerWebView.CoreWebView2 != null)
            {
                string url = string.IsNullOrEmpty(Source) ? "about:blank" : Source;
                InnerWebView.CoreWebView2.Navigate(url);
            }
        }

        /// <summary>
        /// 获取内部 WebView2 控件的引用
        /// </summary>
        public Microsoft.Web.WebView2.Wpf.WebView2 WebView => InnerWebView;

        /// <summary>
        /// 在 WebView2 中执行 JavaScript 代码
        /// </summary>
        /// <param name="script">要执行的 JavaScript 代码</param>
        /// <returns>执行任务</returns>
        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (InnerWebView.CoreWebView2 != null)
            {
                script = SanitizeScript(script);
                return await InnerWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            return null;
        }

        /// <summary>
        /// 向 WebView2 发送消息
        /// </summary>
        /// <param name="data">要发送的数据对象</param>
        /// <example>   
        /// <code>
        ///  private async Task SendMessageToWebView(UserControls.WebView2Control webView)
        /// {
        ///     var message = new { type = "greeting", content = "Hello from WPF!" };
        ///     await webView.SendMessageToWebViewAsync(message);
        /// }

        /// public void HandleWebViewMessage(string message)
        /// {
        ///     try
        ///     {
        ///         var jsonMessage = JsonSerializer.Deserialize<JsonElement>(message);
        ///         if (jsonMessage.TryGetProperty("type", out var typeElement) && 
        ///             jsonMessage.TryGetProperty("content", out var contentElement))
        ///         {
        ///             LastReceivedMessage = $"Received: {typeElement.GetString()} - {contentElement.GetString()}";
        ///         }
        //         else
        ///         {
        ///             LastReceivedMessage = $"Received: {message}";
        ///         }
        ///     }
        ///     catch (JsonException)
        ///     {
        ///         LastReceivedMessage = $"Received: {message}";
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <returns>发送任务</returns>
        public async Task SendMessageToWebViewAsync(object data)
        {
            string jsonMessage = JsonSerializer.Serialize(data);
            await ExecuteScriptAsync($"window.postMessage({jsonMessage}, '*');");
        }

        /// <summary>
        /// 对 JavaScript 代码进行基本的安全处理
        /// </summary>
        /// <param name="script">原始 JavaScript 代码</param>
        /// <returns>处理后的 JavaScript 代码</returns>
        private string SanitizeScript(string script)
        {
            // 移除所有的 <script> 标签
            script = Regex.Replace(script, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", string.Empty, RegexOptions.IgnoreCase);
            
            // 移除可能的 JavaScript URL
            script = Regex.Replace(script, @"javascript:", "", RegexOptions.IgnoreCase);
            
            // 移除可能的 HTML 事件属性
            script = Regex.Replace(script, @"on\w+\s*=", "", RegexOptions.IgnoreCase);
            
            // 移除可能的 data: URL
            script = Regex.Replace(script, @"data:", "", RegexOptions.IgnoreCase);
            
            return script;
        }

        private void InnerWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Log.Error($"导航失败: {e.WebErrorStatus}");
                
            }
        }
    }

    /// <summary>
    /// WebMessageReceived 事件的参数类
    /// </summary>
    public class WebMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的消息内容
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 消息的来源 URL
        /// </summary>
        public string Source { get; }

        public WebMessageReceivedEventArgs(string message, string source)
        {
            Message = message;
            Source = source;
        }
    }
}

