using CastorPlugin.Services.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CastorPlugin.ViewModels.Pages;

public sealed partial class SettingsViewModel(
    ISettingsService settingsService, 
    INavigationService navigationService,
    IWindow window) : ObservableObject
{
    [ObservableProperty] private ApplicationTheme _theme = settingsService.Theme;
    [ObservableProperty] private WindowBackdropType _background = settingsService.Background;

    [ObservableProperty] private bool _useTransition = settingsService.TransitionDuration > 0;
    [ObservableProperty] private bool _useSizeRestoring = settingsService.UseSizeRestoring;
  



    public List<ApplicationTheme> Themes { get; } =
    [
        ApplicationTheme.Light,
        ApplicationTheme.Dark
    // ApplicationTheme.HighContrast
    ];

    public List<WindowBackdropType> BackgroundEffects { get; } =
    [
        WindowBackdropType.None,
        WindowBackdropType.Acrylic,
        WindowBackdropType.Tabbed,
        WindowBackdropType.Mica
    ];


    partial void OnThemeChanged(ApplicationTheme value)
    {
        settingsService.Theme = value;

        foreach (var target in Wpf.Ui.Application.Windows)
        {
            Wpf.Ui.Application.MainWindow = target;
            ApplicationThemeManager.Apply(settingsService.Theme, settingsService.Background);
        }
    }


    partial void OnBackgroundChanged(WindowBackdropType value)
    {
        settingsService.Background = value;
        ApplicationThemeManager.Apply(settingsService.Theme, settingsService.Background);
    }


  partial void OnUseTransitionChanged(bool value)
    {
        var transitionDuration = settingsService.ApplyTransition(value);
        navigationService.GetNavigationControl().TransitionDuration = transitionDuration;
    }

    partial void OnUseSizeRestoringChanged(bool value)
    {
        settingsService.UseSizeRestoring = value;
        if (value) window.EnableSizeTracking();
        else window.DisableSizeTracking();
    }






}
