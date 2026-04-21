using CastorPlugin.Services.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace CastorPlugin.ViewModels.Pages
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _phone;

        [ObservableProperty]
        private string _code;

        [ObservableProperty]
        private bool _isCodeSent;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public event Action LoginSucceeded;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        partial void OnErrorMessageChanged(string value) => HasError = !string.IsNullOrEmpty(value);

        [RelayCommand]
        private async Task SendCodeAsync()
        {
            if (string.IsNullOrEmpty(Phone))
            {
                ErrorMessage = "请输入手机号";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var success = await _authService.SendVerificationCodeAsync(Phone);
                if (success)
                {
                    IsCodeSent = true;
                }
                else
                {
                    ErrorMessage = "发送验证码失败，请稍后重试";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(Code))
            {
                ErrorMessage = "请输入验证码";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _authService.LoginAsync(Phone, Code);
                if (result?.Session != null)
                {
                    LoginSucceeded?.Invoke();
                }
                else
                {
                    ErrorMessage = "验证码错误或已过期";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ResendCode()
        {
            IsCodeSent = false;
            Code = string.Empty;
        }
    }
}
