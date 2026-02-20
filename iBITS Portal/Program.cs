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
            var builder = WebApplication.CreateBuilder(args);

            // 1. Database Connection - Railway compatible
            var connectionString = GetConnectionString(builder.Configuration);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddDbContext<PortaliBitsContext>(options =>
                options.UseNpgsql(connectionString));

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
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // 3. FEATURE: Secure Session Management (5-Minute Auto-Logout)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // UPDATED: Changed to 15 Minutes
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                options.SlidingExpiration = true;
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // AUTO-RUN MIGRATIONS ON STARTUP (for Railway)
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Running database migrations...");

                    // Migrate PortaliBitsContext
                    var portalDb = services.GetRequiredService<PortaliBitsContext>();
                    await portalDb.Database.MigrateAsync();
                    logger.LogInformation("PortaliBitsContext migration completed");

                    // Migrate ApplicationDbContext
                    var identityDb = services.GetRequiredService<ApplicationDbContext>();
                    await identityDb.Database.MigrateAsync();
                    logger.LogInformation("ApplicationDbContext migration completed");

                    // Initialize roles
                    await RoleInitializer.InitializeAsync(services);
                    logger.LogInformation("Roles initialized successfully");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred during database migration or seeding.");
                    throw; // Re-throw to prevent app from starting with broken DB
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static string GetConnectionString(IConfiguration configuration)
        {
            // Try to get DATABASE_URL from environment (Railway)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // Parse Railway's DATABASE_URL format: postgresql://user:password@host:port/database
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                
                return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
            }

            // Fallback to appsettings.json
            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }
    }
}
