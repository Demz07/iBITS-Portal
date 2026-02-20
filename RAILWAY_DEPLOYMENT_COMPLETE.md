# 🚂 Complete Railway.app Deployment Guide
## Deploy iBITS Portal - 100% FREE, No Credit Card!

---

## 🎯 What You'll Accomplish

By following this guide, you'll deploy your iBITS Portal to Railway.app with:
- ✅ **Free hosting** for your ASP.NET Core app
- ✅ **Free PostgreSQL database** (1GB)
- ✅ **$5 monthly credit** (~500 hours runtime)
- ✅ **Custom domain support** (optional)
- ✅ **Automatic HTTPS/SSL**
- ✅ **GitHub auto-deployment**

**Estimated Time:** 30-45 minutes

---

## 📋 Before You Start

### Required Accounts (All FREE)
- [ ] **GitHub Account** - https://github.com/signup
- [ ] **Railway Account** - https://railway.app/login (sign up with GitHub)

### Required Tools
- [ ] **Git** installed on your computer
- [ ] **Visual Studio 2022** or **VS Code**

---

## 🎬 Step-by-Step Deployment

### PHASE 1: Prepare Your Code (10 minutes)

#### Step 1.1: Migrate to PostgreSQL

Open PowerShell and run:

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\PostgreSQL_Migration"
.\01_Migrate_To_PostgreSQL.ps1
```

**What this does:**
- ✅ Backs up your current `Program.cs`
- ✅ Removes SQL Server package
- ✅ Adds PostgreSQL package
- ✅ Updates code for PostgreSQL

#### Step 1.2: Export Your Current Data

```powershell
.\02_Export_Data_From_MSSQL.ps1
```

**What this does:**
- ✅ Exports all tables to CSV files
- ✅ Saves to `PostgreSQL_Migration/ExportedData/`
- ✅ Ready for import to Railway database

#### Step 1.3: Initialize Git Repository

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal"

# Initialize git
git init

# Create .gitignore
@"
bin/
obj/
.vs/
*.user
*.suo
appsettings.Development.json
wwwroot/temp_uploads/
wwwroot/uploads/
*.log
"@ | Out-File -FilePath .gitignore -Encoding UTF8

# Add files
git add .
git commit -m "Initial commit - iBITS Portal for Railway"
```

---

### PHASE 2: Create GitHub Repository (5 minutes)

#### Step 2.1: Create Repository on GitHub

1. Go to https://github.com/new
2. **Repository name:** `ibits-portal`
3. **Description:** "iBITS Portal - Student Management System"
4. **Visibility:** Private (recommended) or Public
5. Click **"Create repository"**

#### Step 2.2: Push Your Code

```powershell
# Add remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/ibits-portal.git

# Push code
git branch -M main
git push -u origin main
```

**Expected output:**
```
Enumerating objects: 250, done.
Counting objects: 100% (250/250), done.
...
Branch 'main' set up to track remote branch 'main' from 'origin'.
```

---

### PHASE 3: Deploy to Railway (15 minutes)

#### Step 3.1: Sign Up for Railway

1. Go to https://railway.app
2. Click **"Login"** → **"Login with GitHub"**
3. Authorize Railway to access your GitHub
4. Complete email verification if prompted

#### Step 3.2: Create New Project

1. Click **"New Project"**
2. Select **"Deploy from GitHub repo"**
3. Choose **"ibits-portal"** from the list
4. Railway will start deploying automatically

#### Step 3.3: Add PostgreSQL Database

1. In your Railway project, click **"+ New"**
2. Select **"Database"** → **"PostgreSQL"**
3. Wait for PostgreSQL to provision (~30 seconds)

#### Step 3.4: Configure Environment Variables

1. Click on your **web service** (not the database)
2. Go to **"Variables"** tab
3. Click **"+ New Variable"**
4. Add the following:

**Variable 1:**
- **Name:** `DATABASE_URL`
- **Value:** Click **"Add Reference"** → Select **PostgreSQL** → **`DATABASE_URL`**

**Variable 2:**
- **Name:** `ASPNETCORE_ENVIRONMENT`
- **Value:** `Production`

**Variable 3:**
- **Name:** `ConnectionStrings__DefaultConnection`
- **Value:** Click **"Add Reference"** → Select **PostgreSQL** → **`DATABASE_URL`**

