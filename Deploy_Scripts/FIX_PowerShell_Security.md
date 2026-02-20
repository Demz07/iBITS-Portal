# 🔧 Fix PowerShell Script Execution Error

## Problem
PowerShell is blocking script execution for security reasons.

## ✅ Solution (Choose One)

### **Option 1: Enable Scripts for This Session Only (Recommended)**

Run this command in PowerShell **as Administrator**:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

This only affects the current PowerShell window and is the safest option.

### **Option 2: Run Scripts Without Changing Policy**

Instead of running `.\Step1_Prepare_Project.ps1`, use:

```powershell
powershell -ExecutionPolicy Bypass -File ".\Step1_Prepare_Project.ps1"
```

### **Option 3: Enable Scripts Permanently (Use with Caution)**

Run PowerShell **as Administrator** and execute:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
```

Then type `Y` and press Enter.

---

## 🚀 Quick Start (Easiest Method)

1. **Right-click PowerShell** → Select "Run as Administrator"
2. **Navigate to the folder:**
   ```powershell
   cd "C:\Users\Dave\source\repos\iBITS Portal\Deploy_Scripts"
   ```
3. **Run this command:**
   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   ```
4. **Now run the script:**
   ```powershell
   .\Step1_Prepare_Project.ps1
   ```

---

## ℹ️ What These Options Mean

- **Process**: Only this PowerShell window (safest)
- **CurrentUser**: All your PowerShell windows (your account only)
- **RemoteSigned**: Allows local scripts, requires remote scripts to be signed
- **Bypass**: Runs all scripts (least restrictive)

---

## 🆘 Still Having Issues?

Let me know and I can:
1. Create alternative deployment methods
2. Convert scripts to batch files (.bat)
3. Walk you through manual steps
