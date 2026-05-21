namespace QuotesApi.Models;

public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private User() { }

    public static User Create(string email, string passwordHash) =>
        new() { Email = email, PasswordHash = passwordHash };
}

public record LoginDto(string Email, string Password);
public record RefreshDto(string RefreshToken);
public record LogoutDto(string RefreshToken);
