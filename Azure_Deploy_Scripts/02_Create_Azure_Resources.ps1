# ============================================================================
# iBITS Portal - Azure Resources Creation Script
# ============================================================================
# This script creates all necessary Azure resources for deploying iBITS Portal
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - Azure Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
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

# Configuration
Write-Host ""
Write-Host "⚙️  Step 2: Configuration" -ForegroundColor Yellow
Write-Host ""

$resourceGroup = Read-Host "Enter Resource Group name [iBITS-Portal-RG]"
if ([string]::IsNullOrWhiteSpace($resourceGroup)) { $resourceGroup = "iBITS-Portal-RG" }

Write-Host ""
Write-Host "Available regions:" -ForegroundColor Cyan
Write-Host "   1. Southeast Asia (Singapore)" -ForegroundColor Gray
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

$sqlServer = Read-Host "Enter SQL Server name (must be globally unique) [ibits-portal-sql-$(Get-Random -Maximum 9999)]"
if ([string]::IsNullOrWhiteSpace($sqlServer)) { $sqlServer = "ibits-portal-sql-$(Get-Random -Maximum 9999)" }

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

$webApp = Read-Host "Enter Web App name (must be globally unique) [ibits-portal-$(Get-Random -Maximum 9999)]"
if ([string]::IsNullOrWhiteSpace($webApp)) { $webApp = "ibits-portal-$(Get-Random -Maximum 9999)" }

Write-Host ""
Write-Host "Pricing tiers:" -ForegroundColor Cyan
Write-Host "   SQL Database:" -ForegroundColor Gray
Write-Host "     1. Basic (2GB, ~`$5/month) - Good for testing" -ForegroundColor Gray
Write-Host "     2. Standard S0 (250GB, ~`$15/month) - Production ready" -ForegroundColor Gray
$sqlTierChoice = Read-Host "Select SQL tier [1]"
if ([string]::IsNullOrWhiteSpace($sqlTierChoice)) { $sqlTierChoice = 1 }

$sqlServiceObjective = if ($sqlTierChoice -eq 2) { "S0" } else { "Basic" }

Write-Host ""
Write-Host "   App Service:" -ForegroundColor Gray
Write-Host "     1. Free F1 (Limited, ~`$0/month) - Testing only" -ForegroundColor Gray
Write-Host "     2. Basic B1 (~`$13/month) - Production ready" -ForegroundColor Gray
$appTierChoice = Read-Host "Select App Service tier [2]"
if ([string]::IsNullOrWhiteSpace($appTierChoice)) { $appTierChoice = 2 }

$appServiceSku = if ($appTierChoice -eq 1) { "F1" } else { "B1" }

# Confirmation
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "           Configuration Summary" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Resource Group:    $resourceGroup" -ForegroundColor White
Write-Host "Location:          $location" -ForegroundColor White
Write-Host "SQL Server:        $sqlServer.database.windows.net" -ForegroundColor White
Write-Host "SQL Admin:         $sqlAdmin" -ForegroundColor White
Write-Host "Database:          $database ($sqlServiceObjective)" -ForegroundColor White
Write-Host "Web App:           $webApp.azurewebsites.net" -ForegroundColor White
Write-Host "App Service Plan:  $appServiceSku" -ForegroundColor White
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
$confirm = Read-Host "Proceed with deployment? (yes/no) [yes]"
if ($confirm -eq "no") {
    Write-Host "Deployment cancelled." -ForegroundColor Yellow
    exit 0
}

# Start deployment
Write-Host ""
Write-Host "🚀 Step 3: Creating Azure Resources" -ForegroundColor Yellow
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

# Create SQL Database
Write-Host "   💾 Creating SQL Database..." -ForegroundColor Cyan
az sql db create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name $database `
  --service-objective $sqlServiceObjective `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ Database created: $database" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create database" -ForegroundColor Red
    exit 1
}

# Create App Service Plan
Write-Host "   📋 Creating App Service Plan..." -ForegroundColor Cyan
az appservice plan create `
  --name "$webApp-plan" `
  --resource-group $resourceGroup `
  --location $location `
  --sku $appServiceSku `
  --output none

if ($LASTEXITCODE -eq 0) {
    Write-Host "      ✅ App Service Plan created" -ForegroundColor Green
} else {
    Write-Host "      ❌ Failed to create App Service Plan" -ForegroundColor Red
    exit 1
}

# Create Web App
Write-Host "   🌐 Creating Web App..." -ForegroundColor Cyan
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

Write-Host "      ✅ Web App configured" -ForegroundColor Green

# Create deployment summary
$deploymentSummary = @"
╔════════════════════════════════════════════════════════════════════╗
║         iBITS Portal - Azure Deployment Summary                  ║
╚════════════════════════════════════════════════════════════════════╝

Deployment Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Subscription: $($currentSub.name)

AZURE RESOURCES CREATED:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Resource Group: $resourceGroup
   Location: $location

✅ SQL Server: $sqlServer.database.windows.net
   Admin User: $sqlAdmin
   Admin Password: ******** (saved below)

✅ SQL Database: $database
   Tier: $sqlServiceObjective
   Max Size: $(if ($sqlServiceObjective -eq 'Basic') { '2 GB' } else { '250 GB' })

✅ App Service Plan: $webApp-plan
   Tier: $appServiceSku

✅ Web App: $webApp
   URL: https://$webApp.azurewebsites.net
   Runtime: .NET 8.0

IMPORTANT CREDENTIALS:
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

NEXT STEPS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ✅ Azure resources created successfully
2. ⏭️  Import database to Azure SQL
   - Use script: 03_Import_Database.ps1
   - Or manually via SSMS

3. ⏭️  Deploy application code
   - Use Visual Studio publish
   - Or script: 04_Deploy_Application.ps1

4. ⏭️  Test your deployment
   - Visit: https://$webApp.azurewebsites.net
   - Verify login and functionality

ESTIMATED MONTHLY COST:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SQL Database ($sqlServiceObjective): $(if ($sqlServiceObjective -eq 'Basic') { '~$5' } else { '~$15' })/month
App Service ($appServiceSku): $(if ($appServiceSku -eq 'F1') { '$0' } else { '~$13' })/month
─────────────────────────────────────────────────────────────────────
Total: $(if ($appServiceSku -eq 'F1' -and $sqlServiceObjective -eq 'Basic') { '~$5' } elseif ($appServiceSku -eq 'B1' -and $sqlServiceObjective -eq 'Basic') { '~$18' } else { '~$28' })/month (approximate)

AZURE PORTAL LINKS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Resource Group:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup

SQL Server:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup/providers/Microsoft.Sql/servers/$sqlServer

Web App:
https://portal.azure.com/#@/resource/subscriptions/$($currentSub.id)/resourceGroups/$resourceGroup/providers/Microsoft.Web/sites/$webApp

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Deployment completed successfully! ✅

For next steps, see: AZURE_DEPLOYMENT_GUIDE.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"@

# Save summary
$summaryPath = "C:\Temp\iBITS_Azure_Migration\Azure_Deployment_Summary_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
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
