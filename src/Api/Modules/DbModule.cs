using Infrastructure.Persistence;

namespace Api.Modules;

public static class DbModule
{
    public static async Task InitialiseDatabaseAsync(this WebApplication application)
    {
        using var scope = application.Services.CreateScope();
        
        var initialiser = scope.ServiceProvider.GetService<ApplicationDbContextInitialiser>();

        if (initialiser != null)
        {
            await initialiser.InitialiseAsync();
        }
    }
}