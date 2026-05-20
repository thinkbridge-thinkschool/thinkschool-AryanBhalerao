namespace QuotesApi.Models;

public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;

    private User() { }

    public static User Create(string email, string passwordHash) =>
        new() { Email = email, PasswordHash = passwordHash };
}

public record LoginDto(string Email, string Password);
