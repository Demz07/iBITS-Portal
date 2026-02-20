# Fix Database Connection in Railway

## The Problem

Railway's `DATABASE_URL` is not being read properly. The connection string is empty.

## Solution: Add Reference to PostgreSQL Service

Railway needs to know that your app depends on PostgreSQL.

### Steps:

1. **In Railway, go to your iBITS Portal service**
2. **Click "Settings" tab**
3. **Scroll to "Service Variables" or "Variables"**
4. **Look for a section called "Service References" or "Connected Services"**
5. **Click "Add Reference" or "New Reference"**
6. **Select your PostgreSQL database**
7. **This will automatically create the DATABASE_URL variable**

### Alternative: Manually Add Variables

If you can't find "Service References":

1. Go to PostgreSQL service
2. Click "Variables" tab
3. Copy these variable names (you'll see them):
   - `DATABASE_URL`
   - `PGHOST`
   - `PGPORT`
   - `PGDATABASE`
   - `PGUSER`
   - `PGPASSWORD`

4. Go to your iBITS Portal service
5. Click "Variables" tab
6. Click "Add Variable"
7. Select "Reference" type
8. Choose `DATABASE_URL` from PostgreSQL service

---

## Quick Check

After adding the reference, check your app's Variables tab.
You should see `DATABASE_URL` with a value starting with:
```
postgresql://postgres:...
```

Then redeploy!
