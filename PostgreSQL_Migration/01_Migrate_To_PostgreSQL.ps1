# ========================================
# PostgreSQL Migration Script
# Migrates iBITS Portal from SQL Server to PostgreSQL
# ========================================

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - PostgreSQL Migration" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"

# Step 1: Backup current Program.cs
Write-Host "[Step 1/6] Backing up current Program.cs..." -ForegroundColor Yellow
Copy-Item "$projectPath\Program.cs" "$projectPath\Program.cs.sqlserver.backup" -Force
Write-Host "✅ Backup created: Program.cs.sqlserver.backup" -ForegroundColor Green
Write-Host ""

# Step 2: Remove SQL Server package
Write-Host "[Step 2/6] Removing SQL Server package..." -ForegroundColor Yellow
Set-Location $projectPath
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer
Write-Host "✅ SQL Server package removed" -ForegroundColor Green
Write-Host ""

# Step 3: Add PostgreSQL package
Write-Host "[Step 3/6] Adding PostgreSQL package..." -ForegroundColor Yellow
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
Write-Host "✅ PostgreSQL package added" -ForegroundColor Green
Write-Host ""

# Step 4: Update Program.cs
Write-Host "[Step 4/6] Updating Program.cs for PostgreSQL..." -ForegroundColor Yellow
$newProgramCs = Get-Content "..\PostgreSQL_Migration\Program_PostgreSQL.cs" -Raw
Set-Content "$projectPath\Program.cs" $newProgramCs
Write-Host "✅ Program.cs updated" -ForegroundColor Green
Write-Host ""

# Step 5: Delete old migrations
Write-Host "[Step 5/6] Cleaning old migrations..." -ForegroundColor Yellow
$confirmDelete = Read-Host "Delete old SQL Server migrations? (y/n)"
if ($confirmDelete -eq 'y') {
    Remove-Item "$projectPath\Migrations\*" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Old migrations deleted" -ForegroundColor Green
} else {
    Write-Host "⏭️  Skipped migration cleanup" -ForegroundColor Yellow
}
Write-Host ""

# Step 6: Create new PostgreSQL migrations
Write-Host "[Step 6/6] Creating new PostgreSQL migrations..." -ForegroundColor Yellow
Write-Host "Run these commands manually:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  cd `"$projectPath`"" -ForegroundColor White
Write-Host "  dotnet ef migrations add InitialPostgreSQL" -ForegroundColor White
Write-Host "  dotnet ef database update" -ForegroundColor White
Write-Host ""

Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Migration Preparation Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Install PostgreSQL locally to test" -ForegroundColor White
Write-Host "2. Update connection string in appsettings.json" -ForegroundColor White
Write-Host "3. Run migrations (commands shown above)" -ForegroundColor White
Write-Host "4. Test locally before deploying to Railway" -ForegroundColor White
Write-Host ""
