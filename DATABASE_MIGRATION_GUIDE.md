# iBITS Portal - Database Migration Guide

## 🔴 **ROOT CAUSE OF 500 ERRORS**

Your Railway PostgreSQL database has:
- ✅ **Correct table structure** (all migrations ran successfully)
- ❌ **NO DATA** (zero Student records, possibly zero AspNetUsers)

### Why You Can't Login:
1. You try to login → User doesn't exist in `AspNetUsers` table
2. OR User exists but has no matching record in `Student` table
3. Code at `HomeController.cs:60-71` checks for Student record
4. If not found → Logs you out → Shows error page

---

## 📊 **Solution Options**

### **Option 1: Export Data from MS SQL Server** (Recommended)

**Step 1: Export Data with INSERT Statements**

In **SQL Server Management Studio (SSMS)**:

1. Right-click your database → **Tasks** → **Generate Scripts**
2. Click **Next** → Select **Select specific database objects**
3. Check these tables:
   - `AspNetUsers`
   - `AspNetUserRoles`
   - `AspNetRoles`
   - `Student`
   - `Officers`
   - `Event`
   - `Attendance`
   - `Fees`
   - `Fines`
   - `Announcements`
   - `Notification`
   - `PendingRoleChanges`
   - `PaymentTransactions`
   - `FinePaymentTransactions`
   - `Remittances`
   - `RemittanceItems`
   
4. Click **Next** → **Advanced**
5. Set these options:
   - **Types of data to script**: `Data only` OR `Schema and data`
   - **Script for Server Version**: `SQL Server 2016` or higher
   
6. Click **OK** → **Next** → **Finish**
7. Save as `iBITS_Portal_DATA.sql`

**Step 2: Convert SQL Server to PostgreSQL**

The exported SQL will use SQL Server syntax. You need to convert it to PostgreSQL:

```bash
# Install pgloader (Windows)
# Download from: https://pgloader.io/

# OR use online converter:
# https://www.sqlines.com/online
```

**Manual Conversion Tips:**
- `[TableName]` → `"TableName"` (brackets to quotes)
- `IDENTITY(1,1)` → `SERIAL` or `GENERATED ALWAYS AS IDENTITY`
- `nvarchar(max)` → `text`
- `datetime2` → `timestamp`
- `bit` → `boolean`
- `0` → `false`, `1` → `true` (for boolean values)

**Step 3: Apply to Railway PostgreSQL**

Once converted, apply the data:

```bash
# Connect to Railway PostgreSQL
railway connect postgres

# OR use connection string from Railway
psql "postgresql://username:password@host:port/database" -f iBITS_Portal_DATA_Converted.sql
```

---

### **Option 2: Manual Data Entry** (For Testing Only)

Create a test admin user directly in PostgreSQL:

```sql
-- Connect to Railway PostgreSQL
-- Go to Railway dashboard → PostgreSQL → Connect

-- 1. Create a test admin user in AspNetUsers
INSERT INTO "AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
VALUES 
('test-admin-id-001', 'admin', 'ADMIN', 'admin@ibits.com', 'ADMIN@IBITS.COM', true, 
'AQAAAAIAAYagAAAAEJ...', -- You need to generate this
CAST(uuid_generate_v4() AS text), 
CAST(uuid_generate_v4() AS text), 
false, false, false, 0);

-- 2. Find the Admin role ID
SELECT "Id", "Name" FROM "AspNetRoles" WHERE "Name" = 'Admin';

-- 3. Assign admin role (use the Id from step 2)
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('test-admin-id-001', 'role-id-from-step-2');
```

**Generate Password Hash:**

You need to run this C# code locally to get the password hash:

```csharp
using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<IdentityUser>();
var hash = hasher.HashPassword(null, "YourPassword123!");
Console.WriteLine(hash);
```

---

### **Option 3: Use EF Core Data Seeding** (Best Long-Term Solution)

Add data seeding to your application:

**File**: `iBITS Portal/Data/DbInitializer.cs`

```csharp
public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortaliBitsContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created
        await context.Database.MigrateAsync();

        // Create roles if they don't exist
        string[] roles = { "Admin", "Member", "Officer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default admin user
        var adminEmail = "admin@ibits.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin@123!");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
```

**Update Program.cs** (after line 86):

```csharp
// Add after: await context.Database.MigrateAsync();
await DbInitializer.Initialize(app.Services);
```

---

## ⚡ **IMMEDIATE FIX (Temporary)**

To test if the database is working, you can temporarily modify the code to create a test user:

**Add to Program.cs** after migrations (line 86):

```csharp
// TEMPORARY: Seed test data
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    // Create Admin role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    
    // Create test admin
    if (await userManager.FindByNameAsync("admin") == null)
    {
        var admin = new IdentityUser { UserName = "admin", Email = "admin@test.com" };
        await userManager.CreateAsync(admin, "Admin@123!");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
```

---

## 🎯 **Recommended Action Plan**

1. **Export your data from MS SQL** with INSERT statements
2. **Convert to PostgreSQL format** (use online tool or pgloader)
3. **Apply to Railway database**
4. **OR use Option 3** to add data seeding to your app

---

## ❓ **Questions?**

Let me know which option you'd like to pursue and I can help you implement it!
