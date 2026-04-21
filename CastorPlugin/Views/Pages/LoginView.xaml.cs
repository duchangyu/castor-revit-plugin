using System.Windows.Controls;
using CastorPlugin.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace CastorPlugin.Views.Pages
{
    public partial class LoginView : Page
    {
        public LoginViewModel ViewModel { get; }

        public LoginView(LoginViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
