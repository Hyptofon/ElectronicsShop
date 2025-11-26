using Application.Authentication.Interfaces; 
using Application.Common.Interfaces;
using Infrastructure.Authentication; 
using Infrastructure.FileStorage;
using Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ConfigureInfrastructureServices
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistenceServices(configuration);
        services.AddFileStorageServices();
        
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<ApplicationDbContextInitialiser>();
    }

    private static void AddFileStorageServices(this IServiceCollection services)
    {
        services.AddScoped<IFileStorage, LocalFileStorageService>();
    }
}