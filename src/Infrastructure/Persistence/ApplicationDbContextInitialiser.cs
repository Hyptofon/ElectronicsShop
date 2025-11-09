using Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager)
{
    public async Task InitialiseAsync()
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            await SeedRolesAsync();
            await SeedUsersAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while initialising the database.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { ApplicationRole.Admin, ApplicationRole.Manager, ApplicationRole.User };

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        // Seed Admin
        var adminEmail = "admin@shop.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = ApplicationUser.Create(
                adminEmail, 
                "Admin", 
                "User", 
                "admin");
            
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, ApplicationRole.Admin);
                logger.LogInformation("Admin user created successfully");
            }
        }

        // Seed Manager
        var managerEmail = "manager@shop.com";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);
        
        if (managerUser == null)
        {
            managerUser = ApplicationUser.Create(
                managerEmail, 
                "Manager", 
                "User", 
                "manager");
            
            var result = await userManager.CreateAsync(managerUser, "Manager@123");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, ApplicationRole.Manager);
                logger.LogInformation("Manager user created successfully");
            }
        }

        // Seed Regular User
        var userEmail = "user@shop.com";
        var regularUser = await userManager.FindByEmailAsync(userEmail);
        
        if (regularUser == null)
        {
            regularUser = ApplicationUser.Create(
                userEmail, 
                "Regular", 
                "User", 
                "user");
            
            var result = await userManager.CreateAsync(regularUser, "User@123");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, ApplicationRole.User);
                logger.LogInformation("Regular user created successfully");
            }
        }
    }
}