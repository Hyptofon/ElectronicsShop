using Domain.Users;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests.Common;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("test-electronics-shop")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureTestServices(services =>
        {
            RemoveDatabaseInitialiser(services);
            RegisterDatabase(services);
        });
    }

    public WebApplicationFactory<Program> WithWebHostBuilderMock(
        string role = "User", 
        string userId = null)
    {
        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Видаляємо попередні реєстрації
                var authDataDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TestAuthData));
                if (authDataDescriptor != null)
                {
                    services.Remove(authDataDescriptor);
                }

                // ВИПРАВЛЕННЯ: Реєструємо TestAuthData як Singleton
                services.AddSingleton(new TestAuthData 
                { 
                    Role = role,
                    UserId = userId ?? Guid.NewGuid().ToString()
                });

                // Видаляємо стандартну автентифікацію
                var authDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("Authentication") == true)
                    .ToList();
                
                foreach (var descriptor in authDescriptors)
                {
                    services.Remove(descriptor);
                }

                // ВИПРАВЛЕННЯ: Додаємо тестову схему автентифікації
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                    options.DefaultScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", options => { });
            });
        });
    }

    private void RemoveDatabaseInitialiser(IServiceCollection services)
    {
        var descriptors = services.Where(
            d => d.ServiceType == typeof(ApplicationDbContextInitialiser)).ToList();
            
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }

    private void RegisterDatabase(IServiceCollection services)
    {
        var dbContextOptionsDescriptors = services.Where(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)).ToList();
        foreach (var descriptor in dbContextOptionsDescriptors)
        {
            services.Remove(descriptor);
        }

        var appDbContextDescriptors = services.Where(
            d => d.ServiceType == typeof(ApplicationDbContext)).ToList();
        foreach (var descriptor in appDbContextDescriptors)
        {
            services.Remove(descriptor);
        }
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString());
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();
        
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(
                dataSource,
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider;
        
        var context = provider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        
        string[] roles = { ApplicationRole.User, ApplicationRole.Admin, ApplicationRole.Manager };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }

    public new Task DisposeAsync()
    {
        return _dbContainer.DisposeAsync().AsTask();
    }
}

public class TestAuthData
{
    public string Role { get; set; } = "User";
    public string UserId { get; set; } = Guid.NewGuid().ToString();
}