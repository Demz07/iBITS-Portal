# 🚀 Quick Start Deployment Guide - SmarterASP.NET

## 📌 TL;DR - Fast Deployment Steps

### Prerequisites Checklist
- ✅ SmarterASP.NET account active: https://member5-4.smarterasp.net
- ✅ SQL Server database available in your hosting plan
- ✅ Visual Studio 2022 installed (or .NET 8.0 SDK)
- ✅ FTP client (FileZilla recommended)

---

## 🎯 Step-by-Step Deployment (30-60 minutes)

### STEP 1: Get SmarterASP.NET Credentials (5 mins)

1. Login to: https://member5-4.smarterasp.net
2. Write down these details:

```
FTP Credentials:
- Host: _________________
- Username: _________________
- Password: _________________
- Port: 21 (or 990 for FTPS)

Database Credentials:
- Server: _________________
- Database Name: _________________
- DB Username: _________________
- DB Password: _________________
```

### STEP 2: Create Database (5 mins)

1. In SmarterASP.NET Control Panel → **Database Manager** → **MS SQL**
2. Click **"Create New Database"**
3. Note the credentials provided
4. Open **myLittleAdmin** (database management tool)

### STEP 3: Deploy Database Schema (10 mins)

1. Open the database deployment script:
   - File: `Deploy_Scripts/01_SMARTERASP_Database_Deployment.sql`

2. In **myLittleAdmin** or connect via **SSMS**:
   - Server: [Your SmarterASP.NET SQL Server]
   - Database: [Your database name]
   - Login: [Your DB username/password]

3. **Execute the script** → This creates all 35+ tables

4. **Verify**: Check that tables are created (Student, Fees, Fines, etc.)

### STEP 4: Export & Import Your Data (15 mins)

#### Option A: Using SQL Server Management Studio (SSMS)

**Export from Local:**
1. Connect to: `DESKTOP-SG3AI25\SQLEXPRESS`
2. Right-click database `PortaliBITS`
3. Tasks → **Generate Scripts**
4. Choose: "Select specific database objects"
5. Select ALL tables (Student, Fees, Fines, Officers, etc.)
6. Click **Advanced** → Set "Types of data to script" to **"Schema and data"**
7. Save to file: `PortaliBITS_Data_Export.sql`

**Import to SmarterASP.NET:**
1. Open `PortaliBITS_Data_Export.sql`
2. Remove the first line: `USE [PortaliBITS]` (if present)
3. Execute this script in myLittleAdmin or SSMS (connected to SmarterASP.NET)
4. Wait for completion (may take 5-10 minutes)

#### Option B: Using Import/Export Wizard (Easier)

1. In SSMS, right-click `PortaliBITS` database
2. Tasks → **Export Data**
3. Source: SQL Server (local)
4. Destination: SQL Server (SmarterASP.NET - enter credentials)
5. Select **"Copy data from one or more tables"**
6. Select ALL tables
7. Click **Finish** → Wait for completion

### STEP 5: Update Connection String (2 mins)

1. Open file: `iBITS Portal/appsettings.Production.json`

2. Replace placeholders with YOUR SmarterASP.NET credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER_HERE;Database=YOUR_DB_NAME_HERE;User Id=YOUR_DB_USERNAME_HERE;Password=YOUR_DB_PASSWORD_HERE;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  }
}
```

**Example:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql2019-001.smarterasp.net;Database=DB_123456_ibits;User Id=DB_123456_ibits_admin;Password=MySecurePass123!;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
  }
}
```

### STEP 6: Publish Application (5 mins)

#### Using Visual Studio:

1. Open `iBITS Portal.sln` in Visual Studio
2. Right-click on **iBITS Portal** project → **Publish**
3. Click **"Add a publish profile"** (if first time)
4. Choose **Folder**
5. Location: `bin\Release\net8.0\publish`
6. Click **Publish** button
7. Wait for build to complete

