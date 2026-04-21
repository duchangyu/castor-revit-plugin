using System.Windows;
using CastorPlugin.ViewModels.Pages;

namespace CastorPlugin.Views.Pages
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();

            viewModel.LoginSucceeded += () =>
            {
                DialogResult = true;
                Close();
            };
        }
    }
}
