using AttendanceApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AttendanceApp.IntegrationTests.Fixtures;


public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    private string? _dbName;

    public AttendanceAppDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _dbName = $"attendanceapp_test_{Guid.NewGuid()}";
        
        using var scope = Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<AttendanceAppDbContext>();
        
        await DbContext.Database.EnsureCreatedAsync();
    }


    public async Task CleanupAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AttendanceAppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.DisposeAsync();
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AttendanceAppDbContext>));
            
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AttendanceAppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName ?? "Attendanceapp_test");
            });

            var loggerDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(ILogger<>));

            if (loggerDescriptor != null)
            {
                services.Remove(loggerDescriptor);
            }

            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.SetMinimumLevel(LogLevel.Critical);
            });

            /*services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });*/

            services.AddAuthorization();
        });

        builder.UseEnvironment("Testing");
    }
}
