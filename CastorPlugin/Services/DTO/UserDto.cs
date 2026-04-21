using System;
using System.Text.Json.Serialization;

namespace CastorPlugin.Services.DTO
{
    public class UserDto
    {
        [JsonPropertyName("userId")]
        public string Id { get; set; }
        public string Phone { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool ProfileRequired { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
