using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Code_Vanta_Systems.Services
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string CustomerLiaison = "Customer Liaison";
        public const string InventoryLiaison = "Inventory Liaison";
        public const string ServiceTechnician = "Service Technician";
        public const string FaultTechnician = "Fault Technician";
        public const string MaintenanceTechnician = "Maintenance Technician";
        public const string PurchasingManager = "Purchasing Manager";
        public const string Customer = "Customer";
    }

    public class SeedService
    {
        public static async Task SeedDatabase(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedService>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            string[] roles = {
                Roles.Admin, Roles.CustomerLiaison, Roles.InventoryLiaison,
                Roles.ServiceTechnician, Roles.FaultTechnician,
                Roles.MaintenanceTechnician, Roles.PurchasingManager, Roles.Customer
            };

            // Create roles
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                        logger.LogInformation($"Role '{role}' created.");
                    else
                        logger.LogError($"Failed to create role '{role}': {string.Join(", ", result.Errors)}");
                }
            }

            // Create admin user
            var adminEmail = "admin@fridge.com";
            var adminPassword = config["Seed:AdminPassword"] ?? "Admin@123!";
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

                var result = await userManager.CreateAsync(newAdmin, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, Roles.Admin);
                    logger.LogInformation("Admin user created and assigned to 'Admin' role.");

                    var existingEmployee = await dbContext.Employees
                        .FirstOrDefaultAsync(e => e.ApplicationUserId == newAdmin.Id);

                    if (existingEmployee == null)
                    {
                        var adminEmployee = new Employee
                        {
                            ApplicationUserId = newAdmin.Id,
                            Role = EmployeeRole.Admin,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        dbContext.Employees.Add(adminEmployee);
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Admin employee profile created.");
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"Error creating admin user: {error.Description}");
                    }
                }
            }
            else
            {
                logger.LogInformation("Admin user already exists.");
            }

            // Seed valid locations
            if (!await dbContext.Locations.AnyAsync())
            {
                var locations = new List<Location>
                {
                    new Location
                    {
                        Description = "George Branch",
                        Address = "123 Main Street",
                        City = "George",
                        Province = Region.WesternCape
                    },
                    new Location
                    {
                        Description = "Cape Town HQ",
                        Address = "456 Long Street",
                        City = "Cape Town",
                        Province = Region.WesternCape
                    },
                    new Location
                    {
                        Description = "Johannesburg Office",
                        Address = "789 Market Road",
                        City = "Johannesburg",
                        Province = Region.Gauteng
                    },
                    new Location
                    {
                        Description = "Durban Depot",
                        Address = "321 Beach Avenue",
                        City = "Durban",
                        Province = Region.KwaZuluNatal
                    }
                };

                dbContext.Locations.AddRange(locations);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Default locations seeded.");
            }
            else
            {
                logger.LogInformation("Locations already exist. Skipping location seeding.");
            }

            logger.LogInformation("Seeding completed successfully.");
        }

        public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = {
                Roles.Admin, Roles.CustomerLiaison, Roles.InventoryLiaison,
                Roles.ServiceTechnician, Roles.FaultTechnician,
                Roles.MaintenanceTechnician, Roles.PurchasingManager, Roles.Customer
            };

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
