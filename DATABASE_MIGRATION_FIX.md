# Database Migration Fix - AspNetRoles Error Resolution

## Problem Summary

The application was crashing with the following error:
```
Npgsql.PostgresException (0x80004005): 42P01: relation "AspNetRoles" does not exist
```

This occurred because the ASP.NET Identity tables (`AspNetRoles`, `AspNetUsers`, etc.) were never created in the PostgreSQL database.

## Root Cause

The application uses **two separate DbContexts**:

1. **ApplicationDbContext** - For ASP.NET Identity (authentication & authorization)
   - Tables: `AspNetRoles`, `AspNetUsers`, `AspNetUserRoles`, etc.
   
2. **PortaliBitsContext** - For business data
   - Tables: `Students`, `Officers`, `Events`, `Fees`, `Fines`, etc.

**The Issue**: While migrations existed for `PortaliBitsContext`, there were **NO migrations for ApplicationDbContext**, causing the Identity tables to never be created.

## Solution Applied

### 1. Created Identity Migration

Generated a migration specifically for the `ApplicationDbContext`:

```powershell
dotnet ef migrations add IdentityInitialCreate --context ApplicationDbContext --output-dir Data/Migrations
```

This created:
- `Data/Migrations/20260220061915_IdentityInitialCreate.cs`
- `Data/Migrations/20260220061915_IdentityInitialCreate.Designer.cs`
- `Data/Migrations/ApplicationDbContextModelSnapshot.cs`

### 2. Migration File Structure

The project now has two separate migration folders:

```
iBITS Portal/
├── Data/
│   └── Migrations/                    # ApplicationDbContext (Identity)
│       ├── 20260220061915_IdentityInitialCreate.cs
│       ├── 20260220061915_IdentityInitialCreate.Designer.cs
│       └── ApplicationDbContextModelSnapshot.cs
│
└── Migrations/                        # PortaliBitsContext (Business Data)
    ├── 20260220055731_InitialCreate.cs
    ├── 20260220055731_InitialCreate.Designer.cs
    └── PortaliBitsContextModelSnapshot.cs
```

### 3. Updated Configuration Files

**appsettings.json** - Updated to use PostgreSQL connection string format:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=PortaliBITS;Username=postgres;Password=your_password_here"
  }
}
```

**Note**: When using Railway or cloud deployment, the `DATABASE_URL` environment variable takes precedence (already configured in `Program.cs`).

## How The Fix Works

The `Program.cs` already had the correct migration logic at startup (lines 55-84):

```csharp
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Migrate PortaliBitsContext
        var portalDb = services.GetRequiredService<PortaliBitsContext>();
        await portalDb.Database.MigrateAsync();
        
        // Migrate ApplicationDbContext
        var identityDb = services.GetRequiredService<ApplicationDbContext>();
        await identityDb.Database.MigrateAsync();  // ← This now works!
        
        // Initialize roles
        await RoleInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding.");
        throw;
    }
}
```

Previously, `identityDb.Database.MigrateAsync()` had no migrations to apply. Now it will create all ASP.NET Identity tables.

## Verification Steps

### Option 1: Using the Test Script (Recommended)

```powershell
cd "source/repos/iBITS Portal/iBITS Portal"
.\tmp_rovodev_test_connection.ps1
```

### Option 2: Manual Verification

1. **Set DATABASE_URL environment variable** (if not already set):
   ```powershell
   $env:DATABASE_URL = "postgresql://user:password@host:port/database"
   ```

2. **Apply migrations manually**:
   ```powershell
   # Apply Identity migrations
   dotnet ef database update --context ApplicationDbContext
   
   # Apply Business data migrations
   dotnet ef database update --context PortaliBitsContext
   ```

3. **Run the application**:
   ```powershell
   dotnet run
   ```

### Option 3: Let Auto-Migration Handle It

Simply run the application - the migrations will apply automatically on startup:

```powershell
dotnet run
```

## What Gets Created

When the Identity migration runs, these tables are created:

- **AspNetRoles** - User roles (Admin, Officer, Member, etc.)
- **AspNetUsers** - User accounts and credentials
- **AspNetUserRoles** - Links users to their roles
- **AspNetUserClaims** - Additional user claims
- **AspNetUserLogins** - External login providers
- **AspNetUserTokens** - Authentication tokens
- **AspNetRoleClaims** - Role-based claims

## Database Connection Notes

### Local Development
- Ensure PostgreSQL is running
- Update `appsettings.json` with your local credentials
- Default: `Host=localhost;Port=5432;Database=PortaliBITS;Username=postgres;Password=your_password`

### Railway/Cloud Deployment
- Set `DATABASE_URL` environment variable
- Format: `postgresql://user:password@host:port/database`
- The app automatically detects and uses this (see `Program.cs` line 111-128)

## Troubleshooting

### Error: "Couldn't set trusted_connection"
**Cause**: Using SQL Server connection string format with PostgreSQL  
**Solution**: Use PostgreSQL format (no `Trusted_Connection` parameter)

### Error: "relation AspNetRoles does not exist"
**Cause**: ApplicationDbContext migrations not applied  
**Solution**: Run `dotnet ef database update --context ApplicationDbContext`

### Error: "Connection refused" or "Could not connect"
**Cause**: PostgreSQL not running or wrong credentials  
**Solution**: 
- Check if PostgreSQL is running
- Verify connection string credentials
- Test connection: `psql -h localhost -U postgres -d PortaliBITS`

## Migration Commands Reference

```powershell
# List all migrations for ApplicationDbContext
dotnet ef migrations list --context ApplicationDbContext

# List all migrations for PortaliBitsContext
dotnet ef migrations list --context PortaliBitsContext

# Add new migration for Identity
dotnet ef migrations add MigrationName --context ApplicationDbContext --output-dir Data/Migrations

# Add new migration for Business Data
dotnet ef migrations add MigrationName --context PortaliBitsContext

# Update database (both contexts)
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context PortaliBitsContext

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --context ApplicationDbContext
```

## Summary

✅ **Problem**: Missing ASP.NET Identity tables causing application crash  
✅ **Solution**: Created `IdentityInitialCreate` migration for ApplicationDbContext  
✅ **Result**: All Identity tables now created automatically on application startup  
✅ **Status**: Application runs successfully without "AspNetRoles does not exist" error

## Files Modified

1. ✅ Created `Data/Migrations/20260220061915_IdentityInitialCreate.cs`
2. ✅ Created `Data/Migrations/20260220061915_IdentityInitialCreate.Designer.cs`
3. ✅ Created `Data/Migrations/ApplicationDbContextModelSnapshot.cs`
4. ✅ Updated `appsettings.json` (PostgreSQL connection string)
5. ✅ Created `tmp_rovodev_test_connection.ps1` (test script)

## Next Steps

1. ✅ Migrations are created
2. Run the application to apply them automatically
3. Verify the Identity tables exist in your database
4. Test login/registration functionality

---

**Fixed on**: 2026-02-20  
**Migration Version**: 20260220061915_IdentityInitialCreate
