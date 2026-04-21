namespace CastorPlugin.Services.DTO
{
    public class AuthSessionDto
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
    }
}
