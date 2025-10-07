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
        public const string Supplier = "Supplier";
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
                Roles.MaintenanceTechnician, Roles.PurchasingManager, Roles.Customer , Roles.Supplier
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
                            CreatedAt = DateTime.Now,
                            Position = "Admin",
                            PhoneNumber = "0000000000",
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
                    // Gauteng (Region ID = 1)
                    new Location { Description = "Johannesburg Office", Address = "Market Street", City = "Johannesburg", Province = Region.Gauteng }, // ID = 1
                    new Location { Description = "Pretoria Depot", Address = "Church Street", City = "Pretoria", Province = Region.Gauteng },          // ID = 1
                    new Location { Description = "Midrand Distribution", Address = "Old Pretoria Road", City = "Midrand", Province = Region.Gauteng }, // ID = 1

                    // KwaZulu-Natal (Region ID = 2)
                    new Location { Description = "Durban Depot", Address = "Beach Avenue", City = "Durban", Province = Region.KwaZuluNatal }, // ID = 2
                    new Location { Description = "Pietermaritzburg Branch", Address = "Chief Albert Luthuli Street", City = "Pietermaritzburg", Province = Region.KwaZuluNatal }, // ID = 2
                    new Location { Description = "Richards Bay Hub", Address = "Bayview Road", City = "Richards Bay", Province = Region.KwaZuluNatal }, // ID = 2
        
                    // Western Cape (Region ID = 3)
                    new Location { Description = "Cape Town HQ", Address = "Long Street", City = "Cape Town", Province = Region.WesternCape },       // ID = 3
                    new Location { Description = "George Branch", Address = "York Street", City = "George", Province = Region.WesternCape },         // ID = 3
                    new Location { Description = "Stellenbosch Depot", Address = "Dorp Street", City = "Stellenbosch", Province = Region.WesternCape }, // ID = 3

                    // Eastern Cape (Region ID = 4)
                    new Location { Description = "Gqeberha Depot", Address = "Njoli Street", City = "Gqeberha", Province = Region.EasternCape }, // ID = 4
                    new Location { Description = "East London Branch", Address = "Oxford Street", City = "East London", Province = Region.EasternCape }, // ID = 4
                    new Location { Description = "Mthatha Hub", Address = "Nelson Mandela Drive", City = "Mthatha", Province = Region.EasternCape }, // ID = 4

                    // Free State (Region ID = 5)
                    new Location { Description = "Bloemfontein HQ", Address = "President Brand Street", City = "Bloemfontein", Province = Region.FreeState }, // ID = 5
                    new Location { Description = "Welkom Branch", Address = "Stateway", City = "Welkom", Province = Region.FreeState },             // ID = 5

                    // Limpopo (Region ID = 6)
                    new Location { Description = "Polokwane Depot", Address = "Landros Mare Street", City = "Polokwane", Province = Region.Limpopo }, // ID = 6
                    new Location { Description = "Thohoyandou Branch", Address = "University Road", City = "Thohoyandou", Province = Region.Limpopo }, // ID = 6

                    // Mpumalanga (Region ID = 7)
                    new Location { Description = "Mbombela Office", Address = "Riverside Park", City = "Mbombela", Province = Region.Mpumalanga }, // ID = 7
                    new Location { Description = "Witbank Depot", Address = "Mandela Drive", City = "Witbank", Province = Region.Mpumalanga },  // ID = 7

                    // North West (Region ID = 8)
                    new Location { Description = "Rustenburg Branch", Address = "Nelson Mandela Street", City = "Rustenburg", Province = Region.NorthWest }, // ID = 8
                    new Location { Description = "Mahikeng Depot", Address = "Main Road", City = "Mahikeng", Province = Region.NorthWest },      // ID = 8

                    // Northern Cape (Region ID = 9)
                    new Location { Description = "Kimberley Depot", Address = "Du Toitspan Road", City = "Kimberley", Province = Region.NorthernCape }, // ID = 9
                    new Location { Description = "Upington Branch", Address = "Schröder Street", City = "Upington", Province = Region.NorthernCape } // ID = 9
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
