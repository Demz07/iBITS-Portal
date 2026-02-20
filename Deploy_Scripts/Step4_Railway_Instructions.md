# 🚂 Step 4: Deploy to Railway

Welcome to the final step! This guide will walk you through deploying your iBITS Portal to Railway.app.

---

## ⏱️ Estimated Time: 20-30 minutes

---

## 📋 Prerequisites (Make Sure You've Done These)

- ✅ Completed Step 1 (Project prepared)
- ✅ Completed Step 2 (Database exported)
- ✅ Completed Step 3 (Git setup)
- ✅ **Pushed your code to GitHub**

---

## 🚀 Railway Deployment Process

### **Part A: Create Railway Account** (5 minutes)

#### 1. Go to Railway
👉 **https://railway.app**

#### 2. Click "Start a New Project"

#### 3. Sign up with GitHub
- Click "Login with GitHub"
- Authorize Railway to access your GitHub account
- **This is FREE and requires NO credit card!**

✅ **You're now logged into Railway!**

---

### **Part B: Create New Project** (5 minutes)

#### 1. Create New Project
- Click "**New Project**" button (top right)
- You'll see options to deploy

#### 2. Choose "Deploy from GitHub repo"
- You'll see a list of your GitHub repositories
- If you don't see your repo, click "**Configure GitHub App**"
- Give Railway access to your `ibits-portal` repository
- Select **ibits-portal** from the list

