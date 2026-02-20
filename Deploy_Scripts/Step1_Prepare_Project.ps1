# ========================================
# Step 1: Prepare Project for Railway Deployment
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Step 1: Prepare Project for Railway" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$projectDir = "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"

if (-not (Test-Path $projectDir)) {
    Write-Host "ERROR: Project directory not found!" -ForegroundColor Red
    Write-Host "   Expected: $projectDir" -ForegroundColor Yellow
    exit 1
}

Set-Location $projectDir
Write-Host "Found project at: $projectDir" -ForegroundColor Green
Write-Host ""

# Step 1: Create backup of important files
Write-Host "Step 1/6: Creating backup of important files..." -ForegroundColor Yellow

$backupDir = Join-Path $projectDir "BACKUP_BEFORE_RAILWAY_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

$filesToBackup = @(
    "Program.cs",
    "appsettings.json",
    "iBITS Portal.csproj"
)

foreach ($file in $filesToBackup) {
    if (Test-Path $file) {
        Copy-Item $file -Destination $backupDir
        Write-Host "   Backed up: $file" -ForegroundColor Green
    }
}

Write-Host "   Backup saved to: $backupDir" -ForegroundColor Cyan
Write-Host ""

# Step 2: Add PostgreSQL packages
Write-Host "Step 2/6: Adding PostgreSQL packages..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray

# Remove SQL Server packages
Write-Host "   Removing SQL Server package..." -ForegroundColor Gray
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer 2>&1 | Out-Null

# Add PostgreSQL packages
Write-Host "   Adding PostgreSQL package..." -ForegroundColor Gray
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0

Write-Host "   PostgreSQL packages added!" -ForegroundColor Green
Write-Host ""

# Step 3: Update Program.cs for PostgreSQL
Write-Host "Step 3/6: Updating Program.cs for PostgreSQL..." -ForegroundColor Yellow

$programCs = Get-Content "Program.cs" -Raw

# Replace SQL Server with PostgreSQL
$programCs = $programCs -replace 'UseSqlServer', 'UseNpgsql'

# Save updated Program.cs
Set-Content "Program.cs" -Value $programCs

Write-Host "   Program.cs updated!" -ForegroundColor Green
Write-Host ""

# Step 4: Create Railway configuration files
Write-Host "Step 4/6: Creating Railway configuration files..." -ForegroundColor Yellow

# Create railway.json
$railwayJson = @'
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "dotnet iBITS Portal.dll",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
'@

Set-Content "railway.json" -Value $railwayJson
Write-Host "   Created railway.json" -ForegroundColor Green

# Create nixpacks.toml for build configuration
$nixpacksToml = @'
[phases.setup]
nixPkgs = ["dotnet-sdk_8"]

[phases.build]
cmds = ["dotnet publish -c Release -o out"]

[start]
cmd = "cd out; dotnet 'iBITS Portal.dll'"
'@

Set-Content "nixpacks.toml" -Value $nixpacksToml
Write-Host "   Created nixpacks.toml" -ForegroundColor Green

# Create appsettings.Production.json for Railway
$dbUrlPlaceholder = '${DATABASE_URL}'
$appsettingsProduction = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "$dbUrlPlaceholder"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@

Set-Content "appsettings.Production.json" -Value $appsettingsProduction
Write-Host "   Created appsettings.Production.json" -ForegroundColor Green

# Create .env.example for reference
$envExample = @'
# Railway Environment Variables (for reference)
# These will be set automatically by Railway

DATABASE_URL=postgresql://user:password@host:5432/database
ASPNETCORE_ENVIRONMENT=Production
PORT=8080
'@

Set-Content ".env.example" -Value $envExample
Write-Host "   Created .env.example" -ForegroundColor Green
Write-Host ""

# Step 5: Update .gitignore
Write-Host "Step 5/6: Updating .gitignore..." -ForegroundColor Yellow

$gitignorePath = Join-Path $projectDir ".gitignore"
if (-not (Test-Path $gitignorePath)) {
    $parentGitignore = Join-Path (Split-Path $projectDir -Parent) ".gitignore"
    if (Test-Path $parentGitignore) {
        Copy-Item $parentGitignore -Destination $gitignorePath
        Write-Host "   Copied .gitignore from parent directory" -ForegroundColor Green
    } else {
        $gitignoreContent = @'
bin/
obj/
.vs/
*.user
*.bak
appsettings.Development.json
DatabaseExport/
BACKUP_*/
wwwroot/uploads/
wwwroot/temp_uploads/
.env
'@
        Set-Content $gitignorePath -Value $gitignoreContent
        Write-Host "   Created new .gitignore" -ForegroundColor Green
    }
} else {
    Write-Host "   .gitignore already exists" -ForegroundColor Green
}
Write-Host ""

# Step 6: Test build
Write-Host "Step 6/6: Testing build..." -ForegroundColor Yellow
Write-Host "   Running dotnet restore..." -ForegroundColor Gray

$restoreResult = dotnet restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   Build test passed!" -ForegroundColor Green
} else {
    Write-Host "   Warning: Build had some issues, but continuing..." -ForegroundColor Yellow
    Write-Host "   You may need to fix these before deploying" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Step 1 Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "What was done:" -ForegroundColor Cyan
Write-Host "   Backed up original files" -ForegroundColor White
Write-Host "   Added PostgreSQL packages" -ForegroundColor White
Write-Host "   Updated Program.cs for PostgreSQL" -ForegroundColor White
Write-Host "   Created Railway configuration files" -ForegroundColor White
Write-Host "   Updated .gitignore" -ForegroundColor White
Write-Host "   Tested build" -ForegroundColor White
Write-Host ""
Write-Host "Files created:" -ForegroundColor Cyan
Write-Host "   - railway.json" -ForegroundColor White
Write-Host "   - nixpacks.toml" -ForegroundColor White
Write-Host "   - appsettings.Production.json" -ForegroundColor White
Write-Host "   - .env.example" -ForegroundColor White
Write-Host "   - .gitignore (if missing)" -ForegroundColor White
Write-Host ""
Write-Host "Backup location:" -ForegroundColor Cyan
Write-Host "   $backupDir" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next Step: Run Step2_Export_Database.ps1" -ForegroundColor Green
Write-Host ""
Write-Host "Command: " -NoNewline
Write-Host "powershell -ExecutionPolicy Bypass -File Step2_Export_Database.ps1" -ForegroundColor Yellow
Write-Host ""
