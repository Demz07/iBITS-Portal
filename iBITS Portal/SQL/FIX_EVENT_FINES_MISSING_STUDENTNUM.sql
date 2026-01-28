-- ============================================================
-- FIX EVENT FINES MISSING STUDENTNUM
-- ============================================================
-- This script updates existing event fines that have AttendanceId
-- but are missing StudentNum, which prevents them from showing
-- in the student's Financial view.
-- ============================================================

-- Step 1: Check how many fines need to be fixed
PRINT '===== EVENT FINES MISSING STUDENTNUM ====='
SELECT 
    f.FineId,
    f.AttendanceId,
    a.StudentNum AS [Should Be],
    f.Amount,
    f.FinesStatus,
    e.EventName,
    s.FirstName + ' ' + s.LastName AS StudentName
FROM Fines f
INNER JOIN Attendance a ON f.AttendanceId = a.AttendanceId
INNER JOIN Events e ON a.EventId = e.EventId
INNER JOIN Students s ON a.StudentNum = s.StudentNum
WHERE f.AttendanceId IS NOT NULL 
  AND f.StudentNum IS NULL
ORDER BY f.FineId;

PRINT ''
PRINT '===== TOTAL FINES TO BE FIXED ====='
SELECT COUNT(*) AS [Count]
FROM Fines
WHERE AttendanceId IS NOT NULL 
  AND StudentNum IS NULL;

-- ============================================================
-- UNCOMMENT THE SECTION BELOW TO FIX THE FINES
-- ============================================================

/*
PRINT ''
PRINT '===== FIXING EVENT FINES ====='

-- Update all event fines to have the correct StudentNum
UPDATE Fines
SET StudentNum = (
    SELECT StudentNum 
    FROM Attendance 
    WHERE Attendance.AttendanceId = Fines.AttendanceId
)
WHERE AttendanceId IS NOT NULL 
  AND StudentNum IS NULL;

PRINT 'Event fines updated successfully!';

-- Verify the fix
PRINT ''
PRINT '===== VERIFICATION - REMAINING ISSUES ====='
SELECT COUNT(*) AS [Remaining Issues]
FROM Fines
WHERE AttendanceId IS NOT NULL 
  AND StudentNum IS NULL;

PRINT ''
PRINT '===== FIXED FINES SAMPLE ====='
SELECT TOP 10
    f.FineId,
    f.AttendanceId,
    f.StudentNum,
    f.Amount,
    f.FinesStatus,
    e.EventName,
    s.FirstName + ' ' + s.LastName AS StudentName
FROM Fines f
INNER JOIN Attendance a ON f.AttendanceId = a.AttendanceId
INNER JOIN Events e ON a.EventId = e.EventId
INNER JOIN Students s ON f.StudentNum = s.StudentNum
WHERE f.AttendanceId IS NOT NULL
ORDER BY f.FineId DESC;

PRINT ''
PRINT '===== FIX COMPLETE ====='
*/

-- ============================================================
-- TO USE THIS SCRIPT:
-- 1. Review the SELECT results to see which fines will be fixed
-- 2. Uncomment the UPDATE section (remove /* and */)
-- 3. Run the script to fix the fines
-- 4. Verify students can now see their event fines in Financial view
-- ============================================================
