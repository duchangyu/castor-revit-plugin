using CastorPlugin.Services.Contracts;
using CastorPlugin.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Revit.Async;
using System.Text.Json;
using Wpf.Ui;
using Wpf.Ui.Controls;

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
        private string _webViewUrl; // 设置一个默认URL

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
            _webViewUrl = _settingsService.GetLandingPageUrl(); // Set initial URL from settings
        }

        public void OnNavigatedTo()
        {
           Log.Information($"Navigated to {GetType().FullName}");
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
                var documentId = await RevitTask.RunAsync(() => _digService.Dig(_cancellationTokenSource.Token));

               
                // Update WebView2 URL after successful dig
                UpdateCandidateListUrl(documentId);

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
                Task.Delay(1000).ContinueWith(_ =>
                {
                    LoadingIndicator.ShowDefaultImage();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            });
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

        public void UpdateCandidateListUrl(string documentId)
        {
            WebViewUrl = $"http://macbook-pro:9527/#/candidates?sourceDocumentId={documentId}";
            
        }
    }
}
