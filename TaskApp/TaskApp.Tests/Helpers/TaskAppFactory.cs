using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskApp.Data;

namespace TaskApp.Tests.Helpers;

public sealed class TaskAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // ── Replace SQL Server DbContext with in-memory ───────────────────
            var optDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optDescriptor is not null) services.Remove(optDescriptor);

            // Remove IDbContextOptionsConfiguration<AppDbContext> so the SqlServer
            // provider isn't registered alongside the in-memory one.
            var efConfigs = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext) &&
                    d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.StartsWith("Microsoft.EntityFrameworkCore"))
                .ToList();
            foreach (var d in efConfigs) services.Remove(d);

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // ── Override JWT signing key so tests don't need the prod secret ──
            // PostConfigure always runs last, guaranteeing our key wins.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opts =>
                {
                    var key = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtHelper.TestKey));

                    opts.MapInboundClaims = false;
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer           = true,
                        ValidateAudience         = true,
                        ValidateLifetime         = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer              = JwtHelper.TestIssuer,
                        ValidAudience            = JwtHelper.TestAudience,
                        IssuerSigningKey         = key
                    };
                });
        });
    }

    public HttpClient CreateJsonClient(bool allowRedirects = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowRedirects
        });
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }
}
