# 🚂 Railway.app Deployment Guide - iBITS Portal
## 100% FREE - No Credit Card Required!

---

## 📋 Table of Contents
1. [Prerequisites](#prerequisites)
2. [Database Migration (SQL Server → PostgreSQL)](#database-migration)
3. [Railway Setup](#railway-setup)
4. [Deploy Your Application](#deploy-application)
5. [Verify Deployment](#verify-deployment)
6. [Troubleshooting](#troubleshooting)

---

## 🎯 What You'll Get (FREE)

- ✅ **App Hosting:** Your ASP.NET Core app
- ✅ **PostgreSQL Database:** 1GB storage
- ✅ **$5 Monthly Credit:** ~500 hours runtime
- ✅ **Custom Domain:** Bring your own domain
- ✅ **SSL Certificate:** Automatic HTTPS
- ✅ **GitHub Integration:** Auto-deploy on push

**Total Cost: $0/month** 🎉

---

## 📋 Prerequisites

### 1. Create Accounts (All FREE)
- [ ] **GitHub Account:** https://github.com
- [ ] **Railway Account:** https://railway.app (sign up with GitHub)

### 2. Install Required Tools
```powershell
# Install Git (if not installed)
winget install Git.Git

# Install GitHub CLI (optional but helpful)
winget install GitHub.cli

# Verify installations
git --version
```

---

## 🗄️ Database Migration (SQL Server → PostgreSQL)

### Step 1: Update Your Project Dependencies

Open PowerShell in your project directory:

```powershell
cd "C:\Users\Dave\source\repos\iBITS Portal\iBITS Portal"

# Remove SQL Server package
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer

# Add PostgreSQL package
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Step 2: Update Program.cs

I'll create an updated version for you...