#### Using Command Line:

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
dotnet publish -c Release -o ./publish
```

### STEP 7: Upload Files via FTP (15-30 mins)

#### Using FileZilla (Recommended):

1. **Download FileZilla**: https://filezilla-project.org/

2. **Connect to SmarterASP.NET:**
   - Host: [Your FTP host]
   - Username: [Your username]
   - Password: [Your password]
   - Port: 21 (or 990 for FTPS)
   - Click **Quickconnect**

3. **Navigate:**
   - Left panel: Go to `C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal\bin\Release\net8.0\publish`
   - Right panel: Go to `/wwwroot/` (your website root folder)

4. **Upload:**
   - Select ALL files and folders from left panel
   - Right-click → **Upload**
   - Wait for upload to complete (10-30 minutes depending on connection)

5. **Verify Upload:**
   - Check that `web.config` is uploaded
   - Check that `wwwroot` folder exists with css, js, images
   - Check that `appsettings.Production.json` is uploaded

### STEP 8: Configure IIS (5 mins)

1. In SmarterASP.NET Control Panel → **IIS Manager**

2. **Application Pool Settings:**
   - .NET CLR Version: **No Managed Code** (Important for .NET Core!)
   - Pipeline Mode: **Integrated**

3. **Set Environment Variable:**
   - Go to: Configuration → Environment Variables
   - Add new variable:
     - Name: `ASPNETCORE_ENVIRONMENT`
     - Value: `Production`

4. **Restart Application:**
   - In Control Panel → Restart your site

### STEP 9: Set Folder Permissions (3 mins)

1. In Control Panel → **File Manager**

2. Create and set writable permissions for these folders:
   - `/wwwroot/uploads/` (set to writable)
   - `/wwwroot/uploads/profiles/` (set to writable)
   - `/wwwroot/temp_uploads/` (set to writable)
   - `/logs/` (create folder and set to writable)

### STEP 10: Test Your Website! (5 mins)

1. **Visit your website:** https://yourdomain.com

2. **Test checklist:**
   - ✅ Homepage loads
   - ✅ CSS/Images load correctly
   - ✅ Login page accessible
   - ✅ Can login with existing credentials
   - ✅ Student records page works
   - ✅ Fees/Fines management works
   - ✅ File upload works

3. **If you see errors:**
   - Check `web.config` has `stdoutLogEnabled="true"`
   - Check logs in `/logs/stdout*.log`
   - Review troubleshooting section in `DEPLOYMENT_GUIDE_SMARTERASP.md`

---

## 🎉 Success Indicators

You'll know deployment is successful when:

1. ✅ Website loads without 500 errors
2. ✅ Login page appears correctly
3. ✅ You can login with existing admin account
4. ✅ Student records are visible and correct (132 students)
5. ✅ Dashboard shows correct data
6. ✅ All features work (fees, fines, events, reports)

---

## ⚠️ Common Issues & Quick Fixes

### Issue 1: "500 Internal Server Error"
**Quick Fix:**
1. Enable logging in `web.config`
2. Check `/logs/` folder for error details
3. Most common: Wrong connection string

### Issue 2: "Database connection failed"
**Quick Fix:**
1. Double-check connection string in `appsettings.Production.json`
2. Test database connection using SSMS
3. Verify database credentials in Control Panel

### Issue 3: "CSS/JS not loading"
**Quick Fix:**
1. Verify `wwwroot` folder uploaded completely
2. Clear browser cache (Ctrl+F5)
3. Check browser console for 404 errors

### Issue 4: "Login doesn't work"
**Quick Fix:**
1. Verify `AspNetUsers` table has data
2. Check `AspNetRoles` table exists
3. Verify Identity tables are populated

---

## 📞 Need Help?

### SmarterASP.NET Support:
- Control Panel → **Support** → Create ticket
- Live Chat (available in control panel)
- Email: support@smarterasp.net
- Knowledge Base: https://www.smarterasp.net/support

### Detailed Documentation:
- See: `DEPLOYMENT_GUIDE_SMARTERASP.md` for comprehensive guide
- See: `DEPLOYMENT_CHECKLIST.md` for step-by-step checklist

---

## 📊 Post-Deployment Tasks

After successful deployment:

1. **Security:**
   - [ ] Change default admin password
   - [ ] Enable HTTPS/SSL (in SmarterASP.NET control panel)
   - [ ] Review user accounts and permissions

2. **Backup:**
   - [ ] Set up automatic database backups
   - [ ] Export database backup weekly
   - [ ] Keep local copy of published files

3. **Monitoring:**
   - [ ] Monitor error logs regularly
   - [ ] Check application performance
   - [ ] Review activity logs

4. **Documentation:**
   - [ ] Document your production URL
   - [ ] Save database credentials securely
   - [ ] Keep deployment notes

---

## 🎯 Estimated Time Breakdown

| Task | Time |
|------|------|
| Get credentials & setup | 5 mins |
| Create database | 5 mins |
| Deploy schema | 10 mins |
| Export/Import data | 15 mins |
| Update config files | 2 mins |
| Publish application | 5 mins |
| Upload via FTP | 15-30 mins |
| Configure IIS | 5 mins |
| Set permissions | 3 mins |
| Testing | 5 mins |
| **TOTAL** | **60-75 mins** |

---

**Good luck with your deployment! 🚀**

If you encounter any issues, refer to the detailed troubleshooting section in `DEPLOYMENT_GUIDE_SMARTERASP.md`.
