namespace Data.Models;
public class MailModel
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

}