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

            // Define the complete list of roles your application needs
            // UPDATED: Role names now have spaces for better readability
            string[] roleNames = {
                "Admin",           // System administrator (not assignable to students via UI)
                "Officer",         // Generic officer role
                "Member",          // Default role for regular students
                "Org Secretary",   // Organization-level secretary
                "Class Secretary", // Class-level secretary (unique per section)
                "Org Treasurer",   // Organization-level treasurer
                "Class Treasurer"  // Class-level treasurer (unique per section)
            };

            // Loop through the names and create the role only if it doesn't already exist
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}
