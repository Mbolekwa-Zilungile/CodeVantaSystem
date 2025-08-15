using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;

namespace Code_Vanta_Systems.Services
{
    public class SeedService
    {
            public static async Task SeedDatabase(IServiceProvider serviceProvider)
            {
                using var scope = serviceProvider.CreateScope();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

                string[] roles = {
            "Admin", "Customer Liaison", "Inventory Liaison",
            "Service Technician", "Fault Technician",
            "Maintenance Technician", "Purchasing Manager"
        };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation($"Role '{role}' created.");
                    }
                }

                var adminEmail = "admin@fridge.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                        logger.LogInformation("Admin user created and assigned to 'Admin' role.");
                    }
                    else
                    {
                        logger.LogError("Failed to create admin user.");
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists.");
                }

        }
    }
}
