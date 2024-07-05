using System.Text.RegularExpressions;
using Data.Models;

public static class AuthUtils
{
    public static bool ValidateUserInput(AuthModel user)
    {
        // Simple validation example (can be extended)
        return !string.IsNullOrEmpty(user.Username) &&
               !string.IsNullOrEmpty(user.Email) &&
               Regex.IsMatch(user.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") &&
               !string.IsNullOrEmpty(user.PasswordHash);
    }

    public static string HashPassword(string password)
    {
        // Using BCrypt to hash the password with a salt
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        // Using BCrypt to verify the password against the stored hash
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public static bool HasRole(AuthModel user, string requiredRole)
    {
        return user.Role == requiredRole;
    }
}