#### Step 3.5: Configure Build Settings

1. Click on **"Settings"** tab
2. Scroll to **"Build"** section
3. **Build Command:** (leave default or set to)
   ```
   dotnet publish -c Release -o out
   ```
4. **Start Command:**
   ```
   dotnet out/iBITS Portal.dll
   ```

5. Click **"Deploy"** (if not auto-deploying)

---

### PHASE 4: Configure Database (10 minutes)

#### Step 4.1: Connect to Railway Database

1. In Railway, click on your **PostgreSQL** service
2. Go to **"Connect"** tab
3. Copy the **connection details**:
   - **Host:** `xxxxx.railway.app`
   - **Port:** `5432`
   - **Database:** `railway`
   - **Username:** `postgres`
   - **Password:** `(shown in Railway)`

#### Step 4.2: Run Migrations

**Option A: Using Railway CLI (Recommended)**

```powershell
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# Link to your project
railway link

# Run migrations
railway run dotnet ef database update
```

**Option B: Using Connection String Locally**

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"

# Set temporary connection string
$env:ConnectionStrings__DefaultConnection = "Host=your-host.railway.app;Port=5432;Database=railway;Username=postgres;Password=your-password"

# Run migrations
dotnet ef database update
```

#### Step 4.3: Import Your Data

Use **pgAdmin** or **DBeaver** to import CSV files:

1. Download **pgAdmin**: https://www.pgadmin.org/download/
2. Connect using Railway credentials (from Step 4.1)
3. Right-click your database → **Import/Export**
4. Import each CSV file from `PostgreSQL_Migration/ExportedData/`

**Or use Railway's Data tab:**
1. Click PostgreSQL service → **"Data"** tab
2. Use SQL query to insert data manually (for small datasets)

---

### PHASE 5: Verify Deployment (5 minutes)

#### Step 5.1: Get Your App URL

1. In Railway, click your **web service**
2. Go to **"Settings"** tab
3. Under **"Domains"**, you'll see:
   ```
   https://ibits-portal-production.up.railway.app
   ```
4. Click to open your app!

#### Step 5.2: Test Your Application

Visit your Railway URL and test:
- [ ] **Homepage loads** ✅
- [ ] **Login works** ✅
- [ ] **Student records appear** ✅
- [ ] **Database connected** ✅

#### Step 5.3: Monitor Your App

Railway Dashboard shows:
- 📊 **Usage:** CPU, Memory, Network
- 📈 **Metrics:** Response times, errors
- 📝 **Logs:** Real-time application logs

---

## 🎉 Success! Your App is Live!

Your iBITS Portal is now deployed at:
```
https://your-app-name.up.railway.app
```

---

## 🔧 Post-Deployment Configuration

### Add Custom Domain (Optional)

1. Go to your web service → **"Settings"** → **"Domains"**
2. Click **"Custom Domain"**
3. Enter your domain (e.g., `portal.yourschool.com`)
4. Add CNAME record to your DNS provider
5. Railway auto-provisions SSL certificate

### Enable Auto-Deploy

Already enabled! Every `git push` to `main` branch triggers deployment.

```powershell
# Make changes
git add .
git commit -m "Updated feature X"
git push

# Railway automatically deploys! 🚀
```

### Set Up Monitoring

Railway provides:
- **Logs:** Click "Deployments" → "Logs"
- **Metrics:** CPU, RAM, Network usage
- **Alerts:** Set up in project settings

---

## 💰 Usage & Billing

### Free Tier Limits
- **Monthly Credit:** $5 (500 hours)
- **Apps:** 2 services (web + database)
- **Storage:** 1GB database
- **Bandwidth:** 100GB/month

### How to Stay Free

1. **Sleep Timer:** Set your app to sleep during off-hours
2. **Monitor Usage:** Check Railway dashboard regularly
3. **Optimize:** Reduce build times and resource usage

**Typical usage:** ~300-400 hours/month = **FREE** ✅

---

## 🐛 Troubleshooting

### Build Failed

**Error:** "Could not find project file"
- **Fix:** Check `iBITS Portal.csproj` is in root directory
- **Command:** Move project files to root if nested

### Database Connection Error

**Error:** "Unable to connect to database"
- **Fix:** Check environment variable `ConnectionStrings__DefaultConnection`
- **Verify:** PostgreSQL service is running in Railway

### Application Won't Start

**Error:** "Application timeout"
- **Fix:** Check Start Command in Settings
- **Command:** Should be `dotnet out/iBITS Portal.dll`
- **Check:** Port binding (Railway uses `PORT` env variable)

### 502 Bad Gateway

**Causes:**
1. App crashed on startup
2. Wrong start command
3. Missing environment variables

**Fix:**
- Check logs: Railway → Your Service → Logs
- Verify all environment variables set
- Ensure migrations ran successfully

---

## 📚 Useful Commands

### Railway CLI

```powershell
# Install
npm install -g @railway/cli

