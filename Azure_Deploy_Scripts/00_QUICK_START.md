# ⚡ Quick Start - Deploy to Azure in 15 Minutes

Follow these steps to deploy your iBITS Portal to Microsoft Azure.

## 🚦 Before You Start

**Required:**
- Azure account ([Get free trial](https://azure.microsoft.com/free/))
- Azure CLI installed ([Download](https://aka.ms/installazurecliwindows))

**Check Azure CLI:**
```powershell
az version
```

If not installed, download and install from the link above.

---

## 🎯 3-Step Deployment

### Step 1️⃣: Export Database (2 minutes)

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"
.\01_Export_Database.ps1
```

**What happens:**
- Backs up your local database
- Saves to: `C:\Temp\iBITS_Azure_Migration\`

---

### Step 2️⃣: Create Azure Resources (5 minutes)

```powershell
.\02_Create_Azure_Resources.ps1
```

**What happens:**
- You'll be asked to login to Azure (browser opens)
- Script asks for names and configuration
- Creates SQL Server, Database, and Web App
- **SAVE THE CREDENTIALS** displayed at the end!

**Recommended Settings:**
- Resource Group: `iBITS-Portal-RG`
- Region: `1` (Southeast Asia)
- SQL Tier: `1` (Basic - $5/month)
- App Tier: `2` (Basic B1 - $13/month)

**Total Cost: ~$18/month**

---

### Step 3️⃣: Import Database to Azure (5 minutes)

**Using SQL Server Management Studio (SSMS):**

1. Open SSMS
2. Connect to Azure SQL:
   - Server: `[your-server].database.windows.net` (from script output)
   - Authentication: SQL Server Authentication
   - Username: `ibitsadmin` (or what you chose)
   - Password: (from script output)
3. Right-click **Databases** → **Import Data-tier Application**
4. Browse to: `C:\Temp\iBITS_Azure_Migration\PortaliBITS_Backup_*.bak`
5. Click Next → Next → Finish
6. Wait for import (2-3 minutes)

**Verify import:**
```sql
SELECT COUNT(*) as StudentCount FROM Student;
SELECT COUNT(*) as OfficerCount FROM Officer;
```

---

### Step 4️⃣: Deploy Application (3 minutes)

```powershell
.\03_Deploy_Application.ps1
```

**What happens:**
- Builds your application
- Uploads to Azure
- Opens your live site automatically!

---

## ✅ Test Your Deployment

Visit: `https://[your-app].azurewebsites.net`

Try:
- [ ] Login with admin account
- [ ] View students
- [ ] Check fees/fines
- [ ] Create/view events

---

## 🎉 You're Done!

Your iBITS Portal is now live on Azure!

### Important Links (save these):

**Your Application:**
`https://[your-app].azurewebsites.net`

**Azure Portal:**
https://portal.azure.com

**Database Connection:**
```
Server: [your-server].database.windows.net
Database: PortaliBITS
Username: ibitsadmin
Password: [your-password]
```

---

## 🆘 Something Wrong?

### Application won't load?
```powershell
# Check logs
az webapp log tail --name [your-app] --resource-group iBITS-Portal-RG

# Restart app
az webapp restart --name [your-app] --resource-group iBITS-Portal-RG
```

### Can't connect to database?
```powershell
# Add your IP to firewall
az sql server firewall-rule create `
  --resource-group iBITS-Portal-RG `
  --server [your-server] `
  --name MyIP `
  --start-ip-address [your-ip] `
  --end-ip-address [your-ip]
```

### Need more help?
See: `README.md` or `AZURE_DEPLOYMENT_GUIDE.md`

---

## 📋 Summary Files

All credentials and summaries saved to:
```
C:\Temp\iBITS_Azure_Migration\
```

**⚠️ KEEP THESE FILES SAFE!** They contain your passwords and connection strings.

---

## 🔄 Deploy Updates Later

To update your application after changes:

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"
.\03_Deploy_Application.ps1
```

---

## 💰 Monthly Cost

- SQL Database (Basic): ~$5
- App Service (B1): ~$13
- **Total: ~$18/month**

To check current costs:
1. Go to https://portal.azure.com
2. Navigate to **Cost Management**
3. View your spending

---

**Need detailed instructions?** See [AZURE_DEPLOYMENT_GUIDE.md](../AZURE_DEPLOYMENT_GUIDE.md)

**Happy deploying! 🚀**
