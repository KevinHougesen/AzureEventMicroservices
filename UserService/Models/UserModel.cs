using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
namespace Data.Models;

// Non-confidential user data when signing up (Username, Country, Town, Email, etc.)

public class UserModel
{
    [Key]
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    [Required]
    public string Username { get; set; }
    public string DisplayName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    public string Role { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
}