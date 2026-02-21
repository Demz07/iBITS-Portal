using iBITS_Portal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDefaultData(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                
                logger.LogInformation("Starting database seeding...");

                // Create roles if they don't exist
                string[] roles = { "Admin", "Member", "Officer" };
                foreach (var roleName in roles)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        logger.LogInformation($"Creating role: {roleName}");
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Create default admin user
                var adminUsername = "admin";
                var adminUser = await userManager.FindByNameAsync(adminUsername);
                
                if (adminUser == null)
                {
                    logger.LogInformation("Creating default admin user...");
                    adminUser = new IdentityUser
                    {
                        UserName = adminUsername,
                        Email = "admin@ibits.com",
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123!");
                    
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        logger.LogInformation("✓ Default admin user created successfully");
                        logger.LogInformation("  Username: admin");
                        logger.LogInformation("  Password: Admin@123!");
                    }
                    else
                    {
                        logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    logger.LogInformation("Admin user already exists");
                    
                    // Ensure admin has the Admin role
                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        logger.LogInformation("✓ Added Admin role to existing user");
                    }
                }

                logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }
    }
}
