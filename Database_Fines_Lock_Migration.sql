-- Add payment lock columns to Fines table
USE [PortaliBITS]
GO

PRINT 'Adding payment lock columns to Fines table...'

-- Add IsPaymentLocked
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fines') AND name = 'IsPaymentLocked')
BEGIN
    ALTER TABLE Fines ADD IsPaymentLocked BIT NOT NULL DEFAULT 0
    PRINT '? Added IsPaymentLocked column'
END

-- Add PaymentLockedDate
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fines') AND name = 'PaymentLockedDate')
BEGIN
    ALTER TABLE Fines ADD PaymentLockedDate DATETIME2 NULL
    PRINT '? Added PaymentLockedDate column'
END

-- Add LockedBy
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fines') AND name = 'LockedBy')
BEGIN
    ALTER TABLE Fines ADD LockedBy NVARCHAR(450) NULL
    PRINT '? Added LockedBy column'
END

-- Add foreign key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fines_LockedBy_Student')
BEGIN
    ALTER TABLE Fines ADD CONSTRAINT FK_Fines_LockedBy_Student FOREIGN KEY (LockedBy) REFERENCES Student(StudentNum)
    PRINT '? Added FK_Fines_LockedBy_Student'
END

PRINT 'Fines table update complete!'
GO
