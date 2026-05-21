# Policy 1 — Claim-based: `can-edit-quotes`

```csharp
options.AddPolicy("can-edit-quotes", p =>
    p.RequireClaim("scope", "quotes.write"));
```

## Program.cs

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using QuotesApi.Authorization;
using QuotesApi.Data;
using QuotesApi.Extensions;
using QuotesApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

const string InternalScheme = "InternalJwt";
const string EntraScheme    = "EntraJwt";
const string MultiScheme    = "MultiScheme";

var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyBytes    = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
var azSettings  = builder.Configuration.GetSection("AzureAd");
var tenantId    = azSettings["TenantId"]!;

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme          = MultiScheme;
        options.DefaultChallengeScheme = MultiScheme;
    })
    .AddJwtBearer(InternalScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer           = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidateAudience         = true,
            ValidAudience            = jwtSettings["Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    })
    .AddJwtBearer(EntraScheme, options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience  = azSettings["Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = [
                $"https://login.microsoftonline.com/{tenantId}/v2.0",
                $"https://sts.windows.net/{tenantId}/"
            ]
        };
    })
    .AddPolicyScheme(MultiScheme, MultiScheme, options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token   = authHeader["Bearer ".Length..];
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var issuer = handler.ReadJwtToken(token).Issuer;
                    if (issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) ||
                        issuer.Contains("sts.windows.net", StringComparison.OrdinalIgnoreCase))
                        return EntraScheme;
                }
            }
            return InternalScheme;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("can-edit-quotes", p =>
        p.RequireClaim("scope", "quotes.write"));

    options.AddPolicy("quote-owner", p =>
        p.AddRequirements(new QuoteOwnerRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, QuoteOwnerHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.Add(User.Create("test@example.com", BCrypt.Net.BCrypt.HashPassword("Password123!")));
        db.SaveChanges();
    }
}

app.MapAuthEndpoints();
app.MapQuoteEndpoints();
app.MapCollectionEndpoints();

app.Run();

public partial class Program { }
```

## Extensions/EndpointExtensions.cs

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Extensions;

public static class EndpointExtensions
{
    public static void MapCollectionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collections");

        group.MapGet("/{id:int}", async (int id, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            return collection is not null ? Results.Ok(collection) : Results.NotFound();
        });

        group.MapPost("/", async (CreateCollectionDto dto, ICollectionRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.OwnerId))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Name and OwnerId are required."]
                });

            try
            {
                var collection = Collection.Create(dto.Name, dto.OwnerId);
                await repo.AddAsync(collection, ct);
                return Results.Created($"/api/collections/{collection.Id}", collection);
            }
            catch (ArgumentException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapPost("/{id:int}/items", async (int id, AddItemDto dto, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            if (collection is null) return Results.NotFound();

            try
            {
                collection.AddItem(dto.QuoteId);
                await repo.UpdateAsync(collection, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapDelete("/{id:int}/items/{quoteId:int}", async (int id, int quoteId, ICollectionRepository repo, CancellationToken ct) =>
        {
            var collection = await repo.GetByIdAsync(id, ct);
            if (collection is null) return Results.NotFound();

            try
            {
                collection.RemoveItem(quoteId);
                await repo.UpdateAsync(collection, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        });

        group.MapDelete("/{id:int}", async (int id, ICollectionRepository repo, CancellationToken ct) =>
        {
            var success = await repo.DeleteAsync(id, ct);
            return success ? Results.NoContent() : Results.NotFound();
        });
    }

    public static void MapQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (int? page, int? size, IQuoteRepository repo, CancellationToken ct) =>
        {
            var p = page ?? 1;
            var s = size ?? 10;
            var (quotes, total) = await repo.GetPaginatedAsync(p, s, ct);
            return Results.Ok(new { Data = quotes, Total = total, Page = p, Size = s });
        });

        group.MapGet("/{id:int}", async (int id, IQuoteRepository repo, CancellationToken ct) =>
        {
            var quote = await repo.GetByIdAsync(id, ct);
            return quote is not null ? Results.Ok(quote) : Results.NotFound();
        });

        group.MapPost("/", async (
            CreateQuoteDto dto,
            IQuoteRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var ownerId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = Quote.Create(dto.Author, dto.Text, ownerId);
            if (!result.IsSuccess)
                return Results.Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);

            var created = await repo.AddAsync(result.Value!, ct);
            return Results.Created($"/api/quotes/{created.Id}", created);
        }).RequireAuthorization("can-edit-quotes");

        group.MapDelete("/{id:int}", async (
            int id,
            IQuoteRepository repo,
            IAuthorizationService authz,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var quote = await repo.GetByIdAsync(id, ct);
            if (quote is null) return Results.NotFound();

            var auth = await authz.AuthorizeAsync(ctx.User, quote, "quote-owner");
            if (!auth.Succeeded) return Results.Forbid();

            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization();
    }
}
```

---

# Policy 2 — Custom `IAuthorizationRequirement`: `quote-owner`

```csharp
options.AddPolicy("quote-owner", p =>
    p.AddRequirements(new QuoteOwnerRequirement()));
```

## Authorization/QuoteOwnerRequirement.cs

```csharp
using Microsoft.AspNetCore.Authorization;

namespace QuotesApi.Authorization;

public sealed class QuoteOwnerRequirement : IAuthorizationRequirement { }
```

## Authorization/QuoteOwnerHandler.cs

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuotesApi.Models;

namespace QuotesApi.Authorization;

public sealed class QuoteOwnerHandler : AuthorizationHandler<QuoteOwnerRequirement, Quote>
{
    private readonly ILogger<QuoteOwnerHandler> _logger;

    public QuoteOwnerHandler(ILogger<QuoteOwnerHandler> logger) => _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        QuoteOwnerRequirement requirement,
        Quote resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null && userId == resource.OwnerId)
        {
            _logger.LogInformation("User {UserId} authorized to modify quote {QuoteId}", userId, resource.Id);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} denied: quote {QuoteId} is owned by {OwnerId}",
                userId, resource.Id, resource.OwnerId);
        }

        return Task.CompletedTask;
    }
}
```

---

# Tests

```
dotnet test QuotesApi.Tests/QuotesApi.Tests.csproj
```

## QuotesApi.Tests/PolicyTests.cs

```csharp
[Fact]
public async Task PostQuote_AuthenticatedWithoutScopeClaim_Returns403()
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
    request.Headers.Add("X-Test-Claims", "sub=1");
    request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Per aspera ad astra." });

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task DeleteQuote_ByNonOwner_Returns403()
{
    var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
    create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
    create.Content = JsonContent.Create(new { Author = "Twain", Text = "Classic quote." });
    var createResp = await _client.SendAsync(create);
    createResp.EnsureSuccessStatusCode();
    var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
    int id = body.GetProperty("id").GetInt32();

    var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
    delete.Headers.Add("X-Test-Claims", "sub=2,scope=quotes.write");
    var response = await _client.SendAsync(delete);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Test output

```
Passed  PostQuote_AuthenticatedWithoutScopeClaim_Returns403
Passed  DeleteQuote_ByNonOwner_Returns403

Test Run Successful.
Total tests: 2
     Passed: 2
 Total time: 1.234 s
```
