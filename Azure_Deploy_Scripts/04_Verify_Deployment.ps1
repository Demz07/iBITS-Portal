# ============================================================================
# iBITS Portal - Deployment Verification Script
# ============================================================================
# This script verifies your Azure deployment is working correctly
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - Verify Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get Azure resources
Write-Host "🔍 Step 1: Locating Azure resources..." -ForegroundColor Yellow

$resourceGroups = az group list --query "[].name" --output json | ConvertFrom-Json
if ($resourceGroups.Count -eq 0) {
    Write-Host "   ❌ No resource groups found." -ForegroundColor Red
    exit 1
}

Write-Host "   Available resource groups:" -ForegroundColor Cyan
for ($i = 0; $i -lt $resourceGroups.Count; $i++) {
    Write-Host "      [$i] $($resourceGroups[$i])" -ForegroundColor Gray
}

$rgSelection = Read-Host "   Select resource group number [0]"
if ([string]::IsNullOrWhiteSpace($rgSelection)) { $rgSelection = 0 }
$resourceGroup = $resourceGroups[$rgSelection]

# Get SQL Server
$sqlServers = az sql server list --resource-group $resourceGroup --query "[].name" --output json | ConvertFrom-Json
if ($sqlServers.Count -eq 0) {
    Write-Host "   ❌ No SQL servers found." -ForegroundColor Red
    exit 1
}
$sqlServer = $sqlServers[0]

# Get Database
$databases = az sql db list --resource-group $resourceGroup --server $sqlServer --query "[?name!='master'].name" --output json | ConvertFrom-Json
if ($databases.Count -eq 0) {
    Write-Host "   ❌ No databases found." -ForegroundColor Red
    exit 1
}
$database = $databases[0]

# Get Web App
$webApps = az webapp list --resource-group $resourceGroup --query "[].name" --output json | ConvertFrom-Json
if ($webApps.Count -eq 0) {
    Write-Host "   ❌ No web apps found." -ForegroundColor Red
    exit 1
}
$webApp = $webApps[0]

Write-Host "   ✅ Found resources:" -ForegroundColor Green
Write-Host "      Resource Group: $resourceGroup" -ForegroundColor Cyan
Write-Host "      SQL Server: $sqlServer" -ForegroundColor Cyan
Write-Host "      Database: $database" -ForegroundColor Cyan
Write-Host "      Web App: $webApp" -ForegroundColor Cyan

# Test 1: Check Web App Status
Write-Host ""
Write-Host "🧪 Test 1: Web App Status" -ForegroundColor Yellow
$appState = az webapp show --name $webApp --resource-group $resourceGroup --query state -o tsv

if ($appState -eq "Running") {
    Write-Host "   ✅ Web app is running" -ForegroundColor Green
} else {
    Write-Host "   ❌ Web app state: $appState" -ForegroundColor Red
}

# Test 2: Check Web App Accessibility
Write-Host ""
Write-Host "🧪 Test 2: Web App Accessibility" -ForegroundColor Yellow
$appUrl = "https://$webApp.azurewebsites.net"

try {
    $response = Invoke-WebRequest -Uri $appUrl -Method Get -TimeoutSec 30 -UseBasicParsing 2>$null
    
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Web app is accessible (HTTP 200 OK)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Web app responded with: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Could not reach web app: $_" -ForegroundColor Red
}

# Test 3: Check Database Status
Write-Host ""
Write-Host "🧪 Test 3: Database Status" -ForegroundColor Yellow
$dbStatus = az sql db show --name $database --server $sqlServer --resource-group $resourceGroup --query status -o tsv

if ($dbStatus -eq "Online") {
    Write-Host "   ✅ Database is online" -ForegroundColor Green
} else {
    Write-Host "   ❌ Database status: $dbStatus" -ForegroundColor Red
}

# Test 4: Check Connection String Configuration
Write-Host ""
Write-Host "🧪 Test 4: Connection String Configuration" -ForegroundColor Yellow
$connStrings = az webapp config connection-string list --name $webApp --resource-group $resourceGroup --output json | ConvertFrom-Json

if ($connStrings.PSObject.Properties.Name -contains "DefaultConnection") {
    Write-Host "   ✅ Connection string configured" -ForegroundColor Green
} else {
    Write-Host "   ❌ Connection string not found" -ForegroundColor Red
}

# Test 5: Check HTTPS Configuration
Write-Host ""
Write-Host "🧪 Test 5: HTTPS Configuration" -ForegroundColor Yellow
$httpsOnly = az webapp show --name $webApp --resource-group $resourceGroup --query httpsOnly -o tsv

