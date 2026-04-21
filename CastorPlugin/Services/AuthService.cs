using CastorPlugin.Services.Contracts;
using CastorPlugin.Services.DTO;
using CastorPlugin.Utils;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CastorPlugin.Services
{
    public class AuthService : IAuthService
    {
        private readonly ISettingsService _settingsService;
        private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);

        public AuthService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            // Restore token on construction if available
            if (_settingsService.IsLoggedIn)
            {
                WebServiceBroker.SetAccessToken(_settingsService.AccessToken);
            }
        }

        public event Action OnAuthStateChanged;

        public bool IsLoggedIn => _settingsService.IsLoggedIn;

        public UserDto CurrentUser => _settingsService.CurrentUser;

        public async Task<bool> SendVerificationCodeAsync(string phone)
        {
            try
            {
                var apiUrl = WebServiceBroker.GetFullUrl("/auth/sms/send-code");
                Log.Information($"Sending verification code to phone: {MaskPhone(phone)}, URL: {apiUrl}");
                var response = await WebServiceBroker.SendPostRequestAsync(
                    "/auth/sms/send-code",
                    new { phone });
                Log.Information($"Send code response received: {!string.IsNullOrEmpty(response)}");
                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send verification code: {ex.Message}");
                return false;
            }
        }

        public async Task<AuthResultDto> LoginAsync(string phone, string code)
        {
            await _loginLock.WaitAsync();
            try
            {
                var response = await WebServiceBroker.SendPostRequestAsync(
                    "/auth/sms/login",
                    new { phone, code });

                if (string.IsNullOrEmpty(response)) return null;

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var result = JsonSerializer.Deserialize<AuthResultDto>(response, options);
                var accessToken = result?.GetAccessToken();

                if (!string.IsNullOrEmpty(accessToken))
                {
                    _settingsService.AccessToken = accessToken;
                    _settingsService.CurrentUser = result.GetUser();
                    var expiresIn = result.GetExpiresIn();
                    _settingsService.TokenExpiry = expiresIn.HasValue
                        ? DateTime.Now.AddSeconds(expiresIn.Value)
                        : null;
                    _settingsService.Save();

                    WebServiceBroker.SetAccessToken(accessToken);
                    OnAuthStateChanged?.Invoke();
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"Login failed: {ex.Message}");
                return null;
            }
            finally
            {
                _loginLock.Release();
            }
        }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            try
            {
                var response = await WebServiceBroker.SendGetRequestAsync("/auth/me");
                if (string.IsNullOrEmpty(response)) return null;

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<UserDto>(response, options);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get current user: {ex.Message}");
                return null;
            }
        }

        public void Logout()
        {
            _settingsService.ClearAuth();
            _settingsService.Save();
            WebServiceBroker.ClearAccessToken();
            OnAuthStateChanged?.Invoke();
        }

        private static string MaskPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 7)
            {
                return "<redacted>";
            }

            return $"{phone.Substring(0, 3)}****{phone.Substring(phone.Length - 4)}";
        }
    }
}
