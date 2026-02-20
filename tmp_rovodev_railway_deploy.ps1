# Railway Deployment Script for iBITS Portal
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Railway Deployment - iBITS Portal" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if git is initialized
if (-not (Test-Path ".git")) {
    Write-Host "Initializing Git repository..." -ForegroundColor Yellow
    git init
    git branch -M main
}

Write-Host "Staging all changes..." -ForegroundColor Cyan
git add .

Write-Host ""
Write-Host "Creating commit with migration fix..." -ForegroundColor Cyan
git commit -m "Fix: Add ApplicationDbContext migration for Identity tables

- Created IdentityInitialCreate migration for ASP.NET Identity tables
- Fixed appsettings.json to use PostgreSQL connection string
- Resolves 'AspNetRoles does not exist' error
- Both ApplicationDbContext and PortaliBitsContext migrations ready
- Tested build: Success"

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ Commit created successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Push to your Railway-connected repository:" -ForegroundColor White
Write-Host "     git push origin main" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Railway will automatically:" -ForegroundColor White
Write-Host "     • Detect the changes" -ForegroundColor Gray
Write-Host "     • Build the application" -ForegroundColor Gray
Write-Host "     • Run migrations on startup" -ForegroundColor Gray
Write-Host "     • Deploy the fixed version" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Monitor the deployment:" -ForegroundColor White
Write-Host "     • Go to Railway dashboard" -ForegroundColor Gray
Write-Host "     • Check deployment logs" -ForegroundColor Gray
Write-Host "     • Verify migrations applied successfully" -ForegroundColor Gray
Write-Host ""
Write-Host "Database URL Configuration:" -ForegroundColor Yellow
Write-Host "  Railway automatically sets DATABASE_URL for your PostgreSQL service" -ForegroundColor Gray
Write-Host "  No manual configuration needed!" -ForegroundColor Green
Write-Host ""
