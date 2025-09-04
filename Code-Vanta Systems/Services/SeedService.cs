using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Code_Vanta_Systems.Services
{
    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();

            // Define roles
            string[] roles = {
                "Admin", "Customer Liaison", "Inventory Liaison",
                "Service Technician", "Fault Technician",
                "Maintenance Technician", "Purchasing Manager", "Customer"
            };

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation($"Role '{role}' created.");
                }
            }

            // Create admin user
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

                    // Create Employee profile for admin
                    var adminEmployee = new Employee
                    {
                        ApplicationUserId = newAdmin.Id,
                        Role = EmployeeRole.PurchasingManager, // Adjust if needed
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    dbContext.Employees.Add(adminEmployee);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Admin employee profile created.");
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

        // Optional: Separate method to seed roles only
        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Technician", "Customer Liaison" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
