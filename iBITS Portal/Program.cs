// Program.cs

using iBITS_Portal.Data;
using iBITS_Portal.Models;
using iBITS_Portal.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // ✅ Set Philippine Time (UTC+8) as the default timezone for the entire application
            // This ensures ALL DateTime.Now calls return Philippine Standard Time
            Environment.SetEnvironmentVariable("TZ", "Asia/Manila");
            try
            {
                var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                System.AppContext.SetSwitch("System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform", false);
            }
            catch { }

            var builder = WebApplication.CreateBuilder(args);

            // 1. Database Connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDbContext<PortaliBitsContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // 2. FEATURE: Password Protection & Role Management
            builder.Services.AddDefaultIdentity<IdentityUser>(options => {
                options.SignIn.RequireConfirmedAccount = false;

                // Password Settings (Relaxed for Student IDs)
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;

                // Lockout Settings
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // 3. FEATURE: Secure Session Management (5-Minute Auto-Logout)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.SlidingExpiration = true;
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Show detailed errors in all environments to help debug deployment
            app.UseExceptionHandler("/Home/Error");
            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await RoleInitializer.InitializeAsync(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during DB seeding.");
                }
            }

            app.Run();
        }
    }
}
