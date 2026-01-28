-- ============================================================
-- DELETE ALL USERS AND STUDENTS EXCEPT ADMIN
-- ============================================================
-- WARNING: This script will DELETE all users and students
-- except the admin account. Make sure you have a backup!
-- ============================================================

-- First, let's see what will be deleted (SAFETY CHECK)
PRINT '===== USERS TO BE DELETED ====='
SELECT 
    UserName, 
    Email, 
    EmailConfirmed
FROM AspNetUsers 
WHERE UserName != 'admin' 
  AND Email != 'admin@ibits.edu.ph'
ORDER BY UserName;

PRINT '===== STUDENTS TO BE DELETED ====='
SELECT 
    StudentNum, 
    FirstName, 
    LastName,
    Email
FROM Students 
WHERE StudentNum != 'admin'
  AND Email != 'admin@ibits.edu.ph'
ORDER BY StudentNum;

-- ============================================================
-- UNCOMMENT THE SECTIONS BELOW TO ACTUALLY DELETE
-- ============================================================

/*
-- Step 1: Delete user roles (except admin)
DELETE FROM AspNetUserRoles 
WHERE UserId IN (
    SELECT Id FROM AspNetUsers 
    WHERE UserName != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted user roles';

-- Step 2: Delete user claims (except admin)
DELETE FROM AspNetUserClaims 
WHERE UserId IN (
    SELECT Id FROM AspNetUsers 
    WHERE UserName != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted user claims';

-- Step 3: Delete user logins (except admin)
DELETE FROM AspNetUserLogins 
WHERE UserId IN (
    SELECT Id FROM AspNetUsers 
    WHERE UserName != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted user logins';

-- Step 4: Delete user tokens (except admin)
DELETE FROM AspNetUserTokens 
WHERE UserId IN (
    SELECT Id FROM AspNetUsers 
    WHERE UserName != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted user tokens';

-- Step 5: Delete related student data (fees, fines, attendance, etc.)
-- This depends on your foreign key constraints

-- Delete student fees
DELETE FROM Fees 
WHERE StudentNum IN (
    SELECT StudentNum FROM Students 
    WHERE StudentNum != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted student fees';

-- Delete student fines
DELETE FROM Fines 
WHERE StudentNum IN (
    SELECT StudentNum FROM Students 
    WHERE StudentNum != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted student fines';

-- Delete student attendance
DELETE FROM Attendance 
WHERE StudentNum IN (
    SELECT StudentNum FROM Students 
    WHERE StudentNum != 'admin' AND Email != 'admin@ibits.edu.ph'
);
PRINT 'Deleted student attendance';

-- Step 6: Delete students (except admin)
DELETE FROM Students 
WHERE StudentNum != 'admin' 
  AND Email != 'admin@ibits.edu.ph';
PRINT 'Deleted students';

-- Step 7: Delete AspNetUsers (except admin)
DELETE FROM AspNetUsers 
WHERE UserName != 'admin' 
  AND Email != 'admin@ibits.edu.ph';
PRINT 'Deleted users';

PRINT '===== DELETION COMPLETE =====';
PRINT 'Admin account preserved';
*/

-- ============================================================
-- TO USE THIS SCRIPT:
-- 1. Review the SELECT statements to verify what will be deleted
-- 2. Make a backup: BACKUP DATABASE [YourDB] TO DISK = 'C:\Backup\before_delete.bak'
-- 3. Uncomment the DELETE sections (remove /* and */)
-- 4. Run the script in your database
-- ============================================================
