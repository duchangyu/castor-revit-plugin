using CastorPlugin.Services.DTO;

namespace CastorPlugin.Services.Contracts
{
    public interface IAuthService
    {
        Task<bool> SendVerificationCodeAsync(string phone);
        Task<AuthResultDto> LoginAsync(string phone, string code);
        Task<UserDto> GetCurrentUserAsync();
        void Logout();
        bool IsLoggedIn { get; }
        UserDto CurrentUser { get; }
        event Action OnAuthStateChanged;
    }
}
