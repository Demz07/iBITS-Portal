# ============================================================================
# iBITS Portal - Application Deployment Script
# ============================================================================
# This script deploys your iBITS Portal application to Azure App Service
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - Deploy to Azure" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$projectPath = "C:\Users\Dave\source\repos\iBITS Portal"
$projectFile = "$projectPath\iBITS Portal\iBITS Portal.csproj"
$publishPath = "$projectPath\publish"
$deployZip = "$projectPath\deploy.zip"

# Check if project exists
if (-not (Test-Path $projectFile)) {
    Write-Host "❌ Project file not found: $projectFile" -ForegroundColor Red
    exit 1
}

# Get Azure resources
Write-Host "🔍 Step 1: Getting Azure resources..." -ForegroundColor Yellow

$resourceGroups = az group list --query "[].name" --output json | ConvertFrom-Json
if ($resourceGroups.Count -eq 0) {
    Write-Host "   ❌ No resource groups found. Run 02_Create_Azure_Resources.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "   Available resource groups:" -ForegroundColor Cyan
for ($i = 0; $i -lt $resourceGroups.Count; $i++) {
    Write-Host "      [$i] $($resourceGroups[$i])" -ForegroundColor Gray
}

$rgSelection = Read-Host "   Select resource group number [0]"
if ([string]::IsNullOrWhiteSpace($rgSelection)) { $rgSelection = 0 }
$resourceGroup = $resourceGroups[$rgSelection]

# Get web apps in resource group
$webApps = az webapp list --resource-group $resourceGroup --query "[].name" --output json | ConvertFrom-Json

if ($webApps.Count -eq 0) {
    Write-Host "   ❌ No web apps found in resource group: $resourceGroup" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "   Available web apps:" -ForegroundColor Cyan
for ($i = 0; $i -lt $webApps.Count; $i++) {
    Write-Host "      [$i] $($webApps[$i])" -ForegroundColor Gray
}

$appSelection = Read-Host "   Select web app number [0]"
if ([string]::IsNullOrWhiteSpace($appSelection)) { $appSelection = 0 }
$webAppName = $webApps[$appSelection]

Write-Host "   ✅ Target: $webAppName in $resourceGroup" -ForegroundColor Green

# Clean previous build
Write-Host ""
Write-Host "🧹 Step 2: Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path $publishPath) {
    Remove-Item -Recurse -Force $publishPath
    Write-Host "   ✅ Cleaned publish folder" -ForegroundColor Green
}
if (Test-Path $deployZip) {
    Remove-Item -Force $deployZip
    Write-Host "   ✅ Cleaned deployment package" -ForegroundColor Green
}

# Build and publish
Write-Host ""
Write-Host "🔨 Step 3: Building application..." -ForegroundColor Yellow
Write-Host "   This may take 1-2 minutes..." -ForegroundColor Gray

$buildOutput = dotnet publish $projectFile -c Release -o $publishPath 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build completed successfully" -ForegroundColor Green
    
    # Count files
    $fileCount = (Get-ChildItem -Path $publishPath -Recurse -File).Count
    Write-Host "   📦 Files generated: $fileCount" -ForegroundColor Cyan
} else {
    Write-Host "   ❌ Build failed!" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Red
    exit 1
}

# Create deployment package
Write-Host ""
Write-Host "📦 Step 4: Creating deployment package..." -ForegroundColor Yellow

try {
    Compress-Archive -Path "$publishPath\*" -DestinationPath $deployZip -Force
    
    $zipSize = (Get-Item $deployZip).Length / 1MB
    Write-Host "   ✅ Package created: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Failed to create package: $_" -ForegroundColor Red
    exit 1
}

# Deploy to Azure
Write-Host ""
Write-Host "🚀 Step 5: Deploying to Azure..." -ForegroundColor Yellow
Write-Host "   Target: https://$webAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "   This may take 2-3 minutes..." -ForegroundColor Gray
Write-Host ""

az webapp deploy `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src-path $deployZip `
  --type zip `
  --async false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "   ✅ Deployment completed successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "   ❌ Deployment failed!" -ForegroundColor Red
    Write-Host "   Check logs: az webapp log tail --name $webAppName --resource-group $resourceGroup" -ForegroundColor Yellow
    exit 1
}

# Wait for app to start
Write-Host ""
Write-Host "⏳ Step 6: Waiting for application to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test deployment
Write-Host ""
Write-Host "🧪 Step 7: Testing deployment..." -ForegroundColor Yellow

$appUrl = "https://$webAppName.azurewebsites.net"
try {
    $response = Invoke-WebRequest -Uri $appUrl -Method Head -TimeoutSec 30 -UseBasicParsing 2>$null
    
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Application is responding!" -ForegroundColor Green
        Write-Host "   🌐 Status: $($response.StatusCode) - OK" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ⚠️  Application may still be starting..." -ForegroundColor Yellow
    Write-Host "   Please check manually: $appUrl" -ForegroundColor Cyan
}

# Create deployment summary
$summary = @"
╔════════════════════════════════════════════════════════════════════╗
║         iBITS Portal - Deployment Summary                        ║
╚════════════════════════════════════════════════════════════════════╝

Deployment Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

DEPLOYMENT DETAILS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Application deployed successfully!

Resource Group: $resourceGroup
Web App: $webAppName
URL: $appUrl

Build Configuration: Release
.NET Version: 8.0
Files Deployed: $fileCount
Package Size: $([math]::Round($zipSize, 2)) MB

NEXT STEPS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ✅ Application deployed
2. ⏭️  Open application: $appUrl
3. ⏭️  Test login functionality
4. ⏭️  Verify database connectivity
5. ⏭️  Check all features work correctly

TROUBLESHOOTING:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

View logs:
  az webapp log tail --name $webAppName --resource-group $resourceGroup

Restart app:
  az webapp restart --name $webAppName --resource-group $resourceGroup

SSH into container:
  az webapp ssh --name $webAppName --resource-group $resourceGroup

MONITORING:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Azure Portal:
https://portal.azure.com/#@/resource/subscriptions/.../providers/Microsoft.Web/sites/$webAppName

App Service Logs:
https://portal.azure.com/#@/resource/.../providers/Microsoft.Web/sites/$webAppName/logStream

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🎉 Deployment complete! Your iBITS Portal is now live on Azure!

Visit: $appUrl

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"@

Write-Host ""
Write-Host $summary -ForegroundColor Cyan
Write-Host ""

# Save summary
$summaryPath = "C:\Temp\iBITS_Azure_Migration\Deployment_Summary_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
if (-not (Test-Path "C:\Temp\iBITS_Azure_Migration")) {
    New-Item -ItemType Directory -Path "C:\Temp\iBITS_Azure_Migration" -Force | Out-Null
}
$summary | Out-File $summaryPath -Encoding UTF8

Write-Host "📄 Summary saved to: $summaryPath" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to open your deployed application..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process $appUrl
