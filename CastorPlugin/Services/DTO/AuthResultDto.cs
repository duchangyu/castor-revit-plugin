namespace CastorPlugin.Services.DTO
{
    public class AuthResultDto
    {
        public string UserId { get; set; }
        public string Phone { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool ProfileRequired { get; set; }
        public string AccessToken { get; set; }

        // Backward compatibility with the previous wrapped response shape.
        public AuthSessionDto Session { get; set; }
        public UserDto User { get; set; }

        public string GetAccessToken()
        {
            return !string.IsNullOrEmpty(AccessToken) ? AccessToken : Session?.AccessToken;
        }

        public int? GetExpiresIn()
        {
            return Session?.ExpiresIn > 0 ? Session.ExpiresIn : null;
        }

        public UserDto GetUser()
        {
            if (User != null)
            {
                return User;
            }

            return new UserDto
            {
                Id = UserId,
                Phone = Phone,
                DisplayName = DisplayName,
                Role = Role,
                ProfileRequired = ProfileRequired
            };
        }
    }
}
