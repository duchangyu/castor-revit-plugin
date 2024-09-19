using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nice3point.Revit.Toolkit.External.Handlers;
using Revit.Async;
using System;
using System.Threading.Tasks;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CastorPlugin.ViewModels.Pages
{
    public partial class DigViewModel : ObservableObject, INavigationAware
    {
        private readonly ISettingsService _settingsService;
        private readonly ISnackbarService _snackbarService;
        private readonly IDigService _digService;

        [ObservableProperty]
        private bool _isLoading;

        public DigViewModel(
            ISettingsService settingsService,
            ISnackbarService snackbarService,
            IDigService digService)
        {
            _settingsService = settingsService;
            _snackbarService = snackbarService;
            _digService = digService;
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
            try
            {
                await RevitTask.RunAsync(() => _digService.Dig());
                _snackbarService.Show(
                    "Dig operation",
                    "Dig operation completed successfully",
                    ControlAppearance.Success,
                    new SymbolIcon(SymbolRegular.CheckmarkCircle24),
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
                IsLoading = false;
            }
        }
    }
}
