using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Data.Models;

public class UserModel
{
    [Key]
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    [Required]
    public string Username { get; set; }
    [Required]
    public string DisplayName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
    public string? ProfilePicturePath { get; set; }
    public string? UserVerifiedAt { get; set; }
    public DateTime? UserVerified { get; set; }
    public string Role { get; set; }
    public string? Location { get; set; }

    public string? Occupation { get; set; }

    public int? ViewedProfile { get; set; }
    public string? InstaToken { get; set; }

    // public int Impressions { get; set; }

    public UserModel() { }

}

public class MailEventType
{
    public string Id { get; set; }

    public string Topic { get; set; }

    public string Subject { get; set; }

    public string EventType { get; set; }

    public DateTime EventTime { get; set; }

    public EventModel Data { get; set; }
}