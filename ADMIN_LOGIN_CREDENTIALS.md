# 🔐 Admin Login Credentials

## Default Admin Account

After deploying the updated code to Railway, you can login with:

**Email:** `admin@ibits.edu.ph`  
**Password:** `Admin@123`

---

## How It Works

The `RoleInitializer.cs` now:
1. ✅ Creates all 7 roles (Admin, Officer, Member, etc.)
2. ✅ Creates a default admin user automatically
3. ✅ Assigns the "Admin" role to this user
4. ✅ Runs on every application startup (safe - won't duplicate)

---

## What Happens on First Deploy

When you deploy to Railway:
1. Database migrations run → Creates all tables
2. `RoleInitializer` runs → Creates roles + admin user
3. You can immediately login as admin!

---

## Security Notes

⚠️ **IMPORTANT:** After your first login, please:
1. Login with `admin@ibits.edu.ph` / `Admin@123`
2. Change the password immediately
3. Or create a new admin account and delete the default one

---

## Troubleshooting

**Can't login?**
- Make sure you deployed the latest code to Railway
- Check Railway logs to confirm "RoleInitializer" completed
- Verify database migrations succeeded

**Want to change default credentials?**
Edit `Utilities/RoleInitializer.cs` lines:
- Line 44: `var adminEmail = "admin@ibits.edu.ph";`
- Line 54: Password in `CreateAsync(newAdmin, "Admin@123");`

---

## Next Steps After Login

Once logged in as admin, you can:
- Create other admin accounts
- Manage students and officers
- Assign roles to users
- Configure system settings
