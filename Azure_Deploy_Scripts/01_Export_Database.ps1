# ============================================================================
# iBITS Portal - Database Export Script for Azure Migration
# ============================================================================
# This script exports your local PortaliBITS database to prepare for Azure SQL
# ============================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  iBITS Portal - Database Export" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$localServer = "DESKTOP-SG3AI25\SQLEXPRESS"
$databaseName = "PortaliBITS"
$backupPath = "C:\Temp\iBITS_Azure_Migration"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = "$backupPath\PortaliBITS_Backup_$timestamp.bak"
$scriptFile = "$backupPath\PortaliBITS_Complete_$timestamp.sql"

# Create backup directory
Write-Host "📁 Creating backup directory..." -ForegroundColor Yellow
if (-not (Test-Path $backupPath)) {
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    Write-Host "   ✅ Directory created: $backupPath" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  Directory already exists" -ForegroundColor Gray
}

# Step 1: Create database backup
Write-Host ""
Write-Host "💾 Step 1: Creating database backup..." -ForegroundColor Yellow
try {
    $backupQuery = @"
BACKUP DATABASE [$databaseName] 
TO DISK = '$backupFile' 
WITH FORMAT, 
     INIT, 
     NAME = 'Full Backup of $databaseName for Azure Migration',
     COMPRESSION,
     STATS = 10;
"@
    
    sqlcmd -S $localServer -E -Q $backupQuery
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Backup created successfully!" -ForegroundColor Green
        $fileSize = (Get-Item $backupFile).Length / 1MB
        Write-Host "   📊 Backup size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
        Write-Host "   📂 Location: $backupFile" -ForegroundColor Cyan
    } else {
        throw "Backup failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Host "   ❌ Error creating backup: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Generate complete SQL script
Write-Host ""
Write-Host "📜 Step 2: Generating SQL script..." -ForegroundColor Yellow
Write-Host "   ℹ️  This may take a few minutes for large databases..." -ForegroundColor Gray

# Create script generation SQL command
$scriptGeneration = @"
-- Generate database statistics
SELECT 
    'Database: $databaseName' as Info,
    (SELECT COUNT(*) FROM Student) as Students,
    (SELECT COUNT(*) FROM Officer) as Officers,
    (SELECT COUNT(*) FROM Fee) as Fees,
    (SELECT COUNT(*) FROM Fine) as Fines,
    (SELECT COUNT(*) FROM [Event]) as Events,
    (SELECT COUNT(*) FROM Attendance) as Attendance,
    (SELECT COUNT(*) FROM Remittance) as Remittances,
    (SELECT COUNT(*) FROM AspNetUsers) as Users;
"@

try {
    sqlcmd -S $localServer -E -d $databaseName -Q $scriptGeneration -o "$backupPath\Database_Stats_$timestamp.txt" -W
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Database statistics generated" -ForegroundColor Green
    }
} catch {
    Write-Host "   ⚠️  Warning: Could not generate statistics" -ForegroundColor Yellow
}

# Step 3: Export schema and data using BCP (if needed)
Write-Host ""
Write-Host "📋 Step 3: Exporting table data..." -ForegroundColor Yellow

$tables = @("Student", "Officer", "Fee", "Fine", "Event", "Attendance", "Remittance", "RemittanceItem", 
            "Announcement", "ActivityLog", "SystemSetting", "AspNetUsers", "AspNetRoles", "AspNetUserRoles")

$exportCount = 0
foreach ($table in $tables) {
    try {
        $csvFile = "$backupPath\Data_$table`_$timestamp.csv"
        $query = "SELECT * FROM [$table]"
        
        sqlcmd -S $localServer -E -d $databaseName -Q $query -o $csvFile -s "," -W -h -1 2>$null
        
        if (Test-Path $csvFile) {
            $exportCount++
        }
    } catch {
        # Skip tables that don't exist
    }
}

Write-Host "   ✅ Exported $exportCount tables to CSV format" -ForegroundColor Green

# Step 4: Create summary report
Write-Host ""
Write-Host "📊 Step 4: Creating migration summary..." -ForegroundColor Yellow

$summary = @"
╔════════════════════════════════════════════════════════════════════╗
║         iBITS Portal - Azure Migration Export Summary            ║
╚════════════════════════════════════════════════════════════════════╝

Export Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Source Server: $localServer
Database: $databaseName

FILES GENERATED:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. Database Backup (.bak)
   📂 $backupFile
   📊 Size: $([math]::Round((Get-Item $backupFile).Length / 1MB, 2)) MB
   
   Use this file to restore to Azure SQL using:
   - SQL Server Management Studio (SSMS)
   - Azure Data Studio
   - Import/Export wizard

2. Database Statistics
   📂 $backupPath\Database_Stats_$timestamp.txt
   
   Review this to verify all data is exported correctly.

3. CSV Data Exports
   📂 $backupPath\Data_*.csv
   
   $exportCount table(s) exported for verification/backup

NEXT STEPS:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. ✅ Database exported successfully
2. ⏭️  Create Azure SQL Database (see AZURE_DEPLOYMENT_GUIDE.md)
3. ⏭️  Import backup to Azure SQL
4. ⏭️  Update connection string in App Service
5. ⏭️  Deploy application to Azure

IMPORT TO AZURE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Method 1 - Using SSMS:
  1. Connect to: your-server.database.windows.net
  2. Right-click Databases → Import Data-tier Application
  3. Select: $backupFile
  4. Follow wizard

Method 2 - Using sqlcmd:
  sqlcmd -S "your-server.database.windows.net" \
         -U "your-admin" -P "your-password" \
         -d "PortaliBITS" \
         -i "script.sql"

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

For complete deployment guide, see:
📖 AZURE_DEPLOYMENT_GUIDE.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
"@

$summary | Out-File "$backupPath\EXPORT_SUMMARY_$timestamp.txt" -Encoding UTF8
Write-Host $summary -ForegroundColor Cyan

Write-Host ""
Write-Host "✅ Export completed successfully!" -ForegroundColor Green
Write-Host "📂 All files saved to: $backupPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to open the backup folder..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Start-Process explorer.exe $backupPath
