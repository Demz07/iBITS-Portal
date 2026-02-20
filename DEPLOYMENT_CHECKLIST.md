# 📋 iBITS Portal - SmarterASP.NET Deployment Checklist

## Pre-Deployment Preparation

### ✅ Phase 1: Gather Information
- [ ] Login to SmarterASP.NET Control Panel: https://member5-4.smarterasp.net
- [ ] Note your domain/subdomain name: _________________
- [ ] Get FTP credentials:
  - FTP Host: _________________
  - Username: _________________
  - Password: _________________
  - Port: 21 (FTP) or 990 (FTPS)

### ✅ Phase 2: Database Setup
- [ ] Create new SQL Server database in Control Panel
- [ ] Note database credentials:
  - Server: _________________
  - Database Name: _________________
  - Username: _________________
  - Password: _________________
- [ ] Test database connection using myLittleAdmin or SSMS

### ✅ Phase 3: Prepare Application
- [ ] Update `appsettings.Production.json` with SmarterASP.NET database credentials
- [ ] Verify `web.config` exists in project
- [ ] Check all NuGet packages are restored
- [ ] Build project in Release mode (to test for errors)

---

## Deployment Steps

### 📊 Database Deployment

- [ ] **Step 1**: Connect to SmarterASP.NET database
  - Use myLittleAdmin (in Control Panel)
  - OR use SSMS with remote connection

- [ ] **Step 2**: Run schema script
  - Execute: `Deploy_Scripts/01_SMARTERASP_Database_Deployment.sql`
  - Verify: All 35+ tables created successfully
  - Check: No errors in execution

- [ ] **Step 3**: Import data (choose one method)
  
  **Method A: Using SSMS (Recommended)**
  1. Generate data script from local database:
     - Right-click `PortaliBITS` → Tasks → Generate Scripts
     - Select "Schema and data"
     - Save as `Data_Export.sql`
  2. Execute script on SmarterASP.NET database
  
  **Method B: Using Import/Export Wizard**
  1. In SSMS, connect to local database
  2. Right-click database → Tasks → Export Data
  3. Source: Local SQL Server
  4. Destination: SmarterASP.NET SQL Server
  5. Copy all tables with data

- [ ] **Step 4**: Verify data imported
  - Check Student count: Should be 132
  - Check Officers count: Should be 5
  - Check AspNetUsers exists with data
  - Check AspNetRoles exists with data

- [ ] **Step 5**: Run EF Migrations (if needed)
  ```powershell
  # In Package Manager Console
  Update-Database -Context ApplicationDbContext
  ```

### 🚀 Application Deployment

- [ ] **Step 6**: Publish Application
  
  **Using Visual Studio:**
  1. Right-click project → Publish
  2. Select publish profile: `SmarterASP-Deploy`
  3. Click "Publish"
  4. Wait for build to complete
  5. Find output in: `bin\Release\net8.0\publish`
  
  **Using Command Line:**
  ```powershell
  cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"
  dotnet publish -c Release -o ./publish
  ```

- [ ] **Step 7**: Verify publish output
  - Check `publish` folder contains:
    - iBITS Portal.dll
    - appsettings.json
    - appsettings.Production.json
    - web.config
    - wwwroot folder (with all assets)
    - Views folder
    - All dependencies

- [ ] **Step 8**: Update production connection string
  - Edit `publish/appsettings.Production.json`
  - Replace placeholders with actual SmarterASP.NET credentials
  - Save file

- [ ] **Step 9**: Upload files via FTP
  
  **Using FileZilla:**
  1. Host: (Your FTP host)
  2. Username: (Your username)
  3. Password: (Your password)
  4. Port: 21 or 990
  5. Connect
  6. Navigate to: `/wwwroot/` (or your site root)
  7. Upload ALL files from `publish` folder
  8. Wait for upload to complete (may take 10-30 minutes)

- [ ] **Step 10**: Set folder permissions
  - In Control Panel → File Manager
  - Set writable permissions for:
    - `/wwwroot/uploads/`
    - `/wwwroot/uploads/profiles/`
    - `/wwwroot/temp_uploads/`
    - Create `/logs/` folder and set writable