# Login
railway login

# Link project
railway link

# View logs
railway logs

# Run commands in Railway environment
railway run [command]

# Open in browser
railway open
```

### Git Workflow

```powershell
# Check status
git status

# Stage changes
git add .

# Commit
git commit -m "Description of changes"

# Push (triggers auto-deploy)
git push

# View history
git log --oneline
```

---

## 🚀 Advanced Features

### Environment-Specific Settings

Create `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Database Backups

Railway provides automatic daily backups for paid plans. For free tier:

**Manual backup:**
```powershell
# Using Railway CLI
railway run pg_dump > backup.sql

# Or use pgAdmin
# Right-click database → Backup
```

### Scaling (Paid Feature)

Upgrade your service in Railway settings:
- **Starter:** $5/month (more hours)
- **Pro:** $20/month (unlimited)

---

## 📊 Cost Comparison

| Feature | Railway FREE | Azure | Heroku |
|---------|-------------|-------|---------|
| **App Hosting** | $0 (500h) | $13/mo | $7/mo |
| **Database** | $0 (1GB) | $5/mo | $9/mo |
| **SSL** | ✅ Free | ✅ Free | ✅ Free |
| **Auto-deploy** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Credit Card** | ❌ No | ✅ Yes | ✅ Yes |
| **Total** | **$0-5/mo** | **$18/mo** | **$16/mo** |

**Winner: Railway for students!** 🏆

---

## 🎓 Learning Resources

- **Railway Docs:** https://docs.railway.app
- **PostgreSQL Docs:** https://www.postgresql.org/docs/
- **ASP.NET Core:** https://learn.microsoft.com/aspnet/core
- **Entity Framework:** https://learn.microsoft.com/ef/core

---

## ✅ Deployment Checklist

Use this checklist for your deployment:

**Pre-Deployment:**
- [ ] Migrated to PostgreSQL
- [ ] Exported data from SQL Server
- [ ] Created GitHub repository
- [ ] Pushed code to GitHub
- [ ] Signed up for Railway

**Deployment:**
- [ ] Created Railway project
- [ ] Connected GitHub repository
- [ ] Added PostgreSQL database
- [ ] Set environment variables
- [ ] Configured build settings
- [ ] Deployed successfully

**Post-Deployment:**
- [ ] Ran database migrations
- [ ] Imported data
- [ ] Tested application
- [ ] Verified all features work
- [ ] Set up custom domain (optional)
- [ ] Monitored usage

---

## 🆘 Getting Help

**Railway Support:**
- Discord: https://discord.gg/railway
- Help Center: https://help.railway.app

**Community:**
- Railway Community Forum
- Stack Overflow (tag: railway)

**Documentation:**
- This guide: `RAILWAY_DEPLOYMENT_COMPLETE.md`
- Migration guide: `PostgreSQL_Migration/README.md`
- Free hosting options: `FREE_HOSTING_OPTIONS.md`

---

## 🎊 Congratulations!

You've successfully deployed your iBITS Portal to the cloud for **FREE**! 🎉

Your students can now access the portal from anywhere in the world.

**Share your success:**
- Show your classmates
- Add to your portfolio
- Share on LinkedIn

---

**Need more help? Check the other guides in this folder!**

- `FREE_HOSTING_OPTIONS.md` - Compare other platforms
- `PostgreSQL_Migration/README.md` - Database migration details
- `AZURE_DEPLOYMENT_GUIDE.md` - Azure deployment (requires credit card)

**Happy deploying! 🚀**
