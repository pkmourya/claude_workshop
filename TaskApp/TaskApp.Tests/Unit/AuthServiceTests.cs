using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskApp.Data;
using TaskApp.Models;
using TaskApp.Services;
using TaskApp.Tests.Helpers;

namespace TaskApp.Tests.Unit;

public class AuthServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]      = JwtHelper.TestKey,
                ["Jwt:Issuer"]   = JwtHelper.TestIssuer,
                ["Jwt:Audience"] = JwtHelper.TestAudience
            })
            .Build();

    private static AuthService MakeService(AppDbContext db) =>
        new(db, new PasswordHasher<User>(), CreateConfig());

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_NewUser_ReturnsAuthResponse()
    {
        using var db = CreateDb();
        var svc = MakeService(db);

        var result = await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.UserName);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.UserId > 0);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        using var db = CreateDb();
        var svc = MakeService(db);
        await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        var result = await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice2", Email = "alice@test.com", Password = "Password2!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Register_Token_ContainsSubClaim()
    {
        using var db = CreateDb();
        var svc = MakeService(db);

        var result = await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        var handler = new JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(result!.Token);
        var sub     = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        Assert.Equal(result.UserId.ToString(), sub);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsAuthResponse()
    {
        using var db = CreateDb();
        var svc = MakeService(db);
        await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        var result = await svc.LoginAsync(new LoginRequest
            { Email = "alice@test.com", Password = "Password1!" });

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.UserName);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        using var db = CreateDb();
        var svc = MakeService(db);
        await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        var result = await svc.LoginAsync(new LoginRequest
            { Email = "alice@test.com", Password = "WrongPass!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsNull()
    {
        using var db = CreateDb();
        var svc = MakeService(db);

        var result = await svc.LoginAsync(new LoginRequest
            { Email = "nobody@test.com", Password = "Password1!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_Token_HasCorrectIssuerAndAudience()
    {
        using var db = CreateDb();
        var svc = MakeService(db);
        await svc.RegisterAsync(new RegisterRequest
            { Name = "Alice", Email = "alice@test.com", Password = "Password1!" });

        var result = await svc.LoginAsync(new LoginRequest
            { Email = "alice@test.com", Password = "Password1!" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result!.Token);
        Assert.Equal(JwtHelper.TestIssuer,   jwt.Issuer);
        Assert.Contains(JwtHelper.TestAudience, jwt.Audiences);
    }
}
