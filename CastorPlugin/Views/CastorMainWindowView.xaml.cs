using CastorPlugin.Services.Contracts;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace CastorPlugin.Views
{
    /// <summary>
    /// WindowMain.xaml 的交互逻辑
    /// </summary>
    public partial class CastorMainWindowView : IWindow
    {

        ISettingsService _settingsService;
        public CastorMainWindowView()
        {

            Wpf.Ui.Application.MainWindow = this;
            Wpf.Ui.Application.Windows.Add(this);
            InitializeComponent();
        }

        public CastorMainWindowView(
            INavigationService navigaionService,
              IContentDialogService dialogService,
               ISnackbarService snackbarService,
            ISettingsService settingsService
            ) : this()
        {
            _settingsService = settingsService;
            WindowBackdropType = settingsService.Background;

            navigaionService.SetNavigationControl(RootNavigation);
            dialogService.SetContentPresenter(RootContentDialog);

            snackbarService.SetSnackbarPresenter(RootSnackbar);
            snackbarService.DefaultTimeOut = TimeSpan.FromSeconds(3);

            RestoreSize(settingsService);
            ApplicationThemeManager.Apply(_settingsService.Theme, _settingsService.Background);
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