#### 3. Railway will detect your .NET app
- Railway will automatically detect it's a .NET 8 application
- Click "**Deploy Now**" or "**Add variables**" (we'll add them next)

✅ **Your app is now being deployed!** (This might fail initially - that's okay!)

---

### **Part C: Add PostgreSQL Database** (5 minutes)

#### 1. Add PostgreSQL Service
- In your Railway project dashboard
- Click "**+ New**" button
- Select "**Database**"
- Choose "**PostgreSQL**"

#### 2. Wait for PostgreSQL to provision
- This takes about 30-60 seconds
- You'll see the database appear in your project

#### 3. Get Database Connection String
- Click on the **PostgreSQL** service
- Go to "**Variables**" tab
- Find **DATABASE_URL** and copy it
- It looks like: `postgresql://user:password@host:port/database`

✅ **PostgreSQL database is ready!**

---

### **Part D: Configure Environment Variables** (5 minutes)

#### 1. Click on your **App Service** (not the database)

#### 2. Go to "**Variables**" tab

#### 3. Add these variables:

| Variable Name | Value | Notes |
|---------------|-------|-------|
| `DATABASE_URL` | `${{Postgres.DATABASE_URL}}` | References your PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Sets production mode |
| `PORT` | `5000` | Railway default port |

**How to add variables:**
- Click "**+ New Variable**"
- Enter **Variable Name**
- Enter **Value**
- Click "**Add**"

#### 4. Save and Redeploy
- Variables are saved automatically
- Click "**Deployments**" tab
- Click "**Redeploy**" on the latest deployment

✅ **Environment configured!**

---

### **Part E: Configure Deployment Settings** (2 minutes)

#### 1. Go to "**Settings**" tab (in your app service)

#### 2. Scroll to "**Deploy**" section

#### 3. Check these settings:
- **Build Command**: Should auto-detect (leave blank or `dotnet publish -c Release -o out`)
- **Start Command**: Should be `dotnet out/iBITS Portal.dll`

#### 4. Scroll to "**Networking**" section
- Click "**Generate Domain**"
- This creates your public URL: `https://your-app.railway.app`

✅ **Deployment configured!**

---

### **Part F: Wait for Deployment** (5-10 minutes)

#### 1. Go to "**Deployments**" tab

#### 2. Watch the build logs
- You'll see the build process in real-time
- It will:
  - Clone from GitHub
  - Install .NET 8
  - Build your project
  - Deploy to Railway

#### 3. Check for Success
- **Green checkmark** ✅ = Success!
- **Red X** ❌ = Failed (check logs for errors)

**Common issues:**
- Database not connected → Check DATABASE_URL variable
- Build failed → Check build logs for errors
- App crashes → Check application logs

✅ **App is deployed!**

---

### **Part G: Import Your Data** (10-15 minutes)

Now you need to import the data you exported in Step 2.

#### **Option 1: Using TablePlus (Easiest - Recommended)**

1. **Download TablePlus**
   - 👉 https://tableplus.com/download
   - Free version works fine!

2. **Connect to Railway PostgreSQL**
   - Open TablePlus
   - Click "**Create a new connection**"
   - Select "**PostgreSQL**"
   - Enter connection details from Railway:
     - **Host**: (from DATABASE_URL)
     - **Port**: (from DATABASE_URL, usually 5432)
     - **User**: (from DATABASE_URL)
     - **Password**: (from DATABASE_URL)
     - **Database**: (from DATABASE_URL)
   - Click "**Connect**"

3. **Import CSV Files**
   - Right-click on a table → "**Import**" → "**From CSV**"
   - Select the CSV file from `DatabaseExport/` folder
   - Map columns (should auto-match)
   - Click "**Import**"

4. **Import in Order** (Important!)
   - Follow the order in `DatabaseExport/IMPORT_INSTRUCTIONS.txt`
   - Start with `AspNetRoles.csv`
   - Then `AspNetUsers.csv`
   - Then others

#### **Option 2: Using pgAdmin**

1. **Download pgAdmin**
   - 👉 https://www.pgadmin.org/download/

2. **Connect to Railway PostgreSQL**
   - Add new server
   - Use connection details from Railway DATABASE_URL

3. **Import CSV files**
   - Right-click table → "**Import/Export Data**"
   - Select CSV file
   - Import

#### **Option 3: Using Railway CLI (Advanced)**

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# Link to your project
railway link

# Connect to database
railway run psql $DATABASE_URL

# Import CSV (example)
\copy "Student" FROM 'DatabaseExport/Student.csv' DELIMITER ',' CSV HEADER;
```

✅ **Data imported!**

---

### **Part H: Test Your Deployment** (5 minutes)

#### 1. Open your Railway app URL
- Found in "**Settings**" → "**Domains**"
- Should be: `https://your-app.railway.app`

#### 2. Test these features:
- ✅ Homepage loads
- ✅ Login page appears
- ✅ Can login with admin account
- ✅ Student data shows up
- ✅ Dashboard works

#### 3. Check application logs
- In Railway dashboard → "**Deployments**" → Click deployment
- View logs for any errors

✅ **App is live and working!**

---

## 🎉 Congratulations! Your App is Deployed!

Your iBITS Portal is now live on the internet at:
👉 **https://your-app.railway.app**

---

## 📊 Railway Free Tier Limits

✅ **What you get FREE:**
- $5 in monthly credits
- Up to 500 hours of runtime
- 8GB RAM
- 100GB bandwidth

💡 **To stay FREE:**
- Your app should sleep when inactive (Railway does this automatically)
- Monitor your usage in Railway dashboard
- Optimize database queries

---

## 🔧 Common Issues & Solutions

### Issue: App won't start
**Solution:**
- Check environment variables are set correctly
- Check DATABASE_URL is referencing PostgreSQL
- View logs for specific errors

### Issue: Database connection failed
**Solution:**
- Verify DATABASE_URL variable: `${{Postgres.DATABASE_URL}}`
- Make sure PostgreSQL service is running
- Check connection string format

### Issue: Build failed
**Solution:**
- Check your .NET version (should be 8.0)
- Verify all NuGet packages restored
- Check build logs for specific errors

### Issue: 502 Bad Gateway
**Solution:**
- App might be starting up (wait 1-2 minutes)
- Check if PORT variable is set to 5000
- Check application logs for startup errors

### Issue: Can't see my data
**Solution:**
- Make sure you imported the CSV files
- Check import was successful (no errors)
- Verify data exists in Railway PostgreSQL using TablePlus

---

## 📚 Helpful Railway Resources

- **Railway Docs**: https://docs.railway.app
- **Railway Discord**: https://discord.gg/railway
- **Status Page**: https://status.railway.app

---

## 🔄 Updating Your Deployed App

When you make changes to your code:

```bash
# Make changes locally
# Commit to Git
git add .
git commit -m "Description of changes"

# Push to GitHub
git push

# Railway will automatically redeploy! 🎉
```

---

## 💰 Cost Management

### Monitor Usage:
1. Go to Railway dashboard
2. Click "**Usage**" tab
3. See your credit consumption

### Stay Within Free Tier:
- Your app should use < $5/month
- Database is the main cost
- Monitor and optimize queries

---

## 🆘 Need Help?

If you get stuck:
1. Check Railway logs for errors
2. Review this guide step by step
3. Check Railway documentation
4. Ask on Railway Discord community

---

## ✅ Deployment Checklist

Use this to verify everything is working:

- [ ] Railway account created
- [ ] GitHub repository pushed
- [ ] Railway project created
- [ ] PostgreSQL database added
- [ ] Environment variables configured
- [ ] App deployed successfully
- [ ] Domain/URL generated
- [ ] Data imported to PostgreSQL
- [ ] Login works
- [ ] Dashboard loads
- [ ] Students data displays
- [ ] No errors in logs

---

## 🎯 You're Done!

Your iBITS Portal is now:
- ✅ Deployed on Railway
- ✅ Using PostgreSQL database
- ✅ Accessible via public URL
- ✅ Running for FREE!

**Share your app URL with others and start using it!** 🚀

---

Generated on: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
