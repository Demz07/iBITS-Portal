# 🔄 PostgreSQL Migration Guide

This folder contains everything you need to migrate iBITS Portal from SQL Server to PostgreSQL for deployment on Railway.app.

---

## 📁 Files in This Folder

| File | Purpose |
|------|---------|
| `01_Migrate_To_PostgreSQL.ps1` | Automated script to update project packages and files |
| `02_Export_Data_From_MSSQL.ps1` | Export your current data to CSV files |
| `Program_PostgreSQL.cs` | Updated Program.cs for PostgreSQL |
| `appsettings.PostgreSQL.json` | PostgreSQL connection string template |
| `README.md` | This file |

---

## 🚀 Quick Start (3 Steps)

### Step 1: Run Migration Script
```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\PostgreSQL_Migration"
.\01_Migrate_To_PostgreSQL.ps1
```

### Step 2: Install PostgreSQL Locally (Optional - for testing)
Download: https://www.postgresql.org/download/windows/

**Or skip this and deploy directly to Railway!**

### Step 3: Export Your Data
```powershell
.\02_Export_Data_From_MSSQL.ps1
```

---

## 📊 What Gets Changed

### Packages
- ❌ **Removed:** `Microsoft.EntityFrameworkCore.SqlServer`
- ✅ **Added:** `Npgsql.EntityFrameworkCore.PostgreSQL`

### Code Changes
- `Program.cs`: `UseSqlServer()` → `UseNpgsql()`
- Connection strings updated for PostgreSQL format

### Data Migration
- All tables exported to CSV
- Ready for import to PostgreSQL

---

## 🗄️ Database Differences (SQL Server vs PostgreSQL)

| Feature | SQL Server | PostgreSQL | Impact |
|---------|-----------|------------|--------|
| **Column Names** | Case-insensitive | Case-sensitive | ⚠️ Use lowercase |
| **Auto-increment** | `IDENTITY` | `SERIAL` | ✅ EF handles this |
| **String Type** | `NVARCHAR` | `TEXT` / `VARCHAR` | ✅ EF handles this |
| **Date Type** | `DATETIME2` | `TIMESTAMP` | ✅ EF handles this |
| **Boolean** | `BIT` | `BOOLEAN` | ✅ EF handles this |

**Good News:** Entity Framework Core handles most differences automatically! ✨

---

## 🧪 Test Locally (Optional)

### 1. Install PostgreSQL
```powershell
# Using Chocolatey (if installed)
choco install postgresql

# Or download from: https://www.postgresql.org/download/
```

### 2. Update appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=PortaliBITS;Username=postgres;Password=your_password"
  }
}
```

### 3. Create Migrations
```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
dotnet ef migrations add InitialPostgreSQL
dotnet ef database update
```

### 4. Run the App
```powershell
dotnet run
```

---

## 🚂 Deploy to Railway (Recommended)

**Skip local testing and deploy directly!**

See: `RAILWAY_DEPLOYMENT_GUIDE.md` in the parent folder

---

## ⚠️ Important Notes

1. **Backup First:** The script creates `Program.cs.sqlserver.backup`
2. **Test Your App:** After migration, test all features
3. **Data Export:** CSV files saved to `ExportedData/` folder
4. **Rollback:** To revert, restore `Program.cs.sqlserver.backup`

---

## 🆘 Troubleshooting

### Error: "Npgsql package not found"
```powershell
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
```

### Error: "Migration failed"
1. Delete `Migrations` folder
2. Run: `dotnet ef migrations add InitialPostgreSQL`
3. Run: `dotnet ef database update`

### Error: "Connection failed"
- Check PostgreSQL is running: `Get-Service postgresql*`
- Verify connection string in `appsettings.json`
- Test connection with pgAdmin

---

## 📞 Need Help?

Check the main deployment guides:
- `FREE_HOSTING_OPTIONS.md` - Compare platforms
- `RAILWAY_DEPLOYMENT_GUIDE.md` - Complete Railway guide

---

**Ready to migrate? Run `01_Migrate_To_PostgreSQL.ps1` to get started!** 🚀
