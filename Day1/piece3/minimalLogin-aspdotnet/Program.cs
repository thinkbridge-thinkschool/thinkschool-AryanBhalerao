using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration for JWT
// In production, move these to appsettings.json or environment variables
const string jwtKey = "YourSuperSecretKeyThatIsAtLeast32BytesLong!";
const string jwtIssuer = "MinimalApiIssuer";

// Add Authentication and Authorization services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Enable middleware
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---

// 1. Login Endpoint
app.MapPost("/login", (LoginRequest req) =>
{
    // Dummy validation (Replace with database check)
    if (req.Username == "admin" && req.Password == "password123")
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, req.Username) };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return Results.Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
    }

    return Results.Unauthorized();
});

// 2. Protected Endpoint
app.MapGet("/secure", () => 
    Results.Ok(new { Message = "Hello, authenticated user!" }))
    .RequireAuthorization();

app.Run();

// Data Transfer Object
public record LoginRequest(string Username, string Password);