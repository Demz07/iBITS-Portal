# 💰 iBITS Portal - FREE TIER Azure Deployment Guide

Deploy your iBITS Portal to Azure for **only ~$5/month**!

---

## 💵 Cost Breakdown

| Service | Tier | Monthly Cost |
|---------|------|--------------|
| **App Service** | F1 (FREE) | **$0.00** ✅ |
| **SQL Database** | Basic (2GB) | **~$4.90** |
| **Bandwidth** | First 5GB | **$0.00** |
| **TOTAL** | | **~$5/month** 💰 |

⭐ **This is the ABSOLUTE LOWEST COST possible for Azure!**

> Note: Azure does not offer a free SQL Database tier. Basic tier at ~$5/month is the minimum.

---

## ⚠️ FREE Tier Limitations

### App Service F1 (FREE)

**Good for:**
- ✅ Testing and development
- ✅ Demos and proof-of-concept
- ✅ Personal projects with low traffic
- ✅ Student projects

**Limitations:**
- ⚠️ **60 CPU minutes per day** (resets at midnight UTC)
- ⚠️ **App sleeps after 20 min** of inactivity
- ⚠️ **Cold start delay** (~10-20 seconds on first request after sleep)
- ⚠️ **1GB RAM, 1GB Storage**
- ⚠️ **No custom domain SSL**
- ⚠️ **No Always-On** support

### SQL Database Basic

**Good for:**
- ✅ Small to medium databases (up to 2GB)
- ✅ Your current database (~132 students, 5 officers, etc.)
- ✅ Low transaction workloads

**Specifications:**
- 📦 2GB max size
- 🔄 5 DTUs (Database Transaction Units)
- 💾 Automated backups included

---

## 🚀 FREE Tier Deployment Steps

### Quick Start (Use FREE Script)

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\Azure_Deploy_Scripts"

# Step 1: Export database
.\01_Export_Database.ps1

# Step 2: Create FREE tier Azure resources
.\02_Create_Azure_Resources_FREE.ps1

