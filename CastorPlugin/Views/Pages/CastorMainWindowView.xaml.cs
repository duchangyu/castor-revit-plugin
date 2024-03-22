using CastorPlugin.Services.Contracts;
using CastorPlugin.Services.Enums;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace CastorPlugin.Views
{

    public partial class CastorMainWindowView : IWindow
    {

        ISettingsService _settingsService;
        public CastorMainWindowView()
        {

            Wpf.Ui.Application.MainWindow = this;
            Wpf.Ui.Application.Windows.Add(this);
            InitializeComponent();
            AddShortcuts();
        }

        public CastorMainWindowView(
            INavigationService navigaionService,
              IContentDialogService dialogService,
               ISnackbarService snackbarService,
            ISettingsService settingsService,
           ISoftwareUpdateService updateService
            ) : this()
        {
            _settingsService = settingsService;
            RootNavigation.TransitionDuration = settingsService.TransitionDuration;
            WindowBackdropType = settingsService.Background;

            navigaionService.SetNavigationControl(RootNavigation);
            dialogService.SetContentPresenter(RootContentDialog);

            snackbarService.SetSnackbarPresenter(RootSnackbar);
            snackbarService.DefaultTimeOut = TimeSpan.FromSeconds(3);

            RestoreSize(settingsService);
            SetupBadges(updateService);
            ApplicationThemeManager.Apply(_settingsService.Theme, _settingsService.Background);
        }

        private void SetupBadges(ISoftwareUpdateService updateService)
        {
            if (updateService.State != SoftwareUpdateState.ReadyToDownload) return;
            AboutItemBadge.Visibility = Visibility.Visible;
        }

        private void AddShortcuts()
        {
            var closeCurrentCommand = new RelayCommand(Close);
            var closeAllCommand = new RelayCommand(() =>
            {
                for (var i = Wpf.Ui.Application.Windows.Count - 1; i >= 0; i--)
                {
                    var window = Wpf.Ui.Application.Windows[i];
                    window.Close();
                }
            });

            InputBindings.Add(new KeyBinding(closeAllCommand, new KeyGesture(Key.Escape, ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(closeCurrentCommand, new KeyGesture(Key.Escape)));
        }

        private void RestoreSize(ISettingsService settingsService)
        {
            if (!settingsService.UseSizeRestoring) return;

            if (settingsService.WindowWidth >= MinWidth) Width = settingsService.WindowWidth;
            if (settingsService.WindowHeight >= MinHeight) Height = settingsService.WindowHeight;

            EnableSizeTracking();
        }

        public void DisableSizeTracking()
        {
            SizeChanged -= OnSizeChanged;
        }

        public void EnableSizeTracking()
        {
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _settingsService.WindowWidth = e.NewSize.Width;
            _settingsService.WindowHeight = e.NewSize.Height;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Wpf.Ui.Application.MainWindow = this;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Wpf.Ui.Application.Windows.Remove(this);
        }

  

    }
}
