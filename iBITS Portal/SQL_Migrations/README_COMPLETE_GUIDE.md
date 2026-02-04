# iBITS Portal - Database Migration & Cleanup Guide

**Created:** 2026-02-03  
**Status:** Ready to Execute

---

## 📋 Overview

This guide provides complete instructions for fixing all pending migrations and optionally resetting your database.

## 🔧 Issues Identified & Fixed

### 1. **Pending Migration Error**
- **Error:** `SqlException: Invalid object name 'UserAnnouncementDismissals'`
- **Cause:** Database schema is out of sync with Entity Framework models
- **Solution:** Run the master migration script

### 2. **Missing Database Objects**

#### Missing Tables:
- ✅ `UserAnnouncementDismissals` - Tracks dismissed announcements per student
- ✅ `Remittances` - Batch remittances from Class Treasurer to Org Treasurer
- ✅ `RemittanceItems` - Individual payment items within remittances

#### Missing Columns:
- ✅ `Announcements.ExpiryDate` - Auto-hide announcements after expiry
- ✅ `Fees.AmountPaid` - Track partial payments
- ✅ `Fees.RemittanceStatus` - Track remittance workflow
- ✅ `Fees.RemittanceId` - Link to remittance batch
- ✅ `Fees.CollectedBy` - Class Treasurer who collected
- ✅ `Fees.CollectionDate` - Date collected
- ✅ `Fees.OfficialPaymentDate` - Official validation date
- ✅ `Fines.AmountPaid` - Track partial payments
- ✅ `Fines.RemittanceStatus` - Track remittance workflow
- ✅ `Fines.RemittanceId` - Link to remittance batch
- ✅ `Fines.CollectedBy` - Class Treasurer who collected
- ✅ `Fines.CollectionDate` - Date collected
- ✅ `Fines.OfficialPaymentDate` - Official validation date

---

## 🚀 Step-by-Step Instructions

### **Option 1: Fix Migrations Only (Recommended)**

This option preserves all your existing data and only adds missing database objects.

#### Step 1: Backup Your Database
```sql
-- In SQL Server Management Studio (SSMS):
-- Right-click on PortaliBITS database
-- Tasks > Back Up...
-- Choose "Full" backup
-- Save to a safe location
```

#### Step 2: Run the Master Migration Script
1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your database server
3. Open the file: `MASTER_FIX_ALL_PENDING_MIGRATIONS.sql`
4. Ensure you're connected to the `PortaliBITS` database
5. Click **Execute** (F5)
6. Review the output messages

**Expected Output:**
```
========================================================================
iBITS Portal - MASTER MIGRATION SCRIPT
Started at: 2026-02-03 XX:XX:XX
========================================================================

SECTION 1: Announcements Table
    ✓ ExpiryDate column added successfully

SECTION 2: UserAnnouncementDismissals Table
    ✓ Table created successfully
    ✓ Unique constraint created
    ✓ Foreign keys created
    ✓ Indexes created

SECTION 3: Remittances Table
    ✓ Table created successfully
    ... (continues)

MIGRATION COMPLETED SUCCESSFULLY!
✓ Transaction committed successfully
```

#### Step 3: Verify in Your Application
1. Stop your application (if running)
2. Rebuild the solution:
   ```bash
   cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
   dotnet build
   ```
3. Run your application
4. The migration warnings should be gone!

---

### **Option 2: Complete Database Reset (Fresh Start)**

⚠️ **WARNING:** This will delete ALL data except admin accounts!

Use this option if you want to start with a clean database for testing or development.

#### Step 1: Backup Your Database
```sql
-- Same as Option 1 - ALWAYS backup first!
```

#### Step 2: Run the Master Migration Script FIRST
- Follow **Option 1** steps above to ensure all tables exist

#### Step 3: Run the Complete Wipe Script
1. Open **SSMS**
2. Open the file: `iBITSPortal_CompleteWipe_Script.sql` (from Downloads folder)
3. **CAREFULLY REVIEW** what will be deleted
4. Execute the script
5. Review the output showing protected admin accounts
6. If everything looks correct, run:
   ```sql
   COMMIT TRANSACTION;
   ```
   Or to undo:
   ```sql
   ROLLBACK TRANSACTION;
   ```

**What Gets Deleted:**
- ✅ ALL students
- ✅ ALL attendance records
- ✅ ALL fines and payments
- ✅ ALL fees and remittances
- ✅ ALL events and announcements
- ✅ ALL activity logs
- ✅ ALL non-admin users

**What's Protected:**
- ✅ Admin accounts only
- ✅ Database structure (tables, views, stored procedures)

---

## 📁 File Locations

### Migration Scripts (Project Folder):
```
C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal\SQL_Migrations\
├── MASTER_FIX_ALL_PENDING_MIGRATIONS.sql    ← Run this to fix all issues
├── 002_Add_UserAnnouncementDismissals_Table.sql
├── 001_Add_Announcement_Expiry_And_Dismissal_FIXED.sql
└── README_COMPLETE_GUIDE.md                  ← This file
```

