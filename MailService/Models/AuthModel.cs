using Newtonsoft.Json;
namespace Data.Models;
public class AuthModel
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string EmailVerificationToken { get; set; }
    public DateTime EmailVerifiedAt { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public string Role { get; set; }
}