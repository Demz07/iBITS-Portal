# 🚀 iBITS Portal - Azure Deployment Scripts

Quick deployment scripts to get your iBITS Portal running on Microsoft Azure in minutes!

## 📋 Prerequisites

Before running these scripts, ensure you have:

- ✅ **Azure Account** with active subscription
- ✅ **Azure CLI** installed ([Download](https://aka.ms/installazurecliwindows))
- ✅ **PowerShell 5.1+** (Windows) or **PowerShell Core 7+**
- ✅ **.NET 8.0 SDK** installed
- ✅ **SQL Server Management Studio** (optional, for database verification)

## 🎯 Deployment Steps

### Quick Start (3 Scripts to Deploy)

Run these scripts in order from the `Azure_Deploy_Scripts` folder:

```powershell
# Navigate to scripts folder
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"

# Step 1: Export your local database
.\01_Export_Database.ps1

# Step 2: Create Azure resources (SQL + App Service)
.\02_Create_Azure_Resources.ps1

# Step 3: Deploy your application
.\03_Deploy_Application.ps1
```

That's it! Your application will be live on Azure! 🎉

---

## 📜 Script Details

### Script 1: `01_Export_Database.ps1`

**What it does:**
- Backs up your local PortaliBITS database
- Generates SQL scripts for migration
- Exports table data to CSV for verification
- Creates a migration summary report

**Output:**
- `C:\Temp\iBITS_Azure_Migration\PortaliBITS_Backup_[timestamp].bak`
- `C:\Temp\iBITS_Azure_Migration\Database_Stats_[timestamp].txt`
- `C:\Temp\iBITS_Azure_Migration\Data_*.csv`
- `C:\Temp\iBITS_Azure_Migration\EXPORT_SUMMARY_[timestamp].txt`

**Duration:** ~1-2 minutes

---

### Script 2: `02_Create_Azure_Resources.ps1`

**What it does:**
- Logs you into Azure
- Creates Resource Group
- Creates Azure SQL Server + Database
- Configures firewall rules
- Creates App Service Plan + Web App
- Sets up connection strings
- Generates deployment summary with credentials

**Interactive inputs:**
- Resource group name (default: `iBITS-Portal-RG`)
- Azure region (default: Southeast Asia)
- SQL Server name (must be globally unique)
- SQL admin username and password
- Database name (default: `PortaliBITS`)
- Web app name (must be globally unique)
- Pricing tiers (Free/Basic/Standard)

**Output:**
- All Azure resources created and configured
- `C:\Temp\iBITS_Azure_Migration\Azure_Deployment_Summary_[timestamp].txt`

**Duration:** ~3-5 minutes

**Estimated Cost:**
- **Testing**: ~$5/month (Basic SQL + Free App Service)
- **Production**: ~$18/month (Basic SQL + B1 App Service)

---

### Script 3: `03_Deploy_Application.ps1`

**What it does:**
- Builds your .NET 8.0 application in Release mode
- Creates deployment package (ZIP)
- Deploys to Azure App Service
- Tests the deployment
- Opens your live application in browser

**Interactive inputs:**
- Select resource group
- Select web app to deploy to

**Output:**
- Application deployed to Azure
- `C:\Temp\iBITS_Azure_Migration\Deployment_Summary_[timestamp].txt`

**Duration:** ~2-3 minutes

---

## 🗄️ Database Migration

After creating Azure resources, you need to import your database to Azure SQL.

### Option A: Using SQL Server Management Studio (SSMS) - **Recommended**

1. **Connect to Azure SQL:**
   - Server: `[your-server].database.windows.net`
   - Authentication: SQL Server Authentication
   - Login: `ibitsadmin` (or your chosen username)
   - Password: (from deployment summary)

2. **Import Database:**
   - Right-click **Databases** → **Import Data-tier Application**
   - Select backup file: `C:\Temp\iBITS_Azure_Migration\PortaliBITS_Backup_*.bak`
   - Follow wizard to complete

3. **Verify Import:**
   ```sql
   SELECT COUNT(*) FROM Student;
   SELECT COUNT(*) FROM Officer;
   SELECT COUNT(*) FROM Fee;
   ```

### Option B: Using Azure Data Studio

1. Install [Azure Data Studio](https://aka.ms/azuredatastudio)
2. Connect to your Azure SQL server
3. Use **Import** wizard to upload database

### Option C: Using sqlcmd

```powershell
# Navigate to backup folder
cd C:\Temp\iBITS_Azure_Migration

# Find your SQL script file (if generated)
# Note: Replace placeholders with your actual values

sqlcmd -S "your-server.database.windows.net" `
       -d "PortaliBITS" `
       -U "ibitsadmin" `
       -P "your-password" `
       -i "PortaliBITS_Complete_*.sql"
```

---

## ✅ Post-Deployment Checklist

After running all scripts, verify:

- [ ] **Azure Resources Created**
  ```powershell
  az group show --name iBITS-Portal-RG
  az sql db show --name PortaliBITS --server [your-server] --resource-group iBITS-Portal-RG
  az webapp show --name [your-app] --resource-group iBITS-Portal-RG
  ```

- [ ] **Database Imported**
  - Test connection with SSMS
  - Verify table counts match local database

- [ ] **Application Deployed**
  - Visit: `https://[your-app].azurewebsites.net`
  - Login page appears

- [ ] **Functionality Testing**
  - [ ] Admin login works
  - [ ] Student data displays
  - [ ] Fee/Fine records visible
  - [ ] Officer management works
  - [ ] Event creation/management works

- [ ] **Security Configuration**
  - [ ] HTTPS enforced (automatic)
  - [ ] SQL firewall configured
  - [ ] Strong passwords used

---

## 🔧 Troubleshooting

### Issue: "Cannot connect to database"

**Solution:**
```powershell
# Add your IP to SQL firewall
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org").Content

az sql server firewall-rule create `
  --resource-group iBITS-Portal-RG `
  --server [your-server] `
  --name AllowMyIP `
  --start-ip-address $myIp `
  --end-ip-address $myIp
```

### Issue: "Application shows 500 error"

**Solution:**
```powershell
# View real-time logs
az webapp log tail --name [your-app] --resource-group iBITS-Portal-RG

# Restart application
az webapp restart --name [your-app] --resource-group iBITS-Portal-RG
```

### Issue: "Database import fails"

**Solution:**
- Ensure firewall allows your IP
- Check SQL credentials are correct
- Try using SSMS instead of sqlcmd
- Verify backup file is not corrupted

### Issue: "Web app name already exists"

**Solution:**
- Choose a different, unique name
- Try adding random numbers: `ibits-portal-$(Get-Random -Maximum 9999)`

---

## 📊 Monitoring Your Application

### View Application Logs

```powershell
# Real-time logs
az webapp log tail --name [your-app] --resource-group iBITS-Portal-RG

# Download logs
az webapp log download --name [your-app] --resource-group iBITS-Portal-RG
```

### Check Application Health

```powershell
# Get app status
az webapp show --name [your-app] --resource-group iBITS-Portal-RG --query state

# Browse to log stream in portal
az webapp browse --name [your-app] --resource-group iBITS-Portal-RG
```

### Database Monitoring

```powershell
# Check database size
az sql db show --name PortaliBITS --server [your-server] --resource-group iBITS-Portal-RG --query maxSizeBytes

# View database metrics
az sql db show-usage --name PortaliBITS --server [your-server] --resource-group iBITS-Portal-RG
```

---

## 🔄 Updating Your Application

To deploy updates after initial deployment:

```powershell
# Just run script 3 again
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"
.\03_Deploy_Application.ps1
```

Or use Visual Studio:
1. Right-click project → **Publish**
2. Select your existing publish profile
3. Click **Publish**

---

## 🗑️ Cleanup (Delete All Resources)

To remove everything and stop charges:

```powershell
# Delete entire resource group (removes all resources)
az group delete --name iBITS-Portal-RG --yes --no-wait

# This removes:
# - SQL Server + Database
# - App Service Plan + Web App
# - All associated resources
```

**⚠️ Warning:** This is permanent and cannot be undone!

---

## 💰 Cost Management

### Monitor Costs

```powershell
# View cost analysis
az consumption usage list --start-date 2026-02-01 --end-date 2026-02-28

# Set budget alerts in Azure Portal:
# Cost Management → Budgets → Create
```

### Optimize Costs

**For Testing/Development:**
- Use **Free F1** App Service tier ($0/month)
- Use **Basic** SQL tier (~$5/month)
- **Total: ~$5/month**

**For Production:**
- Use **Basic B1** App Service tier (~$13/month)
- Use **Standard S0** SQL tier (~$15/month)
- **Total: ~$28/month**

**Stop/Start Resources:**
```powershell
# Stop web app (stops charges for compute)
az webapp stop --name [your-app] --resource-group iBITS-Portal-RG

# Start web app
az webapp start --name [your-app] --resource-group iBITS-Portal-RG
```

---

## 📞 Support & Resources

- **Azure Portal:** https://portal.azure.com
- **Azure Documentation:** https://docs.microsoft.com/azure/
- **Azure CLI Reference:** https://docs.microsoft.com/cli/azure/
- **Pricing Calculator:** https://azure.microsoft.com/pricing/calculator/
- **Azure Support:** https://azure.microsoft.com/support/

---

## 📝 File Locations

### Script Outputs
All script outputs are saved to:
```
C:\Temp\iBITS_Azure_Migration\
├── PortaliBITS_Backup_[timestamp].bak
├── Database_Stats_[timestamp].txt
├── Data_[TableName]_[timestamp].csv
├── EXPORT_SUMMARY_[timestamp].txt
├── Azure_Deployment_Summary_[timestamp].txt
└── Deployment_Summary_[timestamp].txt
```

### Configuration Files
- `appsettings.Production.json` - Azure production settings
- `AZURE_DEPLOYMENT_GUIDE.md` - Complete deployment documentation

---

## 🎓 Next Steps After Successful Deployment

1. **Custom Domain** - Set up your own domain (e.g., portal.youruniversity.edu)
2. **SSL Certificate** - Free with Azure (automatic with custom domains)
3. **Application Insights** - Add monitoring and analytics
4. **Automated Backups** - Configure database backup retention
5. **CI/CD Pipeline** - Set up GitHub Actions or Azure DevOps
6. **Scaling** - Configure auto-scaling for high traffic
7. **Security** - Review and enhance security settings

---

**Created:** February 2026  
**Version:** 1.0  
**Project:** iBITS Portal  
**Platform:** Microsoft Azure

For detailed step-by-step instructions, see: [AZURE_DEPLOYMENT_GUIDE.md](../AZURE_DEPLOYMENT_GUIDE.md)
