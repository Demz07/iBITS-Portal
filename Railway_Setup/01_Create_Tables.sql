-- Railway PostgreSQL Database Setup
-- Run this in Railway's PostgreSQL Query console

-- Create AspNetUsers table (Identity)
CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "UserName" TEXT,
    "NormalizedUserName" TEXT,
    "Email" TEXT,
    "NormalizedEmail" TEXT,
    "EmailConfirmed" BOOLEAN NOT NULL,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL,
    "TwoFactorEnabled" BOOLEAN NOT NULL,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_NormalizedUserName" ON "AspNetUsers" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_NormalizedEmail" ON "AspNetUsers" ("NormalizedEmail");

-- Create AspNetRoles table
CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" TEXT,
    "NormalizedName" TEXT,
    "ConcurrencyStamp" TEXT
);

CREATE INDEX IF NOT EXISTS "IX_AspNetRoles_NormalizedName" ON "AspNetRoles" ("NormalizedName");

-- Create AspNetUserRoles table
CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

-- Create other required Identity tables
CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

-- Create Student table
CREATE TABLE IF NOT EXISTS "Student" (
    "StudentNum" TEXT NOT NULL PRIMARY KEY,
    "FirstName" TEXT NOT NULL,
    "MiddleInitial" TEXT,
    "LastName" TEXT NOT NULL,
    "Suffix" TEXT,
    "Gender" TEXT,
    "YearLevelSection" TEXT,
    "Program" TEXT,
    "ContactNumber" TEXT,
    "Email" TEXT,
    "StreetAddress" TEXT,
    "City" TEXT,
    "Province" TEXT,
    "ZipCode" TEXT,
    "EmergencyContactName" TEXT,
    "EmergencyContactNumber" TEXT,
    "RelationshipToStudent" TEXT,
    "QRCodePath" TEXT,
    "IsArchived" BOOLEAN NOT NULL DEFAULT FALSE,
    "ArchivedDate" TIMESTAMP WITH TIME ZONE,
    "ArchivedBy" TEXT,
    "ArchiveReason" TEXT
);

-- Create Event table
CREATE TABLE IF NOT EXISTS "Event" (
    "EventID" SERIAL PRIMARY KEY,
    "EventName" TEXT NOT NULL,
    "EventDescription" TEXT,
    "EventDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "EventTime" TEXT,
    "EventLocation" TEXT,
    "CreatedBy" TEXT,
    "CreatedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsClosed" BOOLEAN NOT NULL DEFAULT FALSE
);

-- Create Fee table
CREATE TABLE IF NOT EXISTS "Fee" (
    "FeeID" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "EventID" INTEGER,
    "FeeCategory" TEXT NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "IsPaid" BOOLEAN NOT NULL DEFAULT FALSE,
    "PaymentDate" TIMESTAMP WITH TIME ZONE,
    "PaymentMethod" TEXT,
    "Remarks" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE,
    FOREIGN KEY ("EventID") REFERENCES "Event" ("EventID") ON DELETE SET NULL
);

-- Create Fine table
CREATE TABLE IF NOT EXISTS "Fine" (
    "FineID" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "EventID" INTEGER,
    "FineCategory" TEXT NOT NULL,
    "Amount" DECIMAL(18,2) NOT NULL,
    "IsPaid" BOOLEAN NOT NULL DEFAULT FALSE,
    "PaymentDate" TIMESTAMP WITH TIME ZONE,
    "PaymentMethod" TEXT,
    "Remarks" TEXT,
    "BatchId" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE,
    FOREIGN KEY ("EventID") REFERENCES "Event" ("EventID") ON DELETE SET NULL
);

-- Create Attendance table
CREATE TABLE IF NOT EXISTS "Attendance" (
    "AttendanceID" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "EventID" INTEGER NOT NULL,
    "TimeIn" TIMESTAMP WITH TIME ZONE,
    "TimeOut" TIMESTAMP WITH TIME ZONE,
    "Remarks" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE,
    FOREIGN KEY ("EventID") REFERENCES "Event" ("EventID") ON DELETE CASCADE
);

