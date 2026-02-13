# 🔄 Reset Fees and Fines - User Guide

**Purpose:** Delete all fees and fines records and reset ID counters back to 1

---

## ⚠️ **IMPORTANT WARNINGS**

- ❗ **This will PERMANENTLY DELETE all payment data**
- ❗ **This includes ALL fees, fines, remittances, and payment transactions**
- ❗ **There is NO UNDO - make a backup first!**
- ❗ **Only use this in DEVELOPMENT/TESTING environments**

---

## 📋 **What Gets Deleted**

| Table | What Gets Deleted |
|-------|-------------------|
| **Fees** | All organization and class fees |
| **Fines** | All student fines |
| **PaymentTransactions** | All fee payment records |
| **FinePaymentTransactions** | All fine payment records |
| **Remittances** | All remittance batches |
| **RemittanceItems** | All remittance items |
| **Notifications** | Payment/Remittance/Fine notifications only |

---

## 🎯 **What DOES NOT Get Deleted**

✅ **Student Records** - All student data remains  
✅ **Attendance Records** - All attendance data remains  
✅ **Events** - All event data remains  
✅ **User Accounts** - All login accounts remain  
✅ **Other Notifications** - Non-payment notifications remain  

---

## 🚀 **How to Use**

### **Option 1: Detailed Script (Recommended)**

**File:** `RESET_FEES_FINES_TO_START.sql`

**Features:**
- ✅ Shows before/after counts
- ✅ Step-by-step progress messages
- ✅ Automatic transaction rollback on error
- ✅ Verification checks
- ✅ Safe error handling

**Steps:**
1. Open SQL Server Management Studio (SSMS)
2. Connect to your database server
3. Open `RESET_FEES_FINES_TO_START.sql`
4. Review the script carefully
5. Click **Execute** (F5)
6. Review the output messages
7. Verify all counts are 0

---

### **Option 2: Simple Script (Quick)**

**File:** `RESET_FEES_FINES_SIMPLE.sql`

**Features:**
- ✅ Fast execution
- ✅ Minimal output
- ✅ Single transaction

**Steps:**
1. Open SQL Server Management Studio (SSMS)
2. Connect to your database server
3. Open `RESET_FEES_FINES_SIMPLE.sql`
4. Click **Execute** (F5)
5. Check verification output

---

## 📊 **Sample Output (Detailed Script)**

```
============================================================
CURRENT RECORD COUNTS (Before Deletion)
============================================================
TableName              RecordCount    MaxID
Fees                   150            150
Fines                  45             45
PaymentTransactions    89             89
...

============================================================
STARTING DELETION PROCESS...
============================================================
Step 1: Deleting PaymentTransactions...
PaymentTransactions deleted: 89

Step 2: Deleting FinePaymentTransactions...
FinePaymentTransactions deleted: 23

...

============================================================
RESETTING IDENTITY SEEDS TO 1...
============================================================
Fees identity seed reset to 1
Fines identity seed reset to 1
...

============================================================
VERIFICATION - RECORD COUNTS (After Deletion)
============================================================
Fees remaining: 0
Fines remaining: 0
PaymentTransactions remaining: 0
...

✅ SUCCESS: All fees and fines data has been deleted!
✅ Identity seeds reset to 1
✅ Next Fee ID will be: 1
✅ Next Fine ID will be: 1

============================================================
🎉 RESET COMPLETE - READY FOR FRESH START!
============================================================
```

---

## 🔍 **Verification**

After running the script, verify with these queries:

```sql
-- Check all tables are empty
SELECT 'Fees' AS TableName, COUNT(*) AS Count FROM Fees
UNION ALL
SELECT 'Fines', COUNT(*) FROM Fines
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions
UNION ALL
SELECT 'FinePaymentTransactions', COUNT(*) FROM FinePaymentTransactions
UNION ALL
SELECT 'Remittances', COUNT(*) FROM Remittances
UNION ALL
SELECT 'RemittanceItems', COUNT(*) FROM RemittanceItems;

-- Check identity seeds
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    last_value AS CurrentSeed,
    last_value + increment_value AS NextID
FROM sys.identity_columns
WHERE OBJECT_NAME(object_id) IN ('Fees', 'Fines', 'PaymentTransactions', 
                                  'FinePaymentTransactions', 'Remittances', 'RemittanceItems');
```

