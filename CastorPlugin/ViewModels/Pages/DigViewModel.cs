using CastorPlugin.Services.Contracts;
using CastorPlugin.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Async;
using System.Text.Json;
using Wpf.Ui;
using Wpf.Ui.Controls;
using System.Threading;
using System.Threading.Tasks;


namespace CastorPlugin.ViewModels.Pages
{
    public partial class DigViewModel : ObservableObject, INavigationAware
    {
        private readonly ISettingsService _settingsService;
        private readonly ISnackbarService _snackbarService;
        private readonly IDigService _digService;
        private readonly ICastorService _castorService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _completedCandidates;

        [ObservableProperty]
        private string _webViewUrl;

        private bool _isWebViewInitialized;
        private readonly SemaphoreSlim _webViewLock = new SemaphoreSlim(1, 1);

        public LoadingIndicator LoadingIndicator { get; set; }
        public string LastReceivedMessage { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

        [ObservableProperty]
        private int _totalCandidates = 100000; // Default value

        public DigViewModel(
            ISettingsService settingsService,
            ISnackbarService snackbarService,
            IDigService digService,
            ICastorService castorService)
        {
            _settingsService = settingsService;
            _snackbarService = snackbarService;
            _digService = digService;
            _castorService = castorService;
            LoadingIndicator = new LoadingIndicator();
            _webViewUrl = _settingsService.GetAssetPlazaUrl(); // Set initial URL from settings
        }

        public void OnNavigatedTo()
        {
           Log.Information($"Navigated to {GetType().FullName}");
           OpenAssetPlaza();
        }

        public void OnNavigatedFrom()
        {
            Log.Information($"Navigated from {GetType().FullName}");
        }

        [RelayCommand]
        private async Task DigAsync()
        {

            IsLoading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _digService.CandidatePosted += OnCandidatePosted;
                await RevitTask.RunAsync(() => _digService.Dig(_cancellationTokenSource.Token));

                // Switch the embedded browser to the public asset plaza after upload.
                OpenAssetPlaza();

                //update total candidates
                await RevitTask.RunAsync(() => _digService.FetchCandidateCountAsync());

                _snackbarService.Show(
                    "Dig operation",
                    "Dig operation completed successfully",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
                    TimeSpan.FromSeconds(5));
            }
            catch (OperationCanceledException)
            {
                _snackbarService.Show(
                    "Dig operation",
                    "Dig operation was cancelled",
                    ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _snackbarService.Show(
                    "Dig operation",
                    $"Dig operation failed: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5));
            }
            finally
            {
                _digService.CandidatePosted -= OnCandidatePosted;
                IsLoading = false;
                _cancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelDig()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void OnCandidatePosted()
        {
            _castorService.Execute<object>(service =>
            {
                LoadingIndicator.ShowSuccessImage();
                CompletedCandidates++;
            });

            _ = ResetLoadingIndicatorAsync();
        }

        private async Task ResetLoadingIndicatorAsync()
        {
            await Task.Delay(1000);
            _castorService.Execute<object>(_ => LoadingIndicator.ShowDefaultImage());
        }

        public async Task UpdateDigCounts()
        {
            try
            {
                TotalCandidates = await _digService.FetchCandidateCountAsync();
            }
            catch (Exception ex)
            {
                _snackbarService.Show(
                    "Error",
                    $"Failed to fetch candidate count: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5));
            }
        }

        public void HandleWebViewMessage(string message, string source)
        {
            if (IsAllowedOrigin(source))
            {
                try
                {
                    var jsonMessage = JsonSerializer.Deserialize<JsonElement>(message);
                    if (jsonMessage.TryGetProperty("type", out var typeElement) && 
                        jsonMessage.TryGetProperty("content", out var contentElement))
                    {
                        LastReceivedMessage = $"收到消息: {typeElement.GetString()} - {contentElement.GetString()}";
                    }
                    else
                    {
                        LastReceivedMessage = $"收到消息: {message}";
                    }
                }
                catch (JsonException)
                {
                    LastReceivedMessage = $"收到消息: {message}";
                }
            }
            else
            {
                // 记录或处理未经授权的消息尝试
                LastReceivedMessage = "收到来自未授权源的消息";
            }
        }

        private bool IsAllowedOrigin(string uri)
        {
            string[] allowedOrigins = { "https://trusted-domain.com" };
            return Array.Exists(allowedOrigins, origin => uri.StartsWith(origin, StringComparison.OrdinalIgnoreCase));
        }

        public void OpenAssetPlaza()
        {
            var newUrl = _settingsService.GetAssetPlazaUrl();
            if (string.IsNullOrWhiteSpace(newUrl))
            {
                Log.Warning("Asset plaza URL is not configured");
                return;
            }

            if (WebViewUrl != newUrl)
            {
                WebViewUrl = newUrl;
                Log.Information($"WebView URL updated to asset plaza: {newUrl}");
            }
            else
            {
                _ = UpdateWebViewUrlAsync(newUrl);
            }
        }

        public void UpdateCandidateListUrl(string documentId)
        {
            if (string.IsNullOrEmpty(documentId))
            {
                Log.Warning("UpdateCandidateListUrl called with null or empty documentId");
                return;
            }

            OpenAssetPlaza();
        }

        partial void OnWebViewUrlChanged(string value)
        {
            if (!_isWebViewInitialized)
            {
                Log.Information("WebView not yet initialized, URL update will be handled after initialization");
                return;
            }

            _ = UpdateWebViewUrlAsync(value);
        }

        private async Task UpdateWebViewUrlAsync(string url)
        {
            await _webViewLock.WaitAsync();
            try
            {
                if (!_isWebViewInitialized)
                {
                    Log.Information("WebView not initialized, skipping URL update");
                    return;
                }

                if (string.IsNullOrEmpty(url))
                {
                    Log.Warning("Attempted to update WebView with empty URL");
                    return;
                }

                Log.Information($"Updating WebView URL to: {url}");
                // 这里可以添加额外的URL验证逻辑
            }
            catch (Exception ex)
            {
                Log.Error($"Error updating WebView URL: {ex.Message}");
                _snackbarService.Show(
                    "WebView Update",
                    $"Failed to update WebView URL: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    TimeSpan.FromSeconds(5));
            }
            finally
            {
                _webViewLock.Release();
            }
        }

        public void SetWebViewInitialized(bool initialized)
        {
            _isWebViewInitialized = initialized;
            if (initialized && !string.IsNullOrEmpty(WebViewUrl))
            {
                _ = UpdateWebViewUrlAsync(WebViewUrl);
            }
        }
    }
}
