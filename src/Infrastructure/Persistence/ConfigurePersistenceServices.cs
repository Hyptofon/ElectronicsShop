using Application.Common.Interfaces;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Users;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure.Persistence;

public static class ConfigurePersistenceServices
{
    public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            configuration.GetConnectionString("DefaultConnection"));
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options => options
            .UseNpgsql(
                dataSource,
                builder => builder.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ApplicationDbContextInitialiser>();
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
        
        services.AddRepositories();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductRepository>(provider => 
            provider.GetRequiredService<ProductRepository>());
        services.AddScoped<IProductQueries>(provider => 
            provider.GetRequiredService<ProductRepository>());

        services.AddScoped<CategoryRepository>();
        services.AddScoped<ICategoryRepository>(provider => 
            provider.GetRequiredService<CategoryRepository>());
        services.AddScoped<ICategoryQueries>(provider => 
            provider.GetRequiredService<CategoryRepository>());

        services.AddScoped<CategoryProductRepository>();
        services.AddScoped<ICategoryProductRepository>(provider => 
            provider.GetRequiredService<CategoryProductRepository>());

        services.AddScoped<ProductImageRepository>();
        services.AddScoped<IProductImageRepository>(provider => 
            provider.GetRequiredService<ProductImageRepository>());

        services.AddScoped<ProductReviewRepository>();
        services.AddScoped<IProductReviewRepository>(provider => 
            provider.GetRequiredService<ProductReviewRepository>());

        services.AddScoped<CartRepository>();
        services.AddScoped<ICartRepository>(provider => 
            provider.GetRequiredService<CartRepository>());

        services.AddScoped<OrderRepository>();
        services.AddScoped<IOrderRepository>(provider => 
            provider.GetRequiredService<OrderRepository>());
    }
}