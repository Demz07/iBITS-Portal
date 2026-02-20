# ⚡ FREE TIER Quick Start - Deploy for $5/month

Deploy your iBITS Portal to Azure using the **FREE tier** for only **~$5/month**!

---

## 💰 What You'll Pay

- **App Service (F1):** $0.00/month (FREE!) ✅
- **SQL Database (Basic):** ~$4.90/month
- **Total:** **~$5/month** 💰

**This is the cheapest possible Azure deployment!**

---

## ⚠️ What to Expect (FREE Tier)

**Good:**
- ✅ Full cloud hosting with HTTPS
- ✅ Azure SQL Database with backups
- ✅ Perfect for testing, demos, student projects
- ✅ Handles 5-20 users comfortably

**Limitations:**
- ⚠️ App sleeps after 20 min (first load = 10-20 sec delay)
- ⚠️ 60 CPU minutes per day limit
- ⚠️ 1GB RAM, 1GB storage
- ⚠️ No custom domain SSL

**Bottom line:** Great for capstone projects, portfolios, and low-traffic use!

---

## 🚀 3-Step Deployment (15 Minutes)

### Prerequisites

✅ Azure account ([Free trial](https://azure.microsoft.com/free/))  
✅ Azure CLI installed ([Download](https://aka.ms/installazurecliwindows))

Check if installed:
```powershell
az version
```

---

### Step 1: Export Database (2 min) 💾

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"
.\01_Export_Database.ps1
```

**What happens:**
- Backs up your PortaliBITS database
- Saves to: `C:\Temp\iBITS_Azure_Migration\`

---

### Step 2: Create FREE Azure Resources (5 min) ☁️

```powershell
.\02_Create_Azure_Resources_FREE.ps1
```

**What happens:**
- Browser opens for Azure login
- Creates FREE App Service + Basic SQL Database
- **SAVES YOUR CREDENTIALS** - don't lose them!

**Recommended answers:**
- Resource Group: `iBITS-Portal-FREE` (press Enter)
- Region: `1` (Southeast Asia)
- Other names: Press Enter for defaults

**Cost: ~$5/month**

---

### Step 3: Import Database (5 min) 📊

Use **SQL Server Management Studio (SSMS):**

1. Open SSMS
2. Connect to Azure:
   - Server: `[from script output].database.windows.net`
   - Authentication: SQL Server Authentication
   - Username: `ibitsadmin`
   - Password: `[from script output]`

3. Import:
   - Right-click **Databases** → **Import Data-tier Application**
   - Select: `C:\Temp\iBITS_Azure_Migration\PortaliBITS_Backup_*.bak`
   - Click Next → Finish

4. Verify (run in SSMS):
   ```sql
   SELECT COUNT(*) FROM Student;
   ```

---

### Step 4: Deploy Application (3 min) 🚀

```powershell
.\03_Deploy_Application.ps1
```

**What happens:**
- Builds your app
- Uploads to Azure
- Opens your live site!

**First load will be slow (cold start) - this is normal!**

---

## ✅ Test Your Site

Visit: `https://[your-app].azurewebsites.net`

**Try:**
- [ ] Login with admin account
- [ ] View students
- [ ] Check fees/fines
- [ ] Create event

**⚠️ First load after 20 min idle = slow (10-20 sec)**  
This is expected on FREE tier. Subsequent loads are fast!

---

## 💡 Tips for FREE Tier

### Wake Up Sleeping App

Your app sleeps after 20 min. To keep it awake:

**Option 1 - Free Monitoring Service:**
1. Sign up at [UptimeRobot](https://uptimerobot.com) (FREE)
2. Add monitor:
   - URL: `https://[your-app].azurewebsites.net`
   - Interval: 15 minutes
3. Your app stays awake!

**Option 2 - Accept Cold Starts:**
- Just visit URL before demos/presentations
- App stays awake for 20 minutes

### Monitor Your $5 Budget

```powershell
# In Azure Portal:
# Cost Management → Budgets → Create Budget
# Amount: $10/month
# Alert: 80% ($8)
```

---

## 🔄 Need More Power?

### Upgrade When Ready

**To B1 App Service (~$13/month):**
```powershell
az appservice plan update \
  --name [your-plan] \
  --resource-group iBITS-Portal-FREE \
  --sku B1
```

**Benefits:**
- No more sleep/cold starts!
- No CPU limit
- Better performance

**New total cost: ~$18/month**

---

## 🆘 Troubleshooting

### "App won't load"
```powershell
# Check if it's sleeping (cold start)
# Just wait 15-20 seconds on first load

# Or restart manually
az webapp restart \
  --name [your-app] \
  --resource-group iBITS-Portal-FREE
```

### "Can't connect to database"
```powershell
# Add your IP to firewall
az sql server firewall-rule create \
  --resource-group iBITS-Portal-FREE \
  --server [your-server] \
  --name MyNewIP \
  --start-ip-address [your-ip] \
  --end-ip-address [your-ip]
```

### "CPU quota exceeded"
- You've used 60 minutes of CPU today
- Resets at midnight UTC
- Consider upgrading to B1 tier

---

## 📊 What FREE Tier Can Handle

**Perfect for:**
- ✅ Capstone projects
- ✅ Portfolio demos
- ✅ 5-20 concurrent users
- ✅ Testing/development
- ✅ Internal team tools

**Not ideal for:**
- ❌ 24/7 production systems
- ❌ High-traffic websites (100+ users)
- ❌ Real-time applications
- ❌ Always-on requirements

---

## 📋 Your Deployment Summary

After running scripts, you'll have:

**Files saved to:** `C:\Temp\iBITS_Azure_Migration\`
- Database backup
- Credentials & connection strings
- Deployment summary

**⚠️ KEEP THESE FILES SAFE!**

---

## 🎓 Student Tip

**Are you a student?**

Get **$100 FREE credits** with Azure for Students:
- Sign up: https://azure.microsoft.com/free/students/
- Requires .edu email
- Use B1 tier FREE for 12 months!

---

## 📞 Need Help?

- **FREE Tier Details:** [FREE_TIER_DEPLOYMENT_GUIDE.md](FREE_TIER_DEPLOYMENT_GUIDE.md)
- **Full Documentation:** [AZURE_DEPLOYMENT_GUIDE.md](../AZURE_DEPLOYMENT_GUIDE.md)
- **Azure Portal:** https://portal.azure.com

---

## ✨ Summary

**Cost:** Only ~$5/month!  
**Time:** 15 minutes total  
**Perfect for:** Student projects, demos, testing  

**Scripts to run:**
1. `01_Export_Database.ps1`
2. `02_Create_Azure_Resources_FREE.ps1` ← Use this for FREE tier!
3. Import database via SSMS
4. `03_Deploy_Application.ps1`

**Happy deploying! 🚀**

---

**💰 Remember:** This is the CHEAPEST possible cloud deployment for a full-stack .NET + SQL application!
