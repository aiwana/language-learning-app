using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebShadowing.Data;

namespace WebShadowing.AuthFlowTests;

internal sealed class AuthFlowApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionStringEnvironmentVariable = "WEBSHADOWING_TEST_SQLSERVER";
    private readonly string _connectionString;
    private bool _databaseCreated;

    public AuthFlowApplicationFactory()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable(
            TestConnectionStringEnvironmentVariable)
            ?? "Server=localhost;Database=EnglishShadowingDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";
        var connectionBuilder = new SqlConnectionStringBuilder(configuredConnectionString)
        {
            InitialCatalog = $"EnglishShadowingDB_Test_{Guid.NewGuid():N}"
        };
        _connectionString = connectionBuilder.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_connectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        _databaseCreated = true;
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing || !_databaseCreated)
        {
            return;
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        using var db = new AppDbContext(options);
        db.Database.EnsureDeleted();
    }
}
