-- Check if there are records with NULL RemittanceStatus
SELECT 'Fees with NULL RemittanceStatus' AS TableInfo, COUNT(*) AS RecordCount 
FROM Fees WHERE RemittanceStatus IS NULL;

SELECT 'Fines with NULL RemittanceStatus' AS TableInfo, COUNT(*) AS RecordCount 
FROM Fines WHERE RemittanceStatus IS NULL;

-- Update any NULL values to 'NotRemitted'
UPDATE Fees SET RemittanceStatus = 'NotRemitted' WHERE RemittanceStatus IS NULL;
UPDATE Fines SET RemittanceStatus = 'NotRemitted' WHERE RemittanceStatus IS NULL;

PRINT 'Updated NULL RemittanceStatus values to NotRemitted';
