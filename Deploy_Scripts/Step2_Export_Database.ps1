# ========================================
# Step 2: Export Database to CSV
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Step 2: Export Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection details
$server = "DESKTOP-SG3AI25\SQLEXPRESS"
$database = "PortaliBITS"

# Create export directory
$exportDir = "C:\Users\Dave\source\repos\iBITS Portal\DatabaseExport_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
New-Item -ItemType Directory -Path $exportDir -Force | Out-Null

Write-Host "Export directory: $exportDir" -ForegroundColor Cyan
Write-Host ""

# Tables to export (in correct order for foreign key dependencies)
$tables = @(
    "AspNetRoles",
    "AspNetUsers",
    "AspNetUserRoles",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserTokens",
    "AspNetRoleClaims",
    "Student",
    "Officer",
    "Event",
    "Fee",
    "Fine",
    "Attendance",
    "Announcement",
    "UserAnnouncementDismissal",
    "Remittance",
    "RemittanceItem",
    "PaymentTransaction",
    "ActivityLog",
    "PendingRoleChange",
    "SystemSetting",
    "ArchivedStudent",
    "ArchivedOfficer",
    "ArchivedEvent"
)

$successCount = 0
$failCount = 0

Write-Host "Starting export of $($tables.Count) tables..." -ForegroundColor Yellow
Write-Host ""

foreach ($table in $tables) {
    try {
        $outputFile = Join-Path $exportDir "$table.csv"
        
        # Create BCP command to export to CSV
        $bcpCommand = "bcp `"SELECT * FROM [$database].dbo.[$table]`" queryout `"$outputFile`" -S `"$server`" -T -c -t`",`" -r`"\n`""
        
        Write-Host "Exporting $table..." -NoNewline
        
        # Execute BCP command
        $result = Invoke-Expression $bcpCommand 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            $fileInfo = Get-Item $outputFile
            Write-Host " OK ($([math]::Round($fileInfo.Length/1KB, 2)) KB)" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host " SKIPPED (empty or error)" -ForegroundColor Yellow
            $failCount++
        }
    }
    catch {
        Write-Host " ERROR: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Export Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Success: $successCount tables" -ForegroundColor Green
Write-Host "  Skipped/Failed: $failCount tables" -ForegroundColor Yellow
Write-Host ""
Write-Host "Export location:" -ForegroundColor Cyan
Write-Host "  $exportDir" -ForegroundColor Yellow
Write-Host ""

# Create import instructions
$importInstructions = @"
========================================
DATABASE IMPORT INSTRUCTIONS
========================================

Your database has been exported to CSV files.
These files are ready to import into Railway PostgreSQL.

EXPORT LOCATION:
$exportDir

TABLES EXPORTED: $successCount tables

========================================
HOW TO IMPORT TO RAILWAY
========================================

METHOD 1: Using Railway Dashboard (Easiest)
--------------------------------------------
1. Deploy your app to Railway first
2. Add PostgreSQL database service
3. Go to PostgreSQL service
4. Click 'Data' tab
5. Use 'Import' feature to upload CSV files

METHOD 2: Using psql Command Line
----------------------------------
1. Get your Railway PostgreSQL connection string
2. Install psql (PostgreSQL client)
3. Connect: psql [your-railway-database-url]
4. For each CSV file, run:
   \copy table_name FROM 'file.csv' WITH (FORMAT csv, HEADER true);

METHOD 3: Using DBeaver (Recommended)
--------------------------------------
1. Download DBeaver: https://dbeaver.io/download/
2. Create new PostgreSQL connection using Railway credentials
3. Right-click table -> Import Data
4. Select CSV file and map columns
5. Execute import

========================================
IMPORTANT NOTES
========================================

1. Import tables in the ORDER listed above (due to foreign keys)
2. You may need to disable foreign key checks during import
3. After import, verify row counts match your original database
4. Some identity columns may need to be reset

========================================
TABLE ORDER (Import in this sequence)
========================================
"@

$tableOrder = ""
for ($i = 0; $i -lt $tables.Count; $i++) {
    $tableOrder += "$($i + 1). $($tables[$i])`n"
}

$importInstructions += "`n" + $tableOrder

# Save import instructions
$instructionsFile = Join-Path $exportDir "IMPORT_INSTRUCTIONS.txt"
Set-Content $instructionsFile -Value $importInstructions

Write-Host "Created IMPORT_INSTRUCTIONS.txt in export folder" -ForegroundColor Green
Write-Host ""
Write-Host "Next Step: Run Step3_Setup_Git.ps1" -ForegroundColor Green
Write-Host ""
Write-Host "Command: " -NoNewline
Write-Host "powershell -ExecutionPolicy Bypass -File Step3_Setup_Git.ps1" -ForegroundColor Yellow
Write-Host ""
