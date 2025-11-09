using System.Net.Http.Headers;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>
{
    protected readonly ApplicationDbContext Context;
    protected readonly HttpClient Client;
    protected readonly HttpClient AuthenticatedClient;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        var scope = factory.Services.CreateScope();
        Context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Клієнт без авторизації
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Клієнт з авторизацією (за замовчуванням User роль)
        AuthenticatedClient = factory.WithWebHostBuilderMock()
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        AuthenticatedClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue(scheme: "TestScheme");
    }

    protected HttpClient CreateAuthenticatedClient(string role = "User", string userId = null)
    {
        var client = new IntegrationTestWebFactory()
            .WithWebHostBuilderMock(role, userId)
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue(scheme: "TestScheme");

        return client;
    }

    protected async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }
}