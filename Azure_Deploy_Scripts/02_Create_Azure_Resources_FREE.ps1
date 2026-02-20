# ============================================================================
# iBITS Portal - Azure Resources Creation Script (FREE TIER)
# ============================================================================
# This script creates Azure resources using FREE and low-cost tiers
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - FREE Azure Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "💰 This deployment uses FREE or minimal-cost tiers:" -ForegroundColor Green
Write-Host "   • App Service: F1 FREE (1GB RAM, 60 min/day CPU)" -ForegroundColor Cyan
Write-Host "   • SQL Database: Basic (~`$5/month - lowest tier)" -ForegroundColor Cyan
Write-Host "   • Total estimated cost: ~`$5/month" -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is installed
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Host "   ✅ Azure CLI installed: $($azVersion.'azure-cli')" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Azure CLI is not installed!" -ForegroundColor Red
    Write-Host "   📥 Download from: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    exit 1
}

# Login to Azure
Write-Host ""
Write-Host "🔐 Step 1: Azure Login" -ForegroundColor Yellow
Write-Host "   Opening browser for authentication..." -ForegroundColor Gray
az login

if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Login failed!" -ForegroundColor Red
    exit 1
}

# Get subscription info
$subscriptions = az account list --output json | ConvertFrom-Json
if ($subscriptions.Count -gt 1) {
    Write-Host ""
    Write-Host "Available subscriptions:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $subscriptions.Count; $i++) {
        Write-Host "   [$i] $($subscriptions[$i].name) - $($subscriptions[$i].id)" -ForegroundColor Gray
    }
    $selection = Read-Host "Select subscription number [0]"
    if ([string]::IsNullOrWhiteSpace($selection)) { $selection = 0 }
    az account set --subscription $subscriptions[$selection].id
}

$currentSub = az account show --output json | ConvertFrom-Json
Write-Host "   ✅ Using subscription: $($currentSub.name)" -ForegroundColor Green

# Configuration with FREE tier defaults
Write-Host ""
Write-Host "⚙️  Step 2: Configuration (FREE TIER)" -ForegroundColor Yellow
Write-Host ""

$resourceGroup = Read-Host "Enter Resource Group name [iBITS-Portal-FREE]"
if ([string]::IsNullOrWhiteSpace($resourceGroup)) { $resourceGroup = "iBITS-Portal-FREE" }

Write-Host ""
Write-Host "Available regions:" -ForegroundColor Cyan
Write-Host "   1. Southeast Asia (Singapore) - Recommended for Philippines" -ForegroundColor Gray
Write-Host "   2. East Asia (Hong Kong)" -ForegroundColor Gray
Write-Host "   3. Australia East (Sydney)" -ForegroundColor Gray
Write-Host "   4. East US (Virginia)" -ForegroundColor Gray
Write-Host "   5. West Europe (Netherlands)" -ForegroundColor Gray
$regionChoice = Read-Host "Select region [1]"
if ([string]::IsNullOrWhiteSpace($regionChoice)) { $regionChoice = 1 }

$location = switch ($regionChoice) {
    1 { "southeastasia" }
    2 { "eastasia" }
    3 { "australiaeast" }
    4 { "eastus" }
    5 { "westeurope" }
    default { "southeastasia" }
}

$sqlServer = Read-Host "Enter SQL Server name (must be globally unique) [ibits-free-$(Get-Random -Maximum 9999)]"
if ([string]::IsNullOrWhiteSpace($sqlServer)) { $sqlServer = "ibits-free-$(Get-Random -Maximum 9999)" }

$sqlAdmin = Read-Host "Enter SQL admin username [ibitsadmin]"
if ([string]::IsNullOrWhiteSpace($sqlAdmin)) { $sqlAdmin = "ibitsadmin" }

# Password validation loop
do {
    $sqlPassword = Read-Host "Enter SQL admin password (min 8 chars, must include uppercase, lowercase, digit, special char)" -AsSecureString
    $sqlPasswordText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword))
    
    $passwordValid = $sqlPasswordText.Length -ge 8 -and 
                     $sqlPasswordText -cmatch '[A-Z]' -and 
                     $sqlPasswordText -cmatch '[a-z]' -and 
                     $sqlPasswordText -match '\d' -and 
                     $sqlPasswordText -match '[^a-zA-Z0-9]'
    
    if (-not $passwordValid) {
        Write-Host "   ❌ Password does not meet requirements. Try again." -ForegroundColor Red
    }
} while (-not $passwordValid)

$database = Read-Host "Enter database name [PortaliBITS]"
if ([string]::IsNullOrWhiteSpace($database)) { $database = "PortaliBITS" }

$webApp = Read-Host "Enter Web App name (must be globally unique) [ibits-free-$(Get-Random -Maximum 9999)]"
if ([string]::IsNullOrWhiteSpace($webApp)) { $webApp = "ibits-free-$(Get-Random -Maximum 9999)" }