-- Create Officer table
CREATE TABLE IF NOT EXISTS "Officer" (
    "OfficerID" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "Position" TEXT NOT NULL,
    "AcademicYear" TEXT,
    "Semester" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE
);

-- Create Announcement table
CREATE TABLE IF NOT EXISTS "Announcement" (
    "AnnouncementID" SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "CreatedBy" TEXT,
    "CreatedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiryDate" TIMESTAMP WITH TIME ZONE
);

-- Create ActivityLog table
CREATE TABLE IF NOT EXISTS "ActivityLog" (
    "LogID" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "Action" TEXT NOT NULL,
    "Details" TEXT,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create PendingRoleChange table
CREATE TABLE IF NOT EXISTS "PendingRoleChange" (
    "Id" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "RequestedRole" TEXT NOT NULL,
    "RequestDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsApproved" BOOLEAN,
    "ApprovedDate" TIMESTAMP WITH TIME ZONE,
    "ApprovedBy" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE
);

-- Create PaymentTransaction table
CREATE TABLE IF NOT EXISTS "PaymentTransaction" (
    "TransactionID" SERIAL PRIMARY KEY,
    "StudentNum" TEXT NOT NULL,
    "TransactionDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TotalAmount" DECIMAL(18,2) NOT NULL,
    "PaymentMethod" TEXT NOT NULL,
    "ProcessedBy" TEXT,
    FOREIGN KEY ("StudentNum") REFERENCES "Student" ("StudentNum") ON DELETE CASCADE
);

-- Create Remittance table
CREATE TABLE IF NOT EXISTS "Remittance" (
    "RemittanceID" SERIAL PRIMARY KEY,
    "RemittanceDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TotalAmount" DECIMAL(18,2) NOT NULL,
    "RemittanceType" TEXT NOT NULL,
    "CreatedBy" TEXT,
    "Status" TEXT NOT NULL DEFAULT 'Pending',
    "Program" TEXT
);

-- Create RemittanceItem table
CREATE TABLE IF NOT EXISTS "RemittanceItem" (
    "RemittanceItemID" SERIAL PRIMARY KEY,
    "RemittanceID" INTEGER NOT NULL,
    "FeeID" INTEGER,
    "FineID" INTEGER,
    "Amount" DECIMAL(18,2) NOT NULL,
    FOREIGN KEY ("RemittanceID") REFERENCES "Remittance" ("RemittanceID") ON DELETE CASCADE,
    FOREIGN KEY ("FeeID") REFERENCES "Fee" ("FeeID") ON DELETE SET NULL,
    FOREIGN KEY ("FineID") REFERENCES "Fine" ("FineID") ON DELETE SET NULL
);

-- Create SystemSetting table
CREATE TABLE IF NOT EXISTS "SystemSetting" (
    "SettingKey" TEXT NOT NULL PRIMARY KEY,
    "SettingValue" TEXT NOT NULL,
    "LastModified" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ModifiedBy" TEXT
);

-- Create UserAnnouncementDismissal table
CREATE TABLE IF NOT EXISTS "UserAnnouncementDismissal" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "AnnouncementId" INTEGER NOT NULL,
    "DismissedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("AnnouncementId") REFERENCES "Announcement" ("AnnouncementID") ON DELETE CASCADE
);

-- Insert default roles
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    ('1', 'Admin', 'ADMIN', CAST(gen_random_uuid() AS TEXT)),
    ('2', 'Class Treasurer', 'CLASS TREASURER', CAST(gen_random_uuid() AS TEXT)),
    ('3', 'Class Secretary', 'CLASS SECRETARY', CAST(gen_random_uuid() AS TEXT)),
    ('4', 'Org Treasurer', 'ORG TREASURER', CAST(gen_random_uuid() AS TEXT)),
    ('5', 'Org Secretary', 'ORG SECRETARY', CAST(gen_random_uuid() AS TEXT)),
    ('6', 'Member', 'MEMBER', CAST(gen_random_uuid() AS TEXT))
ON CONFLICT ("Id") DO NOTHING;

-- Success message
SELECT 'Database tables created successfully!' AS message;
