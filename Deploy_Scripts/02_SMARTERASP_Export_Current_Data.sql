-- =============================================
-- iBITS Portal Data Export Script
-- This script exports your current data for import to SmarterASP.NET
-- Run this on your LOCAL database: DESKTOP-SG3AI25\SQLEXPRESS
-- =============================================

USE [PortaliBITS]
GO

-- Set output format
SET NOCOUNT ON
GO

PRINT '========================================='
PRINT 'EXPORTING CURRENT DATABASE DATA'
PRINT 'Database: PortaliBITS'
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================='
PRINT ''

-- Export current record counts
PRINT 'RECORD COUNTS:'
PRINT '-----------------------------------------'
SELECT 'Officers' AS TableName, COUNT(*) AS RecordCount FROM Officers
UNION ALL SELECT 'Student', COUNT(*) FROM Student
UNION ALL SELECT 'Event', COUNT(*) FROM Event
UNION ALL SELECT 'Attendance', COUNT(*) FROM Attendance
UNION ALL SELECT 'Fees', COUNT(*) FROM Fees
UNION ALL SELECT 'Fines', COUNT(*) FROM Fines
UNION ALL SELECT 'Remittances', COUNT(*) FROM Remittances
UNION ALL SELECT 'RemittanceItems', COUNT(*) FROM RemittanceItems
UNION ALL SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions
UNION ALL SELECT 'FinePaymentTransactions', COUNT(*) FROM FinePaymentTransactions
UNION ALL SELECT 'Announcements', COUNT(*) FROM Announcements
UNION ALL SELECT 'ActivityLogs', COUNT(*) FROM ActivityLogs
UNION ALL SELECT 'AspNetUsers', COUNT(*) FROM AspNetUsers
UNION ALL SELECT 'AspNetRoles', COUNT(*) FROM AspNetRoles
ORDER BY TableName
GO

PRINT ''
PRINT 'GENERATING INSERT STATEMENTS...'
PRINT ''

-- NOTE: The actual INSERT statements should be generated using SSMS
-- Tools -> Options -> SQL Server Object Explorer -> Scripting
-- Or use the export wizard to generate the data scripts

PRINT 'To export data:'
PRINT '1. In SSMS, right-click PortaliBITS database'
PRINT '2. Tasks -> Generate Scripts'
PRINT '3. Choose "Select specific database objects"'
PRINT '4. Select all tables'
PRINT '5. Advanced -> Types of data to script: Schema and data'
PRINT '6. Save to file: PortaliBITS_Data_Export.sql'
PRINT ''
PRINT 'OR use the provided script: 03_SMARTERASP_Sample_Data_Import.sql'
GO