# Fixed to FREE tier
$sqlServiceObjective = "Basic"  # ~$5/month (no free SQL tier available)
$appServiceSku = "F1"  # FREE tier

# Confirmation
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "     FREE TIER Configuration Summary" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Resource Group:    $resourceGroup" -ForegroundColor White
Write-Host "Location:          $location" -ForegroundColor White
Write-Host "SQL Server:        $sqlServer.database.windows.net" -ForegroundColor White
Write-Host "SQL Admin:         $sqlAdmin" -ForegroundColor White
Write-Host "Database:          $database (Basic - `$5/month)" -ForegroundColor Yellow
Write-Host "Web App:           $webApp.azurewebsites.net" -ForegroundColor White
Write-Host "App Service Plan:  F1 FREE (60 min CPU/day, 1GB RAM)" -ForegroundColor Green
Write-Host ""
Write-Host "💰 ESTIMATED COST: ~`$5/month" -ForegroundColor Green
Write-Host ""
Write-Host "⚠️  FREE TIER LIMITATIONS:" -ForegroundColor Yellow
Write-Host "   • 60 CPU minutes per day (resets daily)" -ForegroundColor Gray
Write-Host "   • 1GB RAM, 1GB disk storage" -ForegroundColor Gray
Write-Host "   • No custom domain SSL support" -ForegroundColor Gray
Write-Host "   • App sleeps after 20 min inactivity" -ForegroundColor Gray
Write-Host "   • Good for testing, demos, low-traffic use" -ForegroundColor Gray
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
$confirm = Read-Host "Proceed with FREE tier deployment? (yes/no) [yes]"
if ($confirm -eq "no") {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Start deployment
Write-Host ""
Write-Host "🚀 Step 3: Creating Azure Resources (FREE TIER)" -ForegroundColor Yellow
Write-Host "   This will take 3-5 minutes..." -ForegroundColor Gray
Write-Host ""

# Create Resource Group
Write-Host "   📦 Creating resource group..." -ForegroundColor Cyan
az group create --name $resourceGroup --location $location --output none
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ Resource group created" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create resource group" -ForegroundColor Red
    exit 1
}

# Create SQL Server
Write-Host "   🗄️  Creating SQL Server..." -ForegroundColor Cyan
az sql server create `
  --name $sqlServer `
  --resource-group $resourceGroup `
  --location $location `
  --admin-user $sqlAdmin `
  --admin-password $sqlPasswordText `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ SQL Server created: $sqlServer.database.windows.net" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create SQL Server" -ForegroundColor Red
    Write-Host "      ℹ️  Server name may already exist. Try a different name." -ForegroundColor Yellow
    exit 1
}

# Configure SQL Firewall
Write-Host "   🔒 Configuring firewall rules..." -ForegroundColor Cyan

# Allow Azure services
az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0 `
  --output none

# Get client IP and add rule
try {
    $myIp = (Invoke-WebRequest -Uri "https://api.ipify.org" -UseBasicParsing).Content
    az sql server firewall-rule create `
      --resource-group $resourceGroup `
      --server $sqlServer `
      --name AllowClientIP `
      --start-ip-address $myIp `
      --end-ip-address $myIp `
      --output none
    Write-Host "      ✅ Firewall configured (Your IP: $myIp)" -ForegroundColor Green
} catch {
    Write-Host "      ⚠️  Could not auto-detect IP, add manually in Azure Portal" -ForegroundColor Yellow
}

# Create SQL Database (Basic tier - smallest paid tier)
Write-Host "   💾 Creating SQL Database (Basic tier)..." -ForegroundColor Cyan
az sql db create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name $database `
  --service-objective Basic `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ Database created: $database (2GB, ~`$5/month)" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create database" -ForegroundColor Red
    exit 1
}

# Create App Service Plan (FREE F1)
Write-Host "   📋 Creating App Service Plan (FREE F1)..." -ForegroundColor Cyan
az appservice plan create `
  --name "$webApp-plan" `
  --resource-group $resourceGroup `
  --location $location `
  --sku F1 `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ App Service Plan created (FREE tier)" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create App Service Plan" -ForegroundColor Red
    exit 1
}

# Create Web App
Write-Host "   🌐 Creating Web App (FREE tier)..." -ForegroundColor Cyan
az webapp create `
  --name $webApp `
  --resource-group $resourceGroup `
  --plan "$webApp-plan" `
  --runtime "DOTNET:8.0" `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ Web App created: https://$webApp.azurewebsites.net" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create Web App" -ForegroundColor Red
    Write-Host "      ℹ️  App name may already exist. Try a different name." -ForegroundColor Yellow
    exit 1
}

# Configure Web App Settings
Write-Host "   ⚙️  Configuring Web App..." -ForegroundColor Cyan

# Enable HTTPS only
az webapp update `
  --name $webApp `
  --resource-group $resourceGroup `
  --https-only true `
  --output none

