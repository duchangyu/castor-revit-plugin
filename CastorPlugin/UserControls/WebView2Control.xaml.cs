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
using System.Threading;
using CastorPlugin.Core;
using CastorPlugin.Core.Exceptions;
using System.Windows.Media.Animation;
using Serilog;

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
                new PropertyMetadata(BlankUrl, OnSourceChanged, CoerceSourceValue));

        /// <summary>
        /// AllowedOrigins 依赖属性，用于设置允许的源域名列表
        /// </summary>
        public static readonly DependencyProperty AllowedOriginsProperty =
            DependencyProperty.Register("AllowedOrigins", typeof(IEnumerable<string>), typeof(WebView2Control), 
                new PropertyMetadata(null));

        /// <summary>
        /// IsLoading 依赖属性，表示WebView是否正在加载内容
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(WebView2Control), 
                new PropertyMetadata(false, OnIsLoadingChanged));

        /// <summary>
        /// 获取或设置 WebView2 的 URL
        /// </summary>
        public string Source
        {
            get { return NormalizeSource((string)GetValue(SourceProperty)); }
            set { SetValue(SourceProperty, NormalizeSource(value)); }
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
        /// 获取或设置WebView是否正在加载内容
        /// </summary>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            private set { SetValue(IsLoadingProperty, value); }
        }

        /// <summary>
        /// 当从 Web 页面接收到消息时触发的事件
        /// </summary>
        public event EventHandler<WebMessageReceivedEventArgs> WebMessageReceived;

        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isNavigatingFromWebView;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);

        // 定义页面加载完成后的渲染延迟时间（毫秒）
        private const int RenderDelayMs = 500;
        private const string BlankUrl = "about:blank";
        private CancellationTokenSource _fadeTokenSource;

        public WebView2Control()
        {
            InitializeComponent();
            Loaded += WebView2Control_Loaded;
            Unloaded += WebView2Control_Unloaded;
        }

        /// <summary>
        /// 控件加载完成时初始化 WebView2
        /// </summary>
        private async void WebView2Control_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized && !_isDisposed && InnerWebView != null)
                {
                    IsLoading = true;
                    await InitializeWebView2();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during WebView2Control_Loaded: {ex.Message}");
                IsLoading = false;
            }
        }

        private void WebView2Control_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CleanupWebView2();
            }
            catch (Exception ex)
            {
                Log.Error($"Error during WebView2Control_Unloaded: {ex.Message}");
            }
        }

        /// <summary>
        /// IsLoading属性变更处理方法
        /// </summary>
        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WebView2Control)d;
            bool isLoading = (bool)e.NewValue;
            
            // 在UI线程上更新加载指示器的可见性
            control.Dispatcher.BeginInvoke(new Action(() => {
                if (isLoading)
                {
                    // 取消任何正在进行的淡出操作
                    if (control._fadeTokenSource != null)
                    {
                        control._fadeTokenSource.Cancel();
                        control._fadeTokenSource.Dispose();
                        control._fadeTokenSource = null;
                    }
                    
                    control.LoadingOverlay.Opacity = 1.0;
                    control.LoadingOverlay.Visibility = Visibility.Visible;
                }
                // 不在这里处理隐藏，而是在导航完成后延迟执行
            }));
        }

        /// <summary>
        /// 初始化 WebView2 并设置安全选项
        /// </summary>
        private async Task InitializeWebView2()
        {
            if (_isInitialized || _isDisposed || InnerWebView == null) return;

            await _initializationLock.WaitAsync();
            try
            {
                if (_isInitialized || _isDisposed || InnerWebView == null) return;

                if (InnerWebView.CoreWebView2 == null)
                {
                    try
                    {
                        // 检查 WebView2 运行时是否已安装
                        var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                        if (string.IsNullOrEmpty(version))
                        {
                            throw new Core.Exceptions.WebView2RuntimeNotFoundException();
                        }

                        // 设置 WebView2 用户数据文件夹
                        string tempPath = Path.Combine(Path.GetTempPath(), "WebView2UserData", Guid.NewGuid().ToString());
                        Directory.CreateDirectory(tempPath);
                        
                        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: tempPath);
                        await InnerWebView.EnsureCoreWebView2Async(env);

                        // 验证 CoreWebView2 仍然有效
                        if (InnerWebView.CoreWebView2 == null)
                        {
                            Log.Error("CoreWebView2 is null after EnsureCoreWebView2Async");
                            IsLoading = false;
                            return;
                        }

                        // 设置安全选项
                        InnerWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                        InnerWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                        InnerWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                        InnerWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                        InnerWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

                        // 设置事件处理程序
                        InnerWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                        InnerWebView.NavigationStarting += InnerWebView_NavigationStarting;
                        InnerWebView.WebMessageReceived += InnerWebView_WebMessageReceived;
                        InnerWebView.NavigationCompleted += InnerWebView_NavigationCompleted;

                        InnerWebView.CoreWebView2.Navigate(Source);

                        _isInitialized = true;
                        Log.Information("WebView2 initialized successfully");
                    }
                    catch (Core.Exceptions.WebView2RuntimeNotFoundException)
                    {
                        Log.Error("WebView2 运行时未安装。请安装 WebView2 运行时后再试。");
                        IsLoading = false;
                        //TODO： 可以在这里提供下载链接或进一步的指导
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"初始化 WebView2 时发生错误: {ex.Message}");
                        IsLoading = false;
                    }
                }
                else
                {
                    IsLoading = false;
                }
            }
            finally
            {
                _initializationLock.Release();
            }
        }

        /// <summary>
        /// 处理新窗口请求
        /// </summary>
        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            try
            {
                if (_isDisposed || InnerWebView?.CoreWebView2 == null) return;
                
                e.Handled = true; // 阻止新窗口打开
                if (IsAllowedOrigin(e.Uri))
                {
                    IsLoading = true;
                    InnerWebView.CoreWebView2.Navigate(e.Uri); // 在当前 WebView 中导航
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling new window request: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理导航开始事件
        /// </summary>
        private void InnerWebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            try
            {
                if (_isDisposed) return;
                
                IsLoading = true;
                if (!IsAllowedOrigin(e.Uri))
                {
                    e.Cancel = true; // 如果不是允许的源，取消导航
                    IsLoading = false;
                    return;
                }
                if (!string.IsNullOrWhiteSpace(e.Uri))
                {
                    _isNavigatingFromWebView = true;
                    try
                    {
                        Source = e.Uri;
                    }
                    finally
                    {
                        _isNavigatingFromWebView = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling navigation starting: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 处理接收到的 Web 消息
        /// </summary>
        private void InnerWebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                if (_isDisposed) return;
                
                if (IsAllowedOrigin(e.Source))
                {
                    string message = e.TryGetWebMessageAsString();
                    WebMessageReceived?.Invoke(this, new WebMessageReceivedEventArgs(message, e.Source));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error handling web message: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查给定的 URI 是否属于允许的源
        /// </summary>
        private bool IsAllowedOrigin(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }

            var allowedOrigins = AllowedOrigins?
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .ToArray();

            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                return true; // 如果未设置 AllowedOrigins，则允许所有源
            }

            return allowedOrigins.Any(origin => uri.StartsWith(origin, StringComparison.OrdinalIgnoreCase));
        }

        private static object CoerceSourceValue(DependencyObject d, object baseValue)
        {
            return NormalizeSource(baseValue as string);
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? BlankUrl : source.Trim();
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
            try
            {
                if (InnerWebView?.CoreWebView2 != null && !_isDisposed)
                {
                    if (_isNavigatingFromWebView)
                    {
                        return;
                    }

                    InnerWebView.CoreWebView2.Navigate(Source);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error navigating to source: {ex.Message}");
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
            if (_isDisposed || InnerWebView?.CoreWebView2 == null)
            {
                Log.Warning("ExecuteScriptAsync called but WebView2 is not initialized or disposed");
                return null;
            }

            try
            {
                script = SanitizeScript(script);
                return await InnerWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Log.Error($"Error executing script: {ex.Message}");
                return null;
            }
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
            if (_isDisposed || data == null)
            {
                Log.Warning("SendMessageToWebViewAsync called with null data or disposed control");
                return;
            }

            try
            {
                string jsonMessage = JsonSerializer.Serialize(data);
                await ExecuteScriptAsync($"window.postMessage({jsonMessage}, '*');");
            }
            catch (Exception ex)
            {
                Log.Error($"Error sending message to WebView: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理JavaScript脚本，移除潜在的危险内容
        /// </summary>
        /// <param name="script">要清理的脚本</param>
        /// <returns>清理后的脚本</returns>
        private string SanitizeScript(string script)
        {
            if (string.IsNullOrEmpty(script))
            {
                return string.Empty;
            }

            try
            {
                // 移除可能导致XSS的危险模式
                script = Regex.Replace(script, @"<\s*script.*?>.*?<\s*/\s*script\s*>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                script = Regex.Replace(script, @"javascript\s*:", "", RegexOptions.IgnoreCase);
                script = Regex.Replace(script, @"vbscript\s*:", "", RegexOptions.IgnoreCase);
                script = Regex.Replace(script, @"data\s*:", "", RegexOptions.IgnoreCase);
                
                // 移除 HTML 事件属性
                script = Regex.Replace(script, @"\bon\w+\s*=", "data-disabled-event=", RegexOptions.IgnoreCase);
                
                // 移除可能的 DOM XSS 向量
                script = Regex.Replace(script, @"document\.write\s*\(", "/* blocked */", RegexOptions.IgnoreCase);
                script = Regex.Replace(script, @"document\.writeln\s*\(", "/* blocked */", RegexOptions.IgnoreCase);
                
                return script;
            }
            catch (Exception ex)
            {
                Log.Error($"Script sanitization error: {ex.Message}");
                return string.Empty; // 如果处理失败，则返回空字符串
            }
        }

        /// <summary>
        /// 初始化加载指示器的淡出动画
        /// </summary>
        private void InitializeFadeAnimation()
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };
            
            fadeOutAnimation.Completed += (s, e) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoadingOverlay.Opacity = 1.0; // 重置不透明度以便下次使用
            };
            
            LoadingOverlay.Opacity = 1.0;
            LoadingOverlay.BeginAnimation(OpacityProperty, fadeOutAnimation);
        }

        /// <summary>
        /// 添加导航完成的事件处理方法
        /// </summary>
        private async void InnerWebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                // 导航完成后，延迟一段时间再隐藏加载指示器，以确保页面已渲染
                if (_fadeTokenSource != null)
                {
                    _fadeTokenSource.Cancel();
                    _fadeTokenSource.Dispose();
                }
                
                _fadeTokenSource = new CancellationTokenSource();
                var token = _fadeTokenSource.Token;
                
                try
                {
                    // 等待页面渲染
                    await Task.Delay(RenderDelayMs, token);
                    
                    // 如果没有被取消，执行淡出动画
                    if (!token.IsCancellationRequested && !_isDisposed && InnerWebView != null)
                    {
                        Dispatcher.BeginInvoke(new Action(() => {
                            if (!_isDisposed)
                            {
                                IsLoading = false;
                                InitializeFadeAnimation();
                            }
                        }));
                    }
                }
                catch (TaskCanceledException)
                {
                    // 任务被取消，不执行任何操作
                }
                
                if (e.IsSuccess)
                {
                    Log.Information($"Navigation completed successfully: {InnerWebView.Source}");
                    
                    // 注入CSS以防止空白屏闪烁
                    await InjectAntiFlickerStylesAsync();
                }
                else
                {
                    Log.Warning($"Navigation failed with error: {e.WebErrorStatus}`");
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Log.Error($"Error in NavigationCompleted handler: {ex.Message}");
            }
        }

        /// <summary>
        /// 注入防止页面闪烁的CSS样式
        /// </summary>
        private async Task InjectAntiFlickerStylesAsync()
        {
            if (_isDisposed || InnerWebView?.CoreWebView2 == null) return;

            const string script = @"
                (function() {
                    if (!document.getElementById('anti-flicker-style')) {
                        var style = document.createElement('style');
                        style.id = 'anti-flicker-style';
                        style.innerHTML = 'body { opacity: 1; transition: opacity 0.1s ease-in; }';
                        document.head.appendChild(style);
                    }
                })();";
            
            try
            {
                await InnerWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                Log.Error($"Error injecting anti-flicker styles: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全的清理WebView2资源
        /// </summary>
        internal void CleanupWebView2()
        {
            if (_isDisposed) return;

            try
            {
                Log.Information("Starting WebView2 cleanup");
                
                // 取消任何正在进行的淡出操作
                if (_fadeTokenSource != null)
                {
                    _fadeTokenSource.Cancel();
                    _fadeTokenSource.Dispose();
                    _fadeTokenSource = null;
                }
                
                try
                {
                    if (InnerWebView?.CoreWebView2 != null)
                    {
                        // 移除事件处理器
                        try
                        {
                            InnerWebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error removing NewWindowRequested handler: {ex.Message}");
                        }
                        
                        try
                        {
                            InnerWebView.NavigationStarting -= InnerWebView_NavigationStarting;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error removing NavigationStarting handler: {ex.Message}");
                        }
                        
                        try
                        {
                            InnerWebView.WebMessageReceived -= InnerWebView_WebMessageReceived;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error removing WebMessageReceived handler: {ex.Message}");
                        }
                        
                        try
                        {
                            InnerWebView.NavigationCompleted -= InnerWebView_NavigationCompleted;
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error removing NavigationCompleted handler: {ex.Message}");
                        }

                        // 导航到空白页
                        try
                        {
                            InnerWebView.CoreWebView2.Navigate("about:blank");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"导航到空白页时发生错误: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error accessing CoreWebView2: {ex.Message}");
                }

                // 重置Source属性
                try
                {
                    Source = "about:blank";
                }
                catch (Exception ex)
                {
                    Log.Warning($"设置Source属性时发生错误: {ex.Message}");
                }

                IsLoading = false;
                _isDisposed = true;
                _isInitialized = false;
                
                Log.Information("WebView2 cleanup completed");
            }
            catch (Exception ex)
            {
                Log.Error($"清理 WebView2 资源时发生错误: {ex.Message}");
                _isDisposed = true;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// 初始化WebView2控件的异步方法，可以从外部调用
        /// </summary>
        /// <returns>初始化任务</returns>
        public async Task InitializeAsync()
        {
            if (_isDisposed || InnerWebView == null) return;
            
            try
            {
                if (!_isInitialized)
                {
                    IsLoading = true;
                    await InitializeWebView2();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error during InitializeAsync: {ex.Message}");
                IsLoading = false;
            }
        }

        /// <summary>
        /// 用于处理WebView2错误并尝试恢复的方法
        /// </summary>
        /// <returns>恢复是否成功</returns>
        public async Task<bool> RecoverFromErrorAsync()
        {
            if (_isDisposed || InnerWebView == null)
            {
                Log.Warning("Cannot recover WebView2 - control is disposed or null");
                return false;
            }

            try
            {
                IsLoading = true;
                
                // 清理现有资源前先导航到空白页
                try
                {
                    if (InnerWebView.CoreWebView2 != null)
                    {
                        await InnerWebView.CoreWebView2.ExecuteScriptAsync("window.stop();");
                        InnerWebView.CoreWebView2.Navigate("about:blank");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"停止导航时发生错误: {ex.Message}");
                }
                
                // 清理现有资源
                CleanupWebView2();
                
                // 重置状态
                _isInitialized = false;
                _isDisposed = false;
                
                // 重新初始化WebView2
                await InitializeWebView2();
                
                // 检查初始化是否成功
                bool success = _isInitialized && InnerWebView?.CoreWebView2 != null;
                
                if (!success)
                {
                    IsLoading = false;
                }
                
                return success;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                Log.Error($"Error recovering WebView2: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Log.Information("Disposing WebView2Control");
                
                // 先导航到空白页
                try
                {
                    if (InnerWebView?.CoreWebView2 != null)
                    {
                        InnerWebView.CoreWebView2.Navigate("about:blank");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Dispose时导航到空白页失败: {ex.Message}");
                }

                CleanupWebView2();

                // 清理资源
                _initializationLock?.Dispose();
                _fadeTokenSource?.Dispose();
                
                // 移除事件处理器
                Loaded -= WebView2Control_Loaded;
                Unloaded -= WebView2Control_Unloaded;
            }
            catch (Exception ex)
            {
                Log.Error($"Dispose时发生错误: {ex.Message}");
            }
            finally
            {
                _isDisposed = true;
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

