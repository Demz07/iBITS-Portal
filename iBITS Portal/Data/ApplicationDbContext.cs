using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace iBITS_Portal.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Note: ExcuseRequests DbSet moved to PortaliBitsContext to avoid duplication
        // All business logic entities should be in PortaliBitsContext only
    }
}