**Expected Result:**
- All counts should be **0**
- All NextID values should be **1**

---

## 🧪 **Testing After Reset**

After running the reset script, test the following:

### **Test 1: Create a New Fee**
1. Login as **Org Treasurer**
2. Create a new fee
3. Verify the **FeeId = 1**

### **Test 2: Create a New Fine**
1. Login as **Org Treasurer**
2. Create a new fine
3. Verify the **FineId = 1**

### **Test 3: Mark Payment**
1. Mark a fee as paid
2. Verify the **PaymentTransactionId = 1**

### **Test 4: Create Remittance**
1. Login as **Class Treasurer**
2. Mark a fee as paid
3. Create remittance batch
4. Verify **RemittanceId = 1**

---

## 🔙 **Backup Before Reset (Recommended)**

Before running the reset script, create a backup:

```sql
-- Backup Fees table
SELECT * INTO Fees_Backup_20260212 FROM Fees;

-- Backup Fines table
SELECT * INTO Fines_Backup_20260212 FROM Fines;

-- Backup PaymentTransactions table
SELECT * INTO PaymentTransactions_Backup_20260212 FROM PaymentTransactions;

-- Backup FinePaymentTransactions table
SELECT * INTO FinePaymentTransactions_Backup_20260212 FROM FinePaymentTransactions;

-- Backup Remittances table
SELECT * INTO Remittances_Backup_20260212 FROM Remittances;

-- Backup RemittanceItems table
SELECT * INTO RemittanceItems_Backup_20260212 FROM RemittanceItems;
```

**To Restore from Backup:**

```sql
-- Restore Fees
INSERT INTO Fees SELECT * FROM Fees_Backup_20260212;

-- Restore Fines
INSERT INTO Fines SELECT * FROM Fines_Backup_20260212;

-- ... (repeat for all tables)
```

---

## ❌ **Troubleshooting**

### **Error: Foreign Key Constraint**

**Problem:** Cannot delete because of foreign key references

**Solution:** The script deletes in the correct order. If you still get this error:

1. Check if there are other tables referencing Fees/Fines
2. Delete those records first
3. Re-run the script

---

### **Error: Transaction Deadlock**

**Problem:** Another process is using the tables

**Solution:**

1. Close all applications using the database
2. Stop the iBITS Portal application
3. Re-run the script

---

### **Identity Seed Not Reset**

**Problem:** Next ID is not 1 after reset

**Solution:** Manually reseed:

```sql
DBCC CHECKIDENT ('Fees', RESEED, 0);
DBCC CHECKIDENT ('Fines', RESEED, 0);
```

---

## 📝 **Use Cases**

**When to use this script:**

✅ **Testing new features** - Need clean slate for testing  
✅ **Development environment** - Reset after testing  
✅ **Demo preparation** - Start fresh for demo  
✅ **Training environment** - Reset for new training session  
✅ **Data cleanup** - Remove test data before production  

**When NOT to use:**

❌ **Production environment** - NEVER use in production!  
❌ **Live data** - Don't use if data is needed  
❌ **Without backup** - Always backup first  

---

## 🎉 **After Reset - Fresh Start**

After successfully running the reset:

✅ **All fees and fines deleted**  
✅ **Identity seeds reset to 1**  
✅ **Next FeeId will be 1**  
✅ **Next FineId will be 1**  
✅ **Ready for testing Option B workflow!**  

---

## 📞 **Questions?**

If you encounter any issues:

1. Check the error message in SSMS
2. Review the troubleshooting section
3. Verify all tables are empty
4. Check identity seeds

---

**Created:** February 12, 2026  
**Version:** 1.0  
**Purpose:** Testing Option B Grace Period Implementation
