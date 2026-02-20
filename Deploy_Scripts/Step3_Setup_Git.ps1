# ========================================
# Step 3: Setup Git Repository
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Step 3: Setup Git Repository" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to project root (parent of iBITS Portal folder)
$projectRoot = "C:\Users\Dave\source\repos\iBITS Portal"

if (-not (Test-Path $projectRoot)) {
    Write-Host "ERROR: Project directory not found!" -ForegroundColor Red
    exit 1
}

Set-Location $projectRoot
Write-Host "Working directory: $projectRoot" -ForegroundColor Cyan
Write-Host ""

# Check if Git is installed
$gitInstalled = Get-Command git -ErrorAction SilentlyContinue
if (-not $gitInstalled) {
    Write-Host "ERROR: Git is not installed!" -ForegroundColor Red
    Write-Host "Please install Git from: https://git-scm.com/download/win" -ForegroundColor Yellow
    exit 1
}

Write-Host "Git version: " -NoNewline
git --version
Write-Host ""

# Check if already a git repository
if (Test-Path ".git") {
    Write-Host "Git repository already exists!" -ForegroundColor Yellow
    Write-Host "Do you want to:" -ForegroundColor Yellow
    Write-Host "  1. Use existing repository" -ForegroundColor White
    Write-Host "  2. Reinitialize (WARNING: This will reset git history)" -ForegroundColor White
    Write-Host ""
    $choice = Read-Host "Enter choice (1 or 2)"
    
    if ($choice -eq "2") {
        Write-Host "Removing existing .git folder..." -ForegroundColor Yellow
        Remove-Item -Path ".git" -Recurse -Force
        Write-Host "Existing repository removed" -ForegroundColor Green
    } else {
        Write-Host "Using existing repository" -ForegroundColor Green
    }
    Write-Host ""
}

# Initialize Git repository if needed
if (-not (Test-Path ".git")) {
    Write-Host "Step 1/5: Initializing Git repository..." -ForegroundColor Yellow
    git init
    Write-Host "Git repository initialized!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "Step 1/5: Git repository already initialized" -ForegroundColor Green
    Write-Host ""
}

# Configure Git user if not set
Write-Host "Step 2/5: Checking Git configuration..." -ForegroundColor Yellow

$gitUserName = git config user.name 2>&1
$gitUserEmail = git config user.email 2>&1

if (-not $gitUserName -or $gitUserName -like "*error*") {
    Write-Host "Git user name not set. Please enter your name:" -ForegroundColor Yellow
    $userName = Read-Host "Your Name"
    git config user.name "$userName"
    Write-Host "Git user name set to: $userName" -ForegroundColor Green
} else {
    Write-Host "Git user: $gitUserName" -ForegroundColor Green
}

if (-not $gitUserEmail -or $gitUserEmail -like "*error*") {
    Write-Host "Git email not set. Please enter your email:" -ForegroundColor Yellow
    $userEmail = Read-Host "Your Email"
    git config user.email "$userEmail"
    Write-Host "Git email set to: $userEmail" -ForegroundColor Green
} else {
    Write-Host "Git email: $gitUserEmail" -ForegroundColor Green
}
Write-Host ""

# Ensure .gitignore exists in project root
Write-Host "Step 3/5: Checking .gitignore..." -ForegroundColor Yellow

$gitignorePath = Join-Path $projectRoot ".gitignore"
if (-not (Test-Path $gitignorePath)) {
    Write-Host "Creating .gitignore file..." -ForegroundColor Yellow
    
    $gitignoreContent = @'
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# Visual Studio cache/options
.vs/
*.user
*.suo
*.userosscache
*.sln.docstates

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Rr]elease/
x64/
x86/
bld/
[Bb]in/
[Oo]bj/

# ASP.NET
project.lock.json
project.fragment.lock.json
artifacts/

# Sensitive data
appsettings.Development.json
appsettings.Production.json
*.env
.env

# Database
*.mdf
*.ldf
*.ndf
*.bak
DatabaseExport*/
BACKUP_*/

# User uploads
wwwroot/uploads/
wwwroot/temp_uploads/

# Logs
*.log
logs/

# OS files
.DS_Store
Thumbs.db

# IDE
.vscode/
.idea/
*.swp
*.swo

# Railway
.railway/
'@
    
    Set-Content $gitignorePath -Value $gitignoreContent
    Write-Host ".gitignore created!" -ForegroundColor Green
} else {
    Write-Host ".gitignore already exists" -ForegroundColor Green
}
Write-Host ""

# Add all files to git
Write-Host "Step 4/5: Adding files to Git..." -ForegroundColor Yellow
Write-Host "This may take a moment..." -ForegroundColor Gray

git add -A

$status = git status --short
if ($status) {
    Write-Host "Files staged for commit:" -ForegroundColor Cyan
    git status --short
} else {
    Write-Host "No changes to stage" -ForegroundColor Yellow
}
Write-Host ""

# Create initial commit
Write-Host "Step 5/5: Creating initial commit..." -ForegroundColor Yellow

$commitMessage = "Initial commit - Prepared for Railway deployment"
git commit -m "$commitMessage" 2>&1 | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "Initial commit created!" -ForegroundColor Green
} else {
    Write-Host "No changes to commit or commit already exists" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Git Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "What was done:" -ForegroundColor Cyan
Write-Host "  Git repository initialized" -ForegroundColor White
Write-Host "  Git user configured" -ForegroundColor White
Write-Host "  .gitignore created/verified" -ForegroundColor White
Write-Host "  Files added to Git" -ForegroundColor White
Write-Host "  Initial commit created" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NEXT STEPS: GITHUB & RAILWAY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "OPTION 1: Push to GitHub (Recommended)" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Yellow
Write-Host "1. Go to: https://github.com/new" -ForegroundColor White
Write-Host "2. Create a repository named: ibits-portal" -ForegroundColor White
Write-Host "3. Do NOT initialize with README" -ForegroundColor White
Write-Host "4. Run these commands:" -ForegroundColor White
Write-Host ""
Write-Host "   git branch -M main" -ForegroundColor Cyan
Write-Host "   git remote add origin https://github.com/YOUR_USERNAME/ibits-portal.git" -ForegroundColor Cyan
Write-Host "   git push -u origin main" -ForegroundColor Cyan
Write-Host ""
Write-Host "OPTION 2: Deploy directly to Railway" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Yellow
Write-Host "1. Go to: https://railway.app" -ForegroundColor White
Write-Host "2. Sign up with GitHub" -ForegroundColor White
Write-Host "3. Click 'Deploy from GitHub repo'" -ForegroundColor White
Write-Host "4. Select your ibits-portal repository" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "For detailed Railway deployment steps:" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Open: Step4_Railway_Instructions.md" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
