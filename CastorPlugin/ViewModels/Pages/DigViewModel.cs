using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nice3point.Revit.Toolkit.External.Handlers;
using Revit.Async;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui;
using Wpf.Ui.Controls;
using CastorPlugin.UserControls;
using System.Text.Json;
using CastorPlugin.Utils;

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
        private bool _isDigging;

        [ObservableProperty]
        private int _completedCandidates;

        [ObservableProperty]
        private string _webViewUrl = "https://www.baidu.com"; // Initial URL

        public LoadingIndicator LoadingIndicator { get; set; }

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
        }

        public void OnNavigatedTo()
        {
            System.Diagnostics.Debug.WriteLine($"Navigated to {GetType().FullName}");
        }

        public void OnNavigatedFrom()
        {
            System.Diagnostics.Debug.WriteLine($"Navigated from {GetType().FullName}");
        }

        [RelayCommand]
        private async Task DigAsync()
        {
            IsLoading = true;
            IsDigging = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _digService.CandidatePosted += OnCandidatePosted;
                await RevitTask.RunAsync(() => _digService.Dig(_cancellationTokenSource.Token));
                
                // Update WebView2 URL after successful dig
                WebViewUrl = "http://macbook-pro:9527/#/candidates"; // Corrected URL

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
                IsDigging = false;
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

        public async Task FetchCandidateCountAsync()
        {
            try
            {
                var response = await WebServiceBroker.SendGetRequestAsync("/nft-works-candidates/counts");
                if (!string.IsNullOrEmpty(response))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var counts = JsonSerializer.Deserialize<CandidateCounts>(response, options);
                    TotalCandidates = counts.TotalCount;
                }
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

        private class CandidateCounts
        {
            public int TotalCount { get; set; }
            public int AcquiredCount { get; set; }
        }
    }
}
