using CastorPlugin.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace CastorPlugin.Views.Pages
{
    /// <summary>
    /// SettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsView : INavigableView<SettingsViewModel>
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = this;
        }
        public SettingsView(SettingsViewModel viewModel)
        {

            ViewModel = viewModel;
            InitializeComponent();
            DataContext = this;
        }

        public SettingsViewModel ViewModel { get; }
    }
}
