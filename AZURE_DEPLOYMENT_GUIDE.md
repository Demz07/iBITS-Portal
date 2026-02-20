# 🚀 iBITS Portal - Azure Deployment Guide

## Complete Full-Stack Deployment to Azure

This guide will help you deploy your iBITS Portal system to Microsoft Azure, including the ASP.NET Core web application and SQL Server database.

---

## 📋 Prerequisites

### 1. **Azure Account**
- Active Azure subscription ([Get free trial](https://azure.microsoft.com/free/))
- At least "Contributor" role access

### 2. **Required Tools**
- Visual Studio 2022 (or VS Code with Azure extensions)
- Azure CLI ([Download](https://aka.ms/installazurecliwindows))
- SQL Server Management Studio (SSMS) - for database export

### 3. **Project Information**
- **Framework**: .NET 8.0
- **Database**: SQL Server (PortaliBITS)
- **Current Server**: DESKTOP-SG3AI25\SQLEXPRESS
- **Tables**: 132 Students, 5 Officers, 40 Fees, 40 Fines, etc.

---

## 🎯 Deployment Architecture

```
┌─────────────────────────────────────────────┐
│           Azure Cloud Services              │
├─────────────────────────────────────────────┤
│                                             │
│  ┌─────────────────────────────────┐        │
│  │   Azure App Service              │        │
│  │   (Web Application)              │        │
│  │   - ASP.NET Core 8.0            │        │
│  │   - Auto-scaling enabled        │        │
│  │   - HTTPS enforced              │        │
│  └──────────────┬──────────────────┘        │
│                 │                            │
│                 │ Connection String          │
│                 ▼                            │
│  ┌─────────────────────────────────┐        │
│  │   Azure SQL Database             │        │
│  │   - PortaliBITS                 │        │
│  │   - Firewall rules configured   │        │
│  │   - Automated backups           │        │
│  └─────────────────────────────────┘        │
│                                             │
└─────────────────────────────────────────────┘
```

---

## 📦 PART 1: Prepare Your Database for Azure

### Step 1: Export Your Local Database

```powershell
# Open PowerShell as Administrator and run:

# Create backup directory
New-Item -ItemType Directory -Force -Path "C:\Temp\iBITS_Backup"

# Export database using SQLCMD
sqlcmd -S "DESKTOP-SG3AI25\SQLEXPRESS" -E -Q "BACKUP DATABASE [PortaliBITS] TO DISK = 'C:\Temp\iBITS_Backup\PortaliBITS.bak' WITH FORMAT, INIT, NAME = 'Full Backup of PortaliBITS';"
```

### Step 2: Generate Database Script (Alternative Method)

If you prefer a SQL script instead of backup file:

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to `DESKTOP-SG3AI25\SQLEXPRESS`
3. Right-click on `PortaliBITS` database → **Tasks** → **Generate Scripts**
4. Select **Script entire database and all database objects**
5. Click **Advanced** → Set **Types of data to script** to **Schema and data**
6. Save to: `C:\Temp\iBITS_Backup\PortaliBITS_Complete.sql`

---

## 🌩️ PART 2: Create Azure Resources

### Method A: Using Azure Portal (Recommended for Beginners)

#### 1. Create Resource Group

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Resource groups** → **+ Create**
3. Settings:
   - **Subscription**: Your subscription
   - **Resource group**: `iBITS-Portal-RG`
   - **Region**: `Southeast Asia` (or closest to your users)
4. Click **Review + create** → **Create**

#### 2. Create Azure SQL Database

1. In Azure Portal, click **+ Create a resource**
2. Search for **SQL Database** → **Create**
3. **Basics** tab:
   - **Resource group**: `iBITS-Portal-RG`
   - **Database name**: `PortaliBITS`
   - **Server**: Click **Create new**
     - **Server name**: `ibits-portal-sql` (must be globally unique)
     - **Location**: Same as resource group
     - **Authentication**: SQL authentication
     - **Server admin login**: `ibitsadmin`
     - **Password**: `YourSecurePassword123!` (change this!)
   - **Compute + storage**: Click **Configure database**
     - Choose **Basic** (5 DTUs, 2GB) for testing - ~$5/month
     - Or **Standard S0** (10 DTUs) for production - ~$15/month
4. **Networking** tab:
   - **Connectivity method**: Public endpoint
   - **Allow Azure services**: Yes
   - **Add current client IP**: Yes (important!)
5. Click **Review + create** → **Create**

#### 3. Configure SQL Firewall

1. Go to your SQL Server resource: `ibits-portal-sql`
2. Click **Networking** (left menu)
3. Under **Firewall rules**, add:
   - **Rule name**: `AllowMyComputer`
   - **Start IP**: Your public IP (auto-detected)
   - **End IP**: Same as start IP
4. Check **Allow Azure services and resources to access this server**
5. Click **Save**

#### 4. Import Database to Azure SQL

**Option A: Using SSMS**

1. Open **SSMS**
2. Connect to Azure SQL Server:
   - **Server name**: `ibits-portal-sql.database.windows.net`
   - **Authentication**: SQL Server Authentication
   - **Login**: `ibitsadmin`
   - **Password**: (your password)
3. Right-click **Databases** → **Import Data-tier Application**
4. Select your backup file: `C:\Temp\iBITS_Backup\PortaliBITS.bak`
5. Follow wizard to import

**Option B: Using Azure Data Studio**

1. Install [Azure Data Studio](https://aka.ms/azuredatastudio)
2. Connect to Azure SQL server
3. Use **Import** wizard to upload your database

**Option C: Using SQL Script**

```powershell
# Run from PowerShell
sqlcmd -S "ibits-portal-sql.database.windows.net" -d "PortaliBITS" -U "ibitsadmin" -P "YourSecurePassword123!" -i "C:\Temp\iBITS_Backup\PortaliBITS_Complete.sql"
```

#### 5. Create Azure App Service

1. In Azure Portal, click **+ Create a resource**
2. Search for **Web App** → **Create**
3. **Basics** tab:
   - **Resource group**: `iBITS-Portal-RG`
   - **Name**: `ibits-portal` (must be globally unique)
     - This creates: `https://ibits-portal.azurewebsites.net`
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Windows
   - **Region**: Same as database
   - **Pricing plan**: 
     - **Free F1** for testing (no cost, limited)
     - **Basic B1** for production (~$13/month)
4. Click **Review + create** → **Create**

---

### Method B: Using Azure CLI (Advanced/Faster)

```powershell
# Login to Azure
az login

# Set variables
$resourceGroup = "iBITS-Portal-RG"
$location = "southeastasia"
$sqlServer = "ibits-portal-sql"
$sqlAdmin = "ibitsadmin"
$sqlPassword = "YourSecurePassword123!"
$database = "PortaliBITS"
$webApp = "ibits-portal"

# Create resource group
az group create --name $resourceGroup --location $location

# Create SQL Server
az sql server create `
  --name $sqlServer `
  --resource-group $resourceGroup `
  --location $location `
  --admin-user $sqlAdmin `
  --admin-password $sqlPassword

# Configure firewall
az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Get your IP
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Allow your IP
az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name AllowMyComputer `
  --start-ip-address $myIp `
  --end-ip-address $myIp

# Create SQL Database (Basic tier)
az sql db create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name $database `
  --service-objective Basic

# Create App Service Plan
az appservice plan create `
  --name "$webApp-plan" `
  --resource-group $resourceGroup `
  --location $location `
  --sku B1

# Create Web App
az webapp create `
  --name $webApp `
  --resource-group $resourceGroup `
  --plan "$webApp-plan" `
  --runtime "DOTNET|8.0"

Write-Host "✅ Azure resources created successfully!"
Write-Host "📊 SQL Server: $sqlServer.database.windows.net"
Write-Host "🌐 Web App: https://$webApp.azurewebsites.net"
```

---

## ⚙️ PART 3: Configure Your Application

### Step 1: Update Connection String for Azure

Create/update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:ibits-portal-sql.database.windows.net,1433;Initial Catalog=PortaliBITS;Persist Security Info=False;User ID=ibitsadmin;Password=YourSecurePassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Step 2: Configure Azure App Service Settings

**Via Azure Portal:**

1. Go to your App Service: `ibits-portal`
2. Click **Configuration** (left menu)
3. Under **Application settings**, click **+ New connection string**
4. Settings:
   - **Name**: `DefaultConnection`
   - **Value**: `Server=tcp:ibits-portal-sql.database.windows.net,1433;Initial Catalog=PortaliBITS;User ID=ibitsadmin;Password=YourSecurePassword123!;Encrypt=True;`
   - **Type**: `SQLAzure`
5. Click **OK** → **Save**

**Via Azure CLI:**

```powershell
az webapp config connection-string set `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --connection-string-type SQLAzure `
  --settings DefaultConnection="Server=tcp:ibits-portal-sql.database.windows.net,1433;Initial Catalog=PortaliBITS;User ID=ibitsadmin;Password=YourSecurePassword123!;Encrypt=True;"
```

---

## 🚀 PART 4: Deploy Your Application

### Method 1: Deploy from Visual Studio (Easiest)

1. Open `iBITS Portal.sln` in Visual Studio 2022
2. Right-click on the **iBITS Portal** project → **Publish**
3. Click **Add a publish profile** → **Azure** → **Next**
4. Select **Azure App Service (Windows)** → **Next**
5. Sign in to your Azure account
6. Select:
   - **Subscription**: Your subscription
   - **Resource Group**: `iBITS-Portal-RG`
   - **App Service**: `ibits-portal`
7. Click **Finish**
8. Review settings → Click **Publish**
9. Wait for deployment to complete (2-5 minutes)
10. Browser will open with your deployed app!

### Method 2: Deploy using Azure CLI

```powershell
# Navigate to your project directory
cd "C:\Users\Dave\source\repos\iBITS Portal"

# Build the application
dotnet publish "iBITS Portal/iBITS Portal.csproj" -c Release -o ./publish

# Create deployment ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

# Deploy to Azure
az webapp deploy `
  --resource-group iBITS-Portal-RG `
  --name ibits-portal `
  --src-path ./deploy.zip `
  --type zip

Write-Host "✅ Deployment complete!"
Write-Host "🌐 Visit: https://ibits-portal.azurewebsites.net"
```

### Method 3: Deploy using Git (Continuous Deployment)

```powershell
# Enable local Git deployment
az webapp deployment source config-local-git `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG

# Get deployment credentials
az webapp deployment list-publishing-credentials `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --query "{Username:publishingUserName, Password:publishingPassword}" `
  --output table

# In your project directory
cd "C:\Users\Dave\source\repos\iBITS Portal"

# Initialize git (if not already)
git init
git add .
git commit -m "Initial commit for Azure deployment"

# Add Azure remote
git remote add azure https://ibits-portal.scm.azurewebsites.net:443/ibits-portal.git

# Push to Azure
git push azure master
```

---

## ✅ PART 5: Post-Deployment Verification

### 1. Test Database Connection

Run this verification script:

```powershell
# Test Azure SQL connection
sqlcmd -S "ibits-portal-sql.database.windows.net" -d "PortaliBITS" -U "ibitsadmin" -P "YourSecurePassword123!" -Q "SELECT COUNT(*) as StudentCount FROM Student; SELECT COUNT(*) as OfficerCount FROM Officer;"
```

### 2. Test Web Application

1. Visit: `https://ibits-portal.azurewebsites.net`
2. Try logging in with your admin account
3. Verify:
   - ✅ Login works
   - ✅ Dashboard loads
   - ✅ Student data appears
   - ✅ Fee/Fine records display

### 3. Check Application Logs

**Via Azure Portal:**
1. Go to App Service → **Log stream**
2. Monitor real-time logs

**Via CLI:**
```powershell
az webapp log tail --name ibits-portal --resource-group iBITS-Portal-RG
```

---

## 🔒 PART 6: Security Best Practices

### 1. Use Azure Key Vault for Secrets

```powershell
# Create Key Vault
az keyvault create `
  --name "ibits-portal-kv" `
  --resource-group iBITS-Portal-RG `
  --location southeastasia

# Store database password
az keyvault secret set `
  --vault-name "ibits-portal-kv" `
  --name "SqlPassword" `
  --value "YourSecurePassword123!"

# Enable managed identity for App Service
az webapp identity assign `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG

# Grant App Service access to Key Vault
$appId = az webapp identity show --name ibits-portal --resource-group iBITS-Portal-RG --query principalId -o tsv

az keyvault set-policy `
  --name "ibits-portal-kv" `
  --object-id $appId `
  --secret-permissions get list
```

### 2. Enable HTTPS Only

```powershell
az webapp update `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --https-only true
```

### 3. Configure Custom Domain (Optional)

```powershell
# Add custom domain
az webapp config hostname add `
  --webapp-name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --hostname "portal.yourdomain.com"

# Enable SSL
az webapp config ssl create `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --hostname "portal.yourdomain.com"
```

---

## 💰 Cost Estimation

### Monthly Costs (Approximate)

| Service | Tier | Cost/Month |
|---------|------|------------|
| **App Service** | B1 Basic | $13.14 |
| **SQL Database** | Basic (2GB) | $4.90 |
| **Bandwidth** | First 5GB free | ~$0-2 |
| **Total** | | **~$18-20/month** |

### Free Tier Option (For Testing)

| Service | Tier | Cost/Month |
|---------|------|------------|
| **App Service** | F1 Free | $0 |
| **SQL Database** | Basic (2GB) | $4.90 |
| **Total** | | **~$5/month** |

**Note**: Free tier has limitations (60 min/day CPU, 1GB RAM)

---

## 🔧 Troubleshooting

### Issue 1: "Cannot connect to database"

**Solution:**
```powershell
# Check firewall rules
az sql server firewall-rule list `
  --resource-group iBITS-Portal-RG `
  --server ibits-portal-sql

# Add your current IP
$myIp = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az sql server firewall-rule create `
  --resource-group iBITS-Portal-RG `
  --server ibits-portal-sql `
  --name AllowMyNewIP `
  --start-ip-address $myIp `
  --end-ip-address $myIp
```

### Issue 2: "Application Error"

**Solution:**
```powershell
# Enable detailed errors
az webapp config set `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --startup-file "ASPNETCORE_ENVIRONMENT=Development"

# Check logs
az webapp log tail --name ibits-portal --resource-group iBITS-Portal-RG
```

### Issue 3: "500 Internal Server Error"

**Check:**
1. Connection string is correct
2. Database migrations are applied
3. All NuGet packages are compatible with Azure
4. Application Insights for detailed error tracking

---

## 📊 Monitoring & Maintenance

### Enable Application Insights

```powershell
# Create Application Insights
az monitor app-insights component create `
  --app ibits-portal-insights `
  --location southeastasia `
  --resource-group iBITS-Portal-RG `
  --application-type web

# Link to App Service
$instrumentationKey = az monitor app-insights component show `
  --app ibits-portal-insights `
  --resource-group iBITS-Portal-RG `
  --query instrumentationKey -o tsv

az webapp config appsettings set `
  --name ibits-portal `
  --resource-group iBITS-Portal-RG `
  --settings "APPINSIGHTS_INSTRUMENTATIONKEY=$instrumentationKey"
```

### Set Up Automated Backups

```powershell
# Enable SQL Database automated backups (included by default)
# Retention: 7 days for Basic tier, 35 days for Standard

# Manual backup
az sql db export `
  --name PortaliBITS `
  --server ibits-portal-sql `
  --resource-group iBITS-Portal-RG `
  --admin-user ibitsadmin `
  --admin-password "YourSecurePassword123!" `
  --storage-key-type StorageAccessKey `
  --storage-key "your-storage-key" `
  --storage-uri "https://yourstorage.blob.core.windows.net/backups/portalibits.bacpac"
```

---

## 🎓 Next Steps After Deployment

1. ✅ **Test thoroughly** - Verify all features work in production
2. 🔒 **Set up SSL certificate** - Use custom domain with HTTPS
3. 📧 **Configure email** - SendGrid or Azure Communication Services
4. 📊 **Monitor performance** - Use Application Insights
5. 🔄 **Set up CI/CD** - GitHub Actions or Azure DevOps
6. 👥 **User training** - Provide production URL to stakeholders
7. 📱 **Mobile testing** - Ensure responsive design works
8. 🔐 **Security audit** - Review access controls and permissions

---

## 📞 Support Resources

- **Azure Documentation**: https://docs.microsoft.com/azure/
- **Azure Support**: https://azure.microsoft.com/support/
- **Pricing Calculator**: https://azure.microsoft.com/pricing/calculator/
- **Azure Portal**: https://portal.azure.com
- **Azure CLI Reference**: https://docs.microsoft.com/cli/azure/

---

## 📝 Quick Reference

### Important URLs
- **Azure Portal**: https://portal.azure.com
- **Your Web App**: https://ibits-portal.azurewebsites.net
- **SQL Server**: ibits-portal-sql.database.windows.net
- **App Service Editor**: https://ibits-portal.scm.azurewebsites.net

### Connection Strings
```
Local:
Server=DESKTOP-SG3AI25\SQLEXPRESS;Database=PortaliBITS;Trusted_Connection=True;TrustServerCertificate=True;

Azure:
Server=tcp:ibits-portal-sql.database.windows.net,1433;Initial Catalog=PortaliBITS;User ID=ibitsadmin;Password=YourSecurePassword123!;Encrypt=True;
```

---

**Created**: February 2026  
**Version**: 1.0  
**Project**: iBITS Portal  
**Target**: Microsoft Azure Cloud