# Set connection string
$connectionString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$database;Persist Security Info=False;User ID=$sqlAdmin;Password=$sqlPasswordText;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set `
  --name $webApp `
  --resource-group $resourceGroup `
  --connection-string-type SQLAzure `
  --settings DefaultConnection="$connectionString" `
  --output none

# Enable always-on (helps prevent app sleep) - NOTE: Not available on Free tier
# Commenting out as F1 doesn't support always-on
# az webapp config set --name $webApp --resource-group $resourceGroup --always-on false --output none

Write-Host "      ✅ Web App configured" -ForegroundColor Green

# Create deployment summary
$deploymentSummary = @"
╔════════════════════════════════════════════════════════════════════╗
║         iBITS Portal - FREE TIER Azure Deployment                ║
╚════════════════════════════════════════════════════════════════════╝

Deployment Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Subscription: $($currentSub.name)

AZURE RESOURCES CREATED (FREE/LOW-COST):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Resource Group: $resourceGroup
   Location: $location

✅ SQL Server: $sqlServer.database.windows.net
   Admin User: $sqlAdmin
   Admin Password: ******** (saved below)

✅ SQL Database: $database
   Tier: Basic (~`$5/month)
   Max Size: 2 GB
   ⚠️  NOTE: No free SQL tier available - Basic is minimum

✅ App Service Plan: $webApp-plan
   Tier: F1 (FREE - `$0/month)
   Limitations:
     • 60 CPU minutes per day
     • 1 GB RAM, 1 GB Storage
     • App sleeps after 20 min idle
     • No Always On support

✅ Web App: $webApp
   URL: https://$webApp.azurewebsites.net
   Runtime: .NET 8.0

IMPORTANT CREDENTIALS (SAVE THESE!):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SQL Server Connection:
  Server: $sqlServer.database.windows.net
  Database: $database
  Username: $sqlAdmin
  Password: $sqlPasswordText

⚠️  SAVE THESE CREDENTIALS SECURELY! ⚠️

CONNECTION STRING:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

$connectionString

FREE TIER LIMITATIONS & TIPS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️  App Service F1 (FREE):
   • Your app will sleep after 20 minutes of inactivity
   • First request after sleep will be slow (cold start ~10-20 sec)
   • Limited to 60 CPU minutes per day (resets at midnight UTC)
   • Good for: Testing, demos, low-traffic personal projects

💡 To wake up sleeping app:
   • Just visit the URL - it will start automatically
   • Or use a free uptime monitoring service (e.g., UptimeRobot)

💡 To reduce CPU usage:
   • Optimize database queries
   • Use caching where possible
   • Minimize background tasks

⚠️  SQL Database Basic:
   • Smallest paid tier (~`$5/month)
   • 2 GB max size
   • Good for small to medium databases
   • Your current DB: ~132 students fits easily

💡 To minimize database costs:
   • Regularly clean old logs/data
   • Monitor database size in Azure Portal

NEXT STEPS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ✅ Azure resources created successfully
2. ⏭️  Import database to Azure SQL
   - Use script: Will guide you next
   - Or manually via SSMS

3. ⏭️  Deploy application code
   - Use script: 03_Deploy_Application.ps1

4. ⏭️  Test your deployment
   - Visit: https://$webApp.azurewebsites.net
   - First load may be slow (cold start)

ESTIMATED MONTHLY COST:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

App Service (F1 FREE): `$0.00/month ✅
SQL Database (Basic): ~`$4.90/month
Data Transfer: ~`$0.00/month (first 5GB free)
─────────────────────────────────────────────────────────────────────
Total: ~`$5/month 💰

⭐ This is the LOWEST COST possible for Azure deployment!

UPGRADE PATH (when you need more):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

When you need more resources:
  • Upgrade to B1 App Service (~`$13/month): No sleep, always-on
  • Upgrade to S0 SQL (~`$15/month): Better performance, 250GB

Command to upgrade App Service:
  az appservice plan update --name $webApp-plan --resource-group $resourceGroup --sku B1

AZURE PORTAL LINKS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Resource Group:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup

SQL Server:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup/providers/Microsoft.Sql/servers/$sqlServer

Web App:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup/providers/Microsoft.Web/sites/$webApp

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎉 FREE TIER Deployment completed successfully! 

Total Cost: Only ~`$5/month for SQL Database (App Service is FREE!)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"@

# Save summary
$summaryPath = "C:\Temp\iBITS_Azure_Migration\Azure_FREE_Deployment_Summary_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
if (-not (Test-Path "C:\Temp\iBITS_Azure_Migration")) {
    New-Item -ItemType Directory -Path "C:\Temp\iBITS_Azure_Migration" -Force | Out-Null
}
$deploymentSummary | Out-File $summaryPath -Encoding UTF8

Write-Host ""
Write-Host $deploymentSummary -ForegroundColor Cyan
Write-Host ""
Write-Host "📄 Summary saved to: $summaryPath" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
