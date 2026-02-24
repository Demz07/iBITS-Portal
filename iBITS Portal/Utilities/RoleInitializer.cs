// ============================================================
// FILE PATH: Utilities/RoleInitializer.cs
// ============================================================
// UPDATED: Role names now include spaces to match display format
// Roles: Admin, Officer, Member, Org Secretary, Class Secretary, 
//        Org Treasurer, Class Treasurer
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

            // 1. Define and Create Roles
            string[] roleNames = {
                "Admin",
                "Officer",
                "Member",
                "Org Secretary",
                "Class Secretary",
                "Org Treasurer",
                "Class Treasurer"
            };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Create Default Admin if none exists
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                var user = new IdentityUser
                {
                    UserName = "admin",
                    Email = "admin@ibits.edu.ph",
                    EmailConfirmed = true
                };

                // Default password for first-time login
                string adminPassword = "Admin@123";
                var createPowerUser = await userManager.CreateAsync(user, adminPassword);

                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
    }
}
