using System.Net.Http.Headers;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebFactory>
{
    protected readonly ApplicationDbContext Context;
    protected readonly HttpClient Client;
    protected readonly HttpClient AuthenticatedClient;
    protected readonly IntegrationTestWebFactory Factory;

    protected BaseIntegrationTest(IntegrationTestWebFactory factory)
    {
        Factory = factory;
        
        var scope = factory.Services.CreateScope();
        Context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        
        AuthenticatedClient = CreateAuthenticatedClient();
    }

    protected HttpClient CreateAuthenticatedClient(string role = "User", string? userId = null)
    {
        var authenticatedFactory = Factory.WithWebHostBuilderMock(role, userId ?? Guid.NewGuid().ToString());

        var client = authenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
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
    
    protected async Task CleanupDatabaseAsync()
    {
        try
        {
            // ВАЖЛИВО: Завантажуємо всі сутності з їх залежностями
            var carts = await Context.Carts
                .Include(c => c.Items)
                .ToListAsync();
            
            var orders = await Context.Orders
                .Include(o => o.Items)
                .ToListAsync();
            
            var products = await Context.Products
                .Include(p => p.Reviews)
                .Include(p => p.Images)
                .Include(p => p.Categories)
                .ToListAsync();
            
            // Видаляємо в правильному порядку
            Context.Carts.RemoveRange(carts);
            Context.Orders.RemoveRange(orders);
            Context.Products.RemoveRange(products);
            
            await Context.SaveChangesAsync();
            
            // Тепер можна безпечно видалити категорії
            var categories = await Context.Categories.ToListAsync();
            Context.Categories.RemoveRange(categories);
            
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
        }
        catch (Exception)
        {
            // Ігноруємо помилки при очищенні
            Context.ChangeTracker.Clear();
        }
    }
}