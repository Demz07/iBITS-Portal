// ============================================================
// FILE PATH: Utilities/RoleInitializer.cs
// ============================================================
// UPDATED: Added default admin user creation
// Roles: Admin, Officer, Member, Org Secretary, Class Secretary, 
//        Org Treasurer, Class Treasurer
// Default Admin: admin@ibits.edu.ph / Admin@123
// ============================================================

using Microsoft.AspNetCore.Identity;

namespace iBITS_Portal.Utilities
{
    public static class RoleInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Define the complete list of roles your application needs
            string[] roleNames = {
                "Admin",           // System administrator
                "Officer",         // Generic officer role
                "Member",          // Default role for regular students
                "Org Secretary",   // Organization-level secretary
                "Class Secretary", // Class-level secretary
                "Org Treasurer",   // Organization-level treasurer
                "Class Treasurer"  // Class-level treasurer
            };

            // Create roles if they don't exist
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default admin user if it doesn't exist
            var adminEmail = "admin@ibits.edu.ph";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(newAdmin, "Admin@123");

                if (createAdmin.Succeeded)
                {
                    // Assign Admin role to the new user
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            else
            {
                // Ensure existing admin user has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
