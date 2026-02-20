# 🚀 Railway Deployment - Start Here!

Welcome! This guide will help you deploy your iBITS Portal to Railway.app for **FREE** (no credit card needed).

## 📋 What You'll Need

- ✅ Git installed (you have this!)
- ✅ A GitHub account (free to create)
- ✅ A Railway account (free, uses GitHub login)
- ✅ About 30-45 minutes

## 🎯 Deployment Steps Overview

### **Step 1: Prepare Your Project** ⏱️ 5 minutes
Run: `.\Step1_Prepare_Project.ps1`

This script will:
- Install PostgreSQL packages (replaces SQL Server)
- Update your code to work with PostgreSQL
- Create Railway configuration files
- Back up your original files

### **Step 2: Export Your Database** ⏱️ 5 minutes
Run: `.\Step2_Export_Database.ps1`

This script will:
- Export all your data from SQL Server to CSV files
- Create a folder: `DatabaseExport/`
- You'll import this data to Railway later

### **Step 3: Setup Git Repository** ⏱️ 5 minutes
Run: `.\Step3_Setup_Git.ps1`

This script will:
- Initialize Git repository
- Add all files (except secrets)
- Create initial commit
- Give you instructions for GitHub

### **Step 4: Deploy to Railway** ⏱️ 20-30 minutes
Follow: `Step4_Railway_Instructions.md`

This guide has:
- Screenshots for every step
- Railway account setup
- Database creation
- Deployment instructions
- Data import guide

## 🚦 Quick Start (Run These in Order)

```powershell
# Make sure you're in the Deploy_Scripts folder
cd "C:\Users\Dave\source\repos\iBITS Portal\Deploy_Scripts"

# Step 1: Prepare project
.\Step1_Prepare_Project.ps1

# Step 2: Export database
.\Step2_Export_Database.ps1

# Step 3: Setup Git
.\Step3_Setup_Git.ps1

# Step 4: Follow the Railway guide
notepad Step4_Railway_Instructions.md
```

## ⚠️ Important Notes

1. **Backup**: Step 1 creates backups, but make sure your project is backed up
2. **Internet**: You'll need internet to download packages
3. **GitHub**: You'll need to create a GitHub repo (free)
4. **Railway**: Sign up at https://railway.app (free, no credit card)

## 🆘 Need Help?

- Each script has detailed output explaining what it's doing
- If something fails, the script will tell you what went wrong
- You can run scripts multiple times (they're safe)

## 💰 Cost

**100% FREE!** Railway gives you:
- $5 free credits per month
- Your app should stay within free limits
- No credit card required

---

## Ready to Start?

Run the first script:

```powershell
.\Step1_Prepare_Project.ps1
```

Good luck! 🎉
