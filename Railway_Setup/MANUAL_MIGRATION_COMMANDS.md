# Manual Migration Commands for Railway

The migrations need to be run manually. Here's how:

## Option 1: Run Migrations Through Railway CLI

If you have Railway CLI installed:

```bash
railway login
railway link
railway run dotnet ef database update --context PortaliBitsContext
railway run dotnet ef database update --context ApplicationDbContext
```

## Option 2: Add Auto-Migration to Program.cs

We can update the app to run migrations automatically on startup.

## Option 3: Run SQL Script Directly

Use the SQL script we created earlier in Railway's PostgreSQL Query interface.

---

## Which method do you prefer?

1. **Add auto-migration to code** (Easiest - I'll do it now)
2. **Use Railway CLI** (Need to install it)
3. **Run SQL script** (Manual but works)

Let me know!
