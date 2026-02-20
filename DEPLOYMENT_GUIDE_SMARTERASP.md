# 🚀 iBITS Portal - SmarterASP.NET Deployment Guide

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Project Information](#project-information)
3. [Database Deployment](#database-deployment)
4. [Application Configuration](#application-configuration)
5. [Publishing the Application](#publishing-the-application)
6. [Deployment Steps](#deployment-steps)
7. [Post-Deployment Verification](#post-deployment-verification)
8. [Troubleshooting](#troubleshooting)

---

## 1️⃣ Prerequisites

### SmarterASP.NET Hosting Requirements
- ✅ **Hosting Plan**: Windows hosting with ASP.NET support
- ✅ **SQL Server Database**: Available in your SmarterASP.NET plan
- ✅ **Required Features**:
  - ASP.NET Core 8.0 Runtime
  - SQL Server Database (MSSQL)
  - FTP/File Manager access
  - Database management tools (SmarterASP.NET Control Panel)

### Local Development Tools
- Visual Studio 2022 (or VS Code with .NET SDK)
- .NET 8.0 SDK
- SQL Server Management Studio (SSMS) or Azure Data Studio
- FTP Client (FileZilla recommended)

### SmarterASP.NET Account Information Needed
- Control Panel URL: https://member5-4.smarterasp.net
- Database Server Address
- Database Name
- Database Username
- Database Password
- FTP Credentials

---

## 2️⃣ Project Information

### Current Stack
- **Framework**: ASP.NET Core 8.0 (MVC + Razor Pages)
- **Database**: SQL Server (PortaliBITS)
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core 8.0.22

### Key Dependencies
```xml
- ClosedXML (0.105.0) - Excel export functionality
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.22)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.22)
```

### Database Tables (35 total)
- Student (132 records)
- Officers (5 records)
- Fees, Fines, Events, Attendance
- Remittances, RemittanceItems, PaymentTransactions
- AspNetUsers, AspNetRoles (Identity tables)
- ActivityLogs, Announcements, Notifications

---

## 3️⃣ Database Deployment

### Step 1: Export Database Schema and Data

#### Option A: Using SQL Server Management Studio (SSMS)
1. Connect to your local SQL Server: `DESKTOP-SG3AI25\SQLEXPRESS`
2. Right-click on `PortaliBITS` database
3. Tasks → Generate Scripts
4. Select "Script entire database and all database objects"
5. Advanced Options:
   - Types of data to script: **Schema and data**
   - Script for Server Version: **SQL Server 2019** (or compatible)
   - Include IF NOT EXISTS: **True**
6. Save to file: `PortaliBITS_Complete_Deployment.sql`

#### Option B: Using Provided Script (Already Available)
We can use: `source/repos/iBITS_PORTAL_DATABASE.sql`

### Step 2: Prepare Database for SmarterASP.NET

**Important Modifications Required:**
1. Remove `USE [PortaliBITS]` statement (SmarterASP.NET database name will be different)
2. Update database owner references
3. Ensure compatibility with shared hosting environment

---

## 4️⃣ Application Configuration

### Step 1: Create Production appsettings.json

Create: `appsettings.Production.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SMARTERASP_SQL_SERVER;Database=YOUR_DATABASE_NAME;User Id=YOUR_DB_USERNAME;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 2: Update web.config for Hosting

Create/Update: `web.config` (for IIS deployment)
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\iBITS Portal.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

---

## 5️⃣ Publishing the Application

### Using Visual Studio

1. **Open the solution**: `iBITS Portal.sln`

2. **Right-click** on the project → **Publish**

3. **Choose Target**: Folder

4. **Publish Settings**:
   - Configuration: **Release**
   - Target Framework: **net8.0**
   - Deployment Mode: **Framework-dependent**
   - Target Runtime: **Portable**
   - File Publish Options:
     - ✅ Delete existing files
     - ✅ Exclude files from App_Data folder

5. **Folder Location**: `bin\Release\net8.0\publish`

6. Click **Publish**

### Using Command Line

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
dotnet publish -c Release -o ./publish
```

---

## 6️⃣ Deployment Steps

### A. Database Deployment

1. **Login to SmarterASP.NET Control Panel**
   - URL: https://member5-4.smarterasp.net
   - Enter your credentials

2. **Create New Database**
   - Go to: **Database Manager** → **MS SQL**
   - Click: **Create New Database**
   - Note down:
     - Database Server Address
     - Database Name
     - Username
     - Password

3. **Import Database Schema**
   - Use **myLittleAdmin** (built-in SQL tool)
   - Or use SQL Server Management Studio (SSMS) with remote connection
   - Run the deployment script: `PortaliBITS_Complete_Deployment.sql`

4. **Verify Database**
   - Check all 35 tables are created
   - Verify data is imported (132 students, etc.)

### B. Application Deployment

1. **Update Connection String**
   - Edit `appsettings.Production.json` in the publish folder
   - Replace with SmarterASP.NET database credentials

2. **Upload Files via FTP**
   - FTP Host: (provided by SmarterASP.NET)
   - Username: (your account username)
   - Password: (your account password)
   - Port: 21 (FTP) or 990 (FTPS)

3. **Directory Structure on Server**
   ```
   /wwwroot/
   ├── iBITS Portal.dll
   ├── appsettings.json
   ├── appsettings.Production.json
   ├── web.config
   ├── wwwroot/
   │   ├── css/
   │   ├── js/
   │   ├── images/
   │   ├── lib/
   │   └── sounds/
   ├── Views/
   ├── Areas/
   └── [other files]
   ```

4. **Set Application Pool**
   - In SmarterASP.NET Control Panel
   - Go to: **IIS Manager**
   - Set: **.NET CLR Version** → **No Managed Code** (for .NET Core)
   - Pipeline Mode: **Integrated**

5. **Set Environment Variable**
   - Set `ASPNETCORE_ENVIRONMENT` = `Production`

### C. File Permissions

Ensure the following folders are writable:
- `/wwwroot/uploads/`
- `/wwwroot/uploads/profiles/`
- `/wwwroot/temp_uploads/`
- `/logs/` (create if doesn't exist)

---

## 7️⃣ Post-Deployment Verification

### Checklist

- [ ] Website loads without errors
- [ ] Database connection successful
- [ ] Login page accessible
- [ ] Can login with existing credentials
- [ ] Static files loading (CSS, JS, images)
- [ ] File uploads working
- [ ] Student records visible
- [ ] Fees/Fines management functional
- [ ] Remittance system working
- [ ] Reports/Excel exports working

### Test Credentials
Use existing admin account from database

### Common URLs to Test
- Home: `https://yourdomain.com/`
- Login: `https://yourdomain.com/Identity/Account/Login`
- Admin Dashboard: `https://yourdomain.com/Admin`
- Student Records: `https://yourdomain.com/Student`

---

## 8️⃣ Troubleshooting

### Issue: 500 Internal Server Error
**Solution**:
- Enable detailed errors in `web.config`:
  ```xml
  <aspNetCore ... stdoutLogEnabled="true" />
  ```
- Check logs in `/logs/stdout` folder

### Issue: Database Connection Failed
**Solution**:
- Verify connection string in `appsettings.Production.json`
- Check database credentials in SmarterASP.NET control panel
- Ensure SQL Server allows remote connections
- Test connection using SQL Server Management Studio

### Issue: Static Files Not Loading (CSS/JS)
**Solution**:
- Verify `wwwroot` folder uploaded correctly
- Check IIS static content settings
- Clear browser cache

### Issue: Identity/Login Not Working
**Solution**:
- Verify `AspNetUsers` table exists and has data
- Check `AspNetRoles` table
- Ensure Entity Framework migrations applied

### Issue: File Upload Errors
**Solution**:
- Check folder permissions (must be writable)
- Verify max upload size in `web.config`
- Check available disk space

---

## 📞 Support Resources

### SmarterASP.NET Support
- Knowledge Base: https://www.smarterasp.net/support
- Support Ticket: https://member5-4.smarterasp.net/support
- Live Chat: Available in control panel

### Additional Help
- ASP.NET Core Deployment: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/
- Entity Framework Core: https://docs.microsoft.com/en-us/ef/core/

---

## 🎯 Quick Deployment Checklist

- [ ] Export database with schema and data
- [ ] Create production appsettings.json
- [ ] Publish application in Release mode
- [ ] Create database in SmarterASP.NET
- [ ] Import database schema and data
- [ ] Update connection string
- [ ] Upload files via FTP
- [ ] Configure IIS settings
- [ ] Test website functionality
- [ ] Verify all features working

---

**Deployment Date**: _________________
**Deployed By**: _________________
**Production URL**: _________________
**Notes**: _________________