# Step 3: Deploy application (same as before)
.\03_Deploy_Application.ps1
```

---

## 💡 Tips for FREE Tier Success

### 1. Handling App Sleep (Cold Starts)

**Problem:** App sleeps after 20 minutes of inactivity. First request is slow.

**Solutions:**

**Option A - Use Free Uptime Monitor**
```
Sign up for free monitoring service:
- UptimeRobot (https://uptimerobot.com) - FREE plan
- Pingdom (https://pingdom.com) - 100 checks free
- StatusCake (https://statuscake.com) - FREE plan

Set it to ping your app every 15-20 minutes:
URL: https://your-app.azurewebsites.net
Interval: 15 minutes
```

**Option B - Accept Cold Starts**
- First request after sleep takes 10-20 seconds
- Subsequent requests are fast
- Good for demo/testing scenarios

**Option C - Manual Wake-Up**
- Just visit the URL before showing to users
- App stays awake for 20 minutes

### 2. Staying Within CPU Limits

**60 CPU minutes = 1 hour of processing per day**

**Tips to conserve CPU:**
- ✅ Optimize database queries
- ✅ Use efficient LINQ queries
- ✅ Minimize complex calculations
- ✅ Disable verbose logging in production
- ✅ Use async/await properly

**Monitor CPU usage:**
```powershell
az monitor metrics list \
  --resource [your-app-resource-id] \
  --metric CpuTime \
  --start-time [date] \
  --end-time [date]
```

### 3. Database Size Management

**Basic tier = 2GB max**

Your current database fits easily:
- Students: ~132 records
- Officers: ~5 records
- Fees/Fines: ~80 records
- Total: Well under 100MB

**Keep it small:**
- Regularly archive old data
- Clean up activity logs periodically
- Avoid storing large files in database
- Use Azure Blob Storage for images/files

---

## 📊 What Can You Run on FREE Tier?

### Realistic Usage Scenarios

**✅ Perfect For:**
- Student capstone projects
- Portfolio demonstrations
- Internal team tools (5-10 users)
- Testing and development
- Prototype applications

**⚠️ Manageable (with limitations):**
- Small organization portal (20-50 users)
- Event registration system (low frequency)
- Class management system (1-2 classes)

**❌ Not Recommended For:**
- High-traffic public websites
- 24/7 real-time applications
- Large organizations (100+ users)
- Production-critical systems

---

## 🔄 Upgrade Path (When You Grow)

### When to Upgrade?

**Upgrade App Service when:**
- You hit 60 CPU minutes daily
- Users complain about slow loading
- You need custom domain with SSL
- You need always-on availability

**Upgrade SQL Database when:**
- Database size approaches 2GB
- Queries become slow
- You need better performance

### Easy Upgrade Commands

**Upgrade App Service to B1 (~$13/month):**
```powershell
az appservice plan update \
  --name [your-plan] \
  --resource-group [your-rg] \
  --sku B1
```

**Benefits of B1:**
- No sleep/cold starts
- Always-on support
- 1.75GB RAM
- Custom domain SSL
- 10GB storage

**Upgrade SQL to Standard S0 (~$15/month):**
```powershell
az sql db update \
  --name PortaliBITS \
  --server [your-server] \
  --resource-group [your-rg] \
  --service-objective S0
```

**Benefits of S0:**
- 250GB max size
- 10 DTUs (better performance)
- Faster queries

---

## 🆓 Completely FREE Alternatives

### Option: Azure for Students

If you're a student with a .edu email:

**Azure for Students Benefits:**
- $100 free credit (12 months)
- Free services without credit card
- Free access to certain services always

**Sign up:** https://azure.microsoft.com/free/students/

**With student credits, you can use:**
- B1 App Service for FREE (for 12 months)
- Better SQL tier for FREE (for 12 months)

---

## 🔍 Monitoring FREE Tier Usage

### Check CPU Usage

```powershell
# View app metrics
az webapp show \
  --name [your-app] \
  --resource-group [your-rg] \
  --query "usageState"

# View in Azure Portal
# Go to: App Service → Metrics → CPU Time
```

### Check Database Size

```powershell
# Check database size
az sql db show \
  --name PortaliBITS \
  --server [your-server] \
  --resource-group [your-rg] \
  --query "maxSizeBytes"
```

### Set Budget Alerts

```powershell
# In Azure Portal:
# Cost Management → Budgets → Create
# Set budget: $10/month
# Alert at: 80% ($8)
```

---

## 🎯 FREE Tier Best Practices

### 1. Optimize for Cold Starts

```csharp
// In Program.cs - keep startup minimal
public static async Task Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Only essential services
    builder.Services.AddControllersWithViews();
    builder.Services.AddDbContext<ApplicationDbContext>();
    
    // Avoid heavy initialization
    var app = builder.Build();
    
    // Lazy initialization for expensive operations
    app.Run();
}
```

### 2. Efficient Database Queries

```csharp
// Good: Select only what you need
var students = await _context.Student
    .Select(s => new { s.StudentNum, s.FullName, s.Course })
    .ToListAsync();

// Bad: Loading entire entities
var students = await _context.Student.ToListAsync();
```

### 3. Use Application Logging Wisely

```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",  // Not "Information" or "Debug"
      "Microsoft": "Warning"
    }
  }
}
```

---

## 📞 Support & Resources

- **Azure Free Tier Docs:** https://azure.microsoft.com/free/
- **App Service Pricing:** https://azure.microsoft.com/pricing/details/app-service/
- **SQL Database Pricing:** https://azure.microsoft.com/pricing/details/sql-database/
- **Cost Management:** https://portal.azure.com → Cost Management

---

## ✅ Summary

### What You Get for ~$5/month:

✅ **Full-stack cloud deployment**
✅ **Automatic HTTPS/SSL**
✅ **Azure SQL Database with backups**
✅ **.NET 8.0 hosting**
✅ **99.95% uptime SLA**
✅ **Azure security & monitoring**
✅ **Global CDN capability**

### Trade-offs:

⚠️ App sleeps after 20 min idle
⚠️ 60 CPU minutes per day limit
⚠️ Cold start delays (10-20 sec)
⚠️ 2GB database size limit

**Perfect for:** Student projects, demos, testing, low-traffic portals

---

**Created:** February 2026  
**Version:** 1.0 (FREE TIER)  
**For:** iBITS Portal Azure Deployment
