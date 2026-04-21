using System;
using System.Windows;
using CastorPlugin.ViewModels.Pages;

namespace CastorPlugin.Views.Pages
{
    public partial class LoginWindow : Window, IDisposable
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();

            viewModel.LoginSucceeded += OnLoginSucceeded;
        }

        private void OnLoginSucceeded()
        {
            DialogResult = true;
            Close();
        }

        public void Dispose()
        {
            _viewModel.LoginSucceeded -= OnLoginSucceeded;
        }
    }
}
