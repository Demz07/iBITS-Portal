# Test script to verify database connection and migrations
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "iBITS Portal - Database Setup Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if DATABASE_URL is set
if ($env:DATABASE_URL) {
    Write-Host "✓ DATABASE_URL environment variable is set" -ForegroundColor Green
    Write-Host "  Connection: $($env:DATABASE_URL.Substring(0, 30))..." -ForegroundColor Gray
} else {
    Write-Host "✗ DATABASE_URL environment variable is NOT set" -ForegroundColor Red
    Write-Host "  Please set DATABASE_URL before running the application" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Example:" -ForegroundColor Yellow
    Write-Host '  $env:DATABASE_URL = "postgresql://user:password@host:port/database"' -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "Building the application..." -ForegroundColor Cyan
dotnet build --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build successful" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Applying ApplicationDbContext migrations (Identity tables)..." -ForegroundColor Cyan
dotnet ef database update --context ApplicationDbContext
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ ApplicationDbContext migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "✗ ApplicationDbContext migrations failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Applying PortaliBitsContext migrations (Business tables)..." -ForegroundColor Cyan
dotnet ef database update --context PortaliBitsContext
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ PortaliBitsContext migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "✗ PortaliBitsContext migrations failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ All migrations applied successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "You can now run the application with:" -ForegroundColor Cyan
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
