using AuthService.Models;
using Shared.Constants;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Data;

public static class AuthDbSeeder
{
    public static void Seed(AuthDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User
                {
                    Name = "System Administrator",
                    Email = "admin@railway.com",
                    PasswordHash = HashPassword("Admin@123"),
                    Phone = "9999999999",
                    Role = Roles.Admin
                },
                new User
                {
                    Name = "Rahul Sharma",
                    Email = "rahul@example.com",
                    PasswordHash = HashPassword("Rahul@123"),
                    Phone = "9876543210",
                    Role = Roles.User
                },
                new User
                {
                    Name = "Priya Singh",
                    Email = "priya@example.com",
                    PasswordHash = HashPassword("Priya@123"),
                    Phone = "9876543211",
                    Role = Roles.User
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "RailwaySaltSecret2026"));
        return Convert.ToBase64String(hashedBytes);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var newHash = HashPassword(password);
        return string.Equals(newHash, storedHash, StringComparison.Ordinal);
    }
}
