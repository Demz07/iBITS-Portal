-- Check the RemittanceType column data type and current values
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Remittances' AND COLUMN_NAME = 'RemittanceType';

-- Check existing values in the RemittanceType column
SELECT DISTINCT RemittanceType, COUNT(*) as Count
FROM [Remittances]
GROUP BY RemittanceType;
