using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace iBITS_Portal.Data
{
    public static class DataSeeder
    {
        public static async Task SeedFromSqlFile(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Checking if data seeding is needed...");
                
                // Check if data already exists
                var hasUsers = await context.Users.AnyAsync();
                if (hasUsers)
                {
                    logger.LogInformation("Database already has data. Skipping seeding.");
                    return;
                }
                
                logger.LogInformation("No data found. Starting data import from seed_data.sql...");
                
                string sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "seed_data.sql");
                
                if (!File.Exists(sqlFilePath))
                {
                    logger.LogWarning($"Seed data file not found at: {sqlFilePath}");
                    return;
                }
                
                string sql = await File.ReadAllTextAsync(sqlFilePath);
                logger.LogInformation($"Read {sql.Length} characters from seed file");
                
                logger.LogInformation("Executing seed SQL (this may take 30-60 seconds)...");
                await context.Database.ExecuteSqlRawAsync(sql);
                
                logger.LogInformation("Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during data seeding");
            }
        }
    }
}