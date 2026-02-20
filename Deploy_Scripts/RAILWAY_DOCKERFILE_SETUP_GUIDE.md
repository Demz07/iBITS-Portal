# 🚂 Railway: Switch to Dockerfile Builder

## Step-by-Step Visual Guide

---

## **Step 1: Go to Railway Dashboard**

1. Open your browser and go to: **https://railway.app**
2. You should see your project listed
3. **Click on your project name** (it should be something like "iBITS Portal" or whatever you named it)

**What you'll see:**
- A dashboard with your deployments
- Left sidebar with your services
- Main area showing build logs

---

## **Step 2: Find Your Service**

On the left sidebar, you should see:
- Your service name (probably "iBITS-Portal" or "This-will-the-Current")
- Click on **the service name** (NOT the database)

**Look for:**
```
┌─────────────────┐
│  Your Services  │
├─────────────────┤
│ ► iBITS-Portal  │  <--- Click this one
│ ► PostgreSQL    │
└─────────────────┘
```

---

## **Step 3: Click the "Settings" Tab**

At the top of the screen, you'll see several tabs:

```
[Deployments] [Metrics] [Logs] [Variables] [Settings]
                                            ^
                                     Click here!
```

Click on **"Settings"**

---

## **Step 4: Scroll to "Build" Section**

Once in Settings, scroll down until you see a section titled **"Build"**

You'll see several options:
- **Root Directory** (should be empty or "/")
- **Builder** (this is what we need!)
- **Build Command** (optional)
- **Watch Paths** (optional)

---

## **Step 5: Change Builder to Dockerfile**

In the **"Build"** section:

1. Find the **"Builder"** dropdown
   - It currently says: `Nixpacks` (with a red X because it failed)

2. **Click on the dropdown**

3. You'll see options:
   ```
   ○ Nixpacks
   ○ Dockerfile     <--- Select this!
   ○ Buildpacks
   ```

4. **Select "Dockerfile"**

**What happens:**
- The dropdown will now show "Dockerfile"
- You might see a message: "Railway will use your Dockerfile to build"

---

## **Step 6: Deploy!**

After changing to Dockerfile:

**Option A: Automatic Redeploy**
- Railway might automatically trigger a new deployment
- Watch the "Deployments" tab for activity

**Option B: Manual Redeploy**
1. Go back to the **"Deployments"** tab
2. Click the **"Deploy"** button in the top right
3. Or click the **three dots (...)** next to latest deployment
4. Select **"Redeploy"**

---

## **Step 7: Watch the Build**

After deploying:

1. Go to **"Deployments"** tab
2. Click on the **latest deployment** (it will be at the top)
3. You'll see build logs in real-time

**What to expect:**
```
Building with Dockerfile...
[+] Building Docker image
Step 1/10: FROM mcr.microsoft.com/dotnet/sdk:8.0
Step 2/10: WORKDIR /app
...
✓ Build succeeded!
Starting deployment...
✓ Deployment live!
```

**Build time:** 5-10 minutes (first time)

---

## **Step 8: Verify Success**

Once the build completes:

1. Look for **"✓ Deployment successful"** or **green checkmark**
2. Click on **"Settings"** tab
3. Scroll to **"Domains"** section
4. You'll see your app URL: `https://your-app.railway.app`
5. **Click the URL** to test your app!

---

## 🎯 **Quick Reference - What to Click**

```
1. Railway.app → Your Project
2. Left Sidebar → Your Service Name (not PostgreSQL)
3. Top Tabs → Settings
4. Scroll Down → Build section
5. Builder Dropdown → Dockerfile
6. Top Right → Deploy button (if needed)
7. Deployments Tab → Watch it build
```

---

## ⚠️ **Common Issues**

### **Issue: Can't find "Builder" dropdown**
- Make sure you clicked on the **service** (left sidebar), not the project
- Make sure you're in **Settings** tab, not Deployments

### **Issue: Dockerfile not appearing in dropdown**
- Make sure you pushed the Dockerfile to GitHub
- Refresh the Railway page (F5)

### **Issue: Build still failing**
- Check the build logs for errors
- Copy the error and show it to me - I'll help!

---

## 📸 **Visual Clues**

Look for these elements:

**Settings Tab:**
```
┌──────────────────────────────┐
│ Settings                     │
├──────────────────────────────┤
│                              │
│ [Service Name]               │
│ [Environment]                │
│ [Root Directory]             │
│ ┌────────────────────────┐  │
│ │ Build                  │  │ <-- This section
│ ├────────────────────────┤  │
│ │ Builder: [Dockerfile ▼]│  │ <-- This dropdown
│ └────────────────────────┘  │
│                              │
└──────────────────────────────┘
```

---

## ✅ **After Successful Deployment**

Once you see **"Deployment successful"**:

1. **Tell me!** I'll help you with the next steps:
   - Set up environment variables
   - Connect to PostgreSQL
   - Import your database
   - Test the application

2. **Get your app URL** from Settings → Domains

3. **Don't worry about the database yet** - we'll import data after deployment works!

---

## 🆘 **Need Help?**

If you get stuck at any step:
1. Take a screenshot of what you see
2. Tell me which step you're on
3. Copy any error messages

I'll help you figure it out! 🚀

---

**Good luck! Start with Step 1 and let me know when you reach the deployment stage!** 📦
