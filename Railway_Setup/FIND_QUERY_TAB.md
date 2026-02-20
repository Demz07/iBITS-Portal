# How to Find Railway PostgreSQL Query Interface

Railway's UI has different ways to access the database depending on the view:

## Method 1: Through PostgreSQL Service

1. **In your Railway project dashboard**, you should see 2 services:
   - Your app (iBITS Portal)
   - PostgreSQL (database icon)

2. **Click on the PostgreSQL service**

3. **Look for these tabs at the top:**
   - Data
   - Settings
   - Deployments
   - Metrics
   - **Query** ← This one!

If you don't see "Query", try these alternatives:

## Method 2: Data Tab

1. Click on PostgreSQL service
2. Click **"Data"** tab
3. You might see a table view with an option to run SQL queries
4. Look for a "Run Query" or "SQL" button

## Method 3: Connect Tab

1. Click on PostgreSQL service
2. Look for **"Connect"** tab
3. You'll see connection details
4. Use a tool like:
   - Railway CLI: `railway connect postgres`
   - pgAdmin (external tool)
   - Any PostgreSQL client

## Method 4: Use Railway CLI (Easiest!)

If you have Railway CLI installed:

```bash
railway link
railway connect postgres
```

Then paste the SQL script!

## Alternative: I Can Create Migration Files

If Railway's Query interface is hard to find, I can:
1. Create EF Core migrations instead
2. Your app will auto-create tables on first run
3. Then we just import the data

---

**What do you see when you click on PostgreSQL?**
- Tabs at the top?
- Buttons/options?
- Take a screenshot if easier!