### Cleanup Scripts (Downloads Folder):
```
C:\Users\Dave\Downloads\
├── iBITSPortal_CompleteWipe_Script.sql      ← Complete data reset
├── iBITSPortal_SafeCleanup_Script.sql       ← Old version (not needed)
└── iBITSPortal_Script.sql                   ← Original database script
```

---

## 🔍 Troubleshooting

### Issue: "Script runs but error persists"
**Solution:**
1. Restart your application
2. Clear the EF Core cache:
   ```bash
   dotnet ef database update
   ```
3. Rebuild the solution

### Issue: "Foreign key constraint error"
**Solution:**
The script is idempotent and checks for existing objects. You can safely run it multiple times.

### Issue: "Admin account was deleted"
**Solution:**
The admin account should be protected. Check the script output for which accounts were preserved. If accidentally deleted, restore from backup.

### Issue: "Still seeing migration warnings"
**Solution:**
After running the SQL script, you may need to create a sync migration:
```bash
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
dotnet ef migrations add SyncDatabaseSchema
```

This creates an empty migration that tells EF Core the database is in sync.

---

## ✅ Verification Steps

After running the migration script, verify everything is working:

### 1. Check Database Objects
```sql
-- Check if UserAnnouncementDismissals table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'UserAnnouncementDismissals';

-- Check if Remittances table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'Remittances';

-- Check if new columns exist in Announcements
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Announcements';

-- Check if new columns exist in Fees
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Fees'
AND COLUMN_NAME IN ('AmountPaid', 'RemittanceStatus', 'RemittanceId', 'CollectedBy');

-- Check if new columns exist in Fines
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Fines'
AND COLUMN_NAME IN ('AmountPaid', 'RemittanceStatus', 'RemittanceId', 'CollectedBy');
```

### 2. Test Application Features
- ✅ Login as admin
- ✅ Create a new announcement
- ✅ Dismiss an announcement (as student)
- ✅ Create fees/fines
- ✅ Test remittance workflow (if implemented)

---

## 📊 Database Schema Summary

### Core Tables
- `Student` - Student records
- `Attendance` - Event attendance tracking
- `Event` - Events/activities
- `Fees` - Student fees
- `Fines` - Student fines
- `Officers` - Student officers

### Financial Management
- `PaymentTransactions` - Fee payment records
- `FinePaymentTransactions` - Fine payment records
- `Remittances` - Remittance batches
- `RemittanceItems` - Individual remittance items

### Communication
- `Announcements` - System announcements
- `Notifications` - Student notifications
- `UserAnnouncementDismissals` - Dismissed announcements

### System
- `ActivityLogs` - Audit trail
- `SystemSettings` - Configuration
- `PendingRoleChanges` - Role change requests

### Archives
- `ArchivedEvents` - Archived events
- `ArchivedAnnouncements` - Archived announcements

---

## 🎯 Next Steps

After successfully running the migration:

1. ✅ **Test thoroughly** - Verify all features work correctly
2. ✅ **Update documentation** - Document any schema changes for your team
3. ✅ **Consider version control** - Add these scripts to your Git repository
4. ✅ **Create regular backups** - Set up automated database backups
5. ✅ **Monitor logs** - Check application logs for any EF Core warnings

---

## 📞 Support

If you encounter issues:

1. Review the script output messages carefully
2. Check the SQL Server error log
3. Verify your database connection string
4. Ensure you have appropriate database permissions
5. Try restoring from backup and running again

---

## 📝 Script Features

### Safety Features
- ✅ **Idempotent** - Safe to run multiple times
- ✅ **Transaction-based** - All-or-nothing execution
- ✅ **Existence checks** - Skips existing objects
- ✅ **Error handling** - Automatic rollback on errors
- ✅ **Detailed logging** - Clear progress messages
- ✅ **Admin protection** - Preserves admin accounts in wipe script

### What Makes These Scripts Safe
1. **Pre-execution checks** - Verifies objects don't exist before creating
2. **Transaction wrapper** - Can be rolled back if needed
3. **Foreign key handling** - Properly ordered to avoid constraint violations
4. **Index creation** - Optimizes query performance
5. **Default values** - Ensures data consistency

---

## 🏆 Success Indicators

You'll know everything is working when:

✅ No more "Invalid object name" errors  
✅ No more "Pending model changes" warnings  
✅ Application starts without EF Core errors  
✅ All CRUD operations work correctly  
✅ Remittance features work (if implemented)  
✅ Announcement dismissal works  

---

**Last Updated:** 2026-02-03  
**Script Version:** 1.0  
**Tested On:** SQL Server (compatible with PortaliBITS database)

---

## 📜 License & Credits

These migration scripts were created to resolve pending migrations in the iBITS Portal project.

**IMPORTANT:** Always backup your database before running any migration scripts!

---

*End of Documentation*
