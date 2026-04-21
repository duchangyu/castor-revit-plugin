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

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.LoginSucceeded -= OnLoginSucceeded;
            base.OnClosed(e);
        }

        public void Dispose()
        {
            _viewModel.LoginSucceeded -= OnLoginSucceeded;
        }
    }
}