- [ ] **Step 11**: Configure IIS Settings (in Control Panel)
  - Go to: IIS Manager
  - Application Pool Settings:
    - .NET CLR Version: **No Managed Code**
    - Pipeline Mode: **Integrated**
  - Set environment variable:
    - Name: `ASPNETCORE_ENVIRONMENT`
    - Value: `Production`

---

## Post-Deployment Testing

### 🧪 Verification Tests

- [ ] **Test 1**: Website loads
  - Visit: https://yourdomain.com
  - Expected: Landing page displays correctly
  - Check: No 500 errors

- [ ] **Test 2**: Static files load
  - Check: Images display
  - Check: CSS styles applied
  - Check: JavaScript working
  - Open browser console: No 404 errors

- [ ] **Test 3**: Database connection
  - Visit login page
  - Expected: Page loads without database errors
  - Check error logs if issues

- [ ] **Test 4**: Authentication system
  - Try to login with existing credentials
  - Expected: Login successful
  - Redirects to appropriate dashboard

- [ ] **Test 5**: Core features
  - [ ] Student records page loads
  - [ ] Fees management works
  - [ ] Fines management works
  - [ ] Events page accessible
  - [ ] Remittance system functional
  - [ ] Reports can be generated
  - [ ] Excel export works

- [ ] **Test 6**: File uploads
  - [ ] Upload student profile picture
  - [ ] Verify file saved to `/uploads/profiles/`
  - [ ] Image displays correctly

- [ ] **Test 7**: Admin features
  - [ ] Admin dashboard accessible
  - [ ] User management works
  - [ ] Role assignment functional
  - [ ] Activity logs recording

### 📝 Documentation

- [ ] Document production URL: _________________
- [ ] Document admin credentials (securely): _________________
- [ ] Save database backup location: _________________
- [ ] Record deployment date: _________________
- [ ] Note any issues encountered: _________________

---

## Troubleshooting Guide

### ❌ Issue: 500 Internal Server Error

**Solution:**
1. Enable detailed errors in `web.config`:
   ```xml
   <aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />
   ```
2. Check logs folder: `/logs/stdout*`
3. Review error messages
4. Common causes:
   - Missing connection string
   - Database connection failed
   - Missing dependencies

### ❌ Issue: Database Connection Failed

**Solution:**
1. Verify connection string in `appsettings.Production.json`
2. Test database connection using SSMS
3. Check firewall rules (SmarterASP.NET support)
4. Verify database credentials in Control Panel

### ❌ Issue: Static Files Not Loading (404)

**Solution:**
1. Verify `wwwroot` folder uploaded completely
2. Check file paths in browser (F12 → Network tab)
3. Ensure IIS static content module enabled
4. Clear browser cache

### ❌ Issue: Login/Identity Not Working

**Solution:**
1. Verify `AspNetUsers` table has data
2. Check `AspNetRoles` table exists
3. Run EF migrations if tables missing:
   ```powershell
   Update-Database -Context ApplicationDbContext
   ```
4. Check cookie settings in `Program.cs`

### ❌ Issue: File Upload Fails

**Solution:**
1. Check folder permissions (must be writable)
2. Verify upload size limits in `web.config`
3. Check available disk space in Control Panel
4. Review upload path in code

---

## Support & Help

### 📞 SmarterASP.NET Support
- Knowledge Base: https://www.smarterasp.net/support
- Support Ticket: https://member5-4.smarterasp.net/support
- Live Chat: Available in control panel
- Email: support@smarterasp.net

### 📚 Additional Resources
- ASP.NET Core Deployment: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/
- Entity Framework: https://docs.microsoft.com/en-us/ef/core/
- SQL Server: https://docs.microsoft.com/en-us/sql/

---

## Deployment Sign-Off

**Deployed By:** _________________  
**Date:** _________________  
**Time:** _________________  
**Production URL:** _________________  
**Status:** ☐ Success  ☐ Issues (see notes)  

**Notes:**
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

**Next Steps:**
- [ ] Monitor application for 24-48 hours
- [ ] Set up regular database backups
- [ ] Configure SSL certificate (if not done)
- [ ] Update DNS if needed
- [ ] Train users on production system
