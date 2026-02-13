# Database Migration Instructions

## Problem
```
SqlException: Invalid column name 'Program'.
```

## Solution
Run the SQL migration script to add the `Program` column to the Remittances table.

---

## Option 1: Run SQL Script Directly (Recommended)

### Using SQL Server Management Studio (SSMS):
1. Open **SQL Server Management Studio**
2. Connect to your database server
3. Open the file: `Add_Program_Column_Migration.sql`
4. Make sure you're connected to the correct database (iBITSPortal or your database name)
5. Click **Execute** (or press F5)
6. Check the Messages tab for success confirmation

### Using Visual Studio:
1. Open **SQL Server Object Explorer** (View → SQL Server Object Explorer)
2. Expand your database connection
3. Right-click on your database → **New Query**
4. Copy and paste the content from `Add_Program_Column_Migration.sql`
5. Click **Execute** (green arrow)

---

## Option 2: Using Entity Framework Migrations (Alternative)

If you prefer to use EF Core migrations instead:

### Package Manager Console (in Visual Studio):
```powershell
Add-Migration AddProgramToRemittances
Update-Database
```

### Command Line (in project directory):
```bash
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
dotnet ef migrations add AddProgramToRemittances
dotnet ef database update
```

---

## What the Migration Does

1. **Adds the Program column** to the Remittances table (NVARCHAR(50), nullable)
2. **Populates existing records** by extracting the Program from the Section field
   - Example: "BSIT 3-1" → Program = "BSIT"
3. **Displays verification** showing how many records were updated

---

## After Running the Migration

1. Refresh your application
2. Test the remittance views:
   - Pending Remittances
   - Remittance History
   - Validated Remittances
3. Verify that Program is displayed correctly

---

## Rollback (if needed)

If something goes wrong, you can rollback using the commented script at the bottom of the migration file:

```sql
BEGIN TRANSACTION;
ALTER TABLE [dbo].[Remittances] DROP COLUMN [Program];
COMMIT TRANSACTION;
```

---

## Files Created

- `Add_Program_Column_Migration.sql` - The migration script
- `MIGRATION_INSTRUCTIONS.md` - These instructions