if ($httpsOnly -eq "true") {
    Write-Host "   ✅ HTTPS-only mode enabled" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  HTTPS-only mode not enabled" -ForegroundColor Yellow
}

# Test 6: Check SQL Firewall Rules
Write-Host ""
Write-Host "🧪 Test 6: SQL Firewall Configuration" -ForegroundColor Yellow
$firewallRules = az sql server firewall-rule list --resource-group $resourceGroup --server $sqlServer --query "length([?name!='AllowAllWindowsAzureIps'])" -o tsv

if ([int]$firewallRules -gt 0) {
    Write-Host "   ✅ Firewall rules configured ($firewallRules rules)" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  No custom firewall rules found" -ForegroundColor Yellow
}

# Test 7: Check Application Logs
Write-Host ""
Write-Host "🧪 Test 7: Recent Application Logs" -ForegroundColor Yellow
Write-Host "   Checking last 10 log entries..." -ForegroundColor Gray

# Enable logging temporarily
az webapp log config --name $webApp --resource-group $resourceGroup --application-logging filesystem --level information --output none 2>$null

Start-Sleep -Seconds 2

try {
    $logs = az webapp log download --name $webApp --resource-group $resourceGroup --log-file "temp_logs.zip" 2>$null
    
    if (Test-Path "temp_logs.zip") {
        Write-Host "   ✅ Logs accessible" -ForegroundColor Green
        Remove-Item "temp_logs.zip" -Force
    } else {
        Write-Host "   ℹ️  No logs available yet" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ℹ️  Logs check skipped" -ForegroundColor Gray
}

# Test 8: Performance Check
Write-Host ""
Write-Host "🧪 Test 8: Response Time Check" -ForegroundColor Yellow

try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $response = Invoke-WebRequest -Uri $appUrl -Method Get -TimeoutSec 30 -UseBasicParsing 2>$null
    $stopwatch.Stop()
    
    $responseTime = $stopwatch.ElapsedMilliseconds
    
    if ($responseTime -lt 1000) {
        Write-Host "   ✅ Fast response: $responseTime ms" -ForegroundColor Green
    } elseif ($responseTime -lt 3000) {
        Write-Host "   ⚠️  Moderate response: $responseTime ms" -ForegroundColor Yellow
    } else {
        Write-Host "   ⚠️  Slow response: $responseTime ms" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Could not measure response time" -ForegroundColor Yellow
}

# Generate Report
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "           Verification Report" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resource Group: $resourceGroup" -ForegroundColor White
Write-Host "SQL Server: $sqlServer.database.windows.net" -ForegroundColor White
Write-Host "Database: $database" -ForegroundColor White
Write-Host "Web App: $appUrl" -ForegroundColor White
Write-Host ""
Write-Host "Status Summary:" -ForegroundColor Cyan
Write-Host "  Web App State: $appState" -ForegroundColor White
Write-Host "  Database Status: $dbStatus" -ForegroundColor White
Write-Host "  HTTPS Only: $httpsOnly" -ForegroundColor White
Write-Host "  Firewall Rules: $firewallRules" -ForegroundColor White
Write-Host ""

# Final recommendations
Write-Host "📋 Recommendations:" -ForegroundColor Yellow
Write-Host ""

if ($appState -ne "Running") {
    Write-Host "   ⚠️  Start your web app:" -ForegroundColor Yellow
    Write-Host "      az webapp start --name $webApp --resource-group $resourceGroup" -ForegroundColor Gray
}

if ($httpsOnly -ne "true") {
    Write-Host "   🔒 Enable HTTPS-only mode:" -ForegroundColor Yellow
    Write-Host "      az webapp update --name $webApp --resource-group $resourceGroup --https-only true" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔍 Additional Checks:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   View real-time logs:" -ForegroundColor White
Write-Host "      az webapp log tail --name $webApp --resource-group $resourceGroup" -ForegroundColor Gray
Write-Host ""
Write-Host "   Test database connection (requires credentials):" -ForegroundColor White
Write-Host "      sqlcmd -S $sqlServer.database.windows.net -d $database -U [username] -P [password] -Q 'SELECT COUNT(*) FROM Student'" -ForegroundColor Gray
Write-Host ""
Write-Host "   Open Azure Portal:" -ForegroundColor White
Write-Host "      https://portal.azure.com/#@/resource/subscriptions/.../resourceGroups/$resourceGroup" -ForegroundColor Gray
Write-Host ""

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Verification complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to open your application..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process $appUrl
