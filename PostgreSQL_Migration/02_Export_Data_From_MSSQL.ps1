# ========================================
# Export Data from SQL Server
# Creates CSV files for migration to PostgreSQL
# ========================================

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Export SQL Server Data to CSV" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$server = "DESKTOP-SG3AI25\SQLEXPRESS"
$database = "PortaliBITS"
$outputFolder = "C:\Users\Dave\source\repos\iBITS Portal\PostgreSQL_Migration\ExportedData"

# Create output folder
if (!(Test-Path $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
}

Write-Host "Output folder: $outputFolder" -ForegroundColor Cyan
Write-Host ""

# Tables to export
$tables = @(
    "Student",
    "Officer",
    "Event",
    "Fee",
    "Fine",
    "Attendance",
    "Remittance",
    "RemittanceItem",
    "Announcement",
    "ActivityLog",
    "PaymentTransaction",
    "PendingRoleChange",
    "SystemSetting",
    "UserAnnouncementDismissal"
)

Write-Host "Exporting $($tables.Count) tables..." -ForegroundColor Yellow
Write-Host ""

foreach ($table in $tables) {
    Write-Host "  Exporting: $table..." -ForegroundColor White
    
    $query = "SELECT * FROM [$table]"
    $outputFile = Join-Path $outputFolder "$table.csv"
    
    try {
        $result = sqlcmd -S $server -d $database -E -Q $query -s "," -W -w 8000 -o $outputFile
        
        if (Test-Path $outputFile) {
            $lineCount = (Get-Content $outputFile).Count
            Write-Host "    ✅ Exported: $lineCount rows" -ForegroundColor Green
        } else {
            Write-Host "    ⚠️  Warning: File not created" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "    ❌ Error: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Export Complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Files saved to: $outputFolder" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: Use pgAdmin or DBeaver to import these CSV files into PostgreSQL" -ForegroundColor Yellow
Write-Host ""
