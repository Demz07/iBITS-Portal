# Debug Railway Connection

## Most Common Issues:

### 1. DATABASE_URL Not Set
**Check:** Railway → iBITS Portal → Variables tab
- Should see: `DATABASE_URL` = `${{Postgres.DATABASE_URL}}`

**Fix:** 
- Click "New Variable"
- Select "Add Reference"
- Choose PostgreSQL service
- Select DATABASE_URL

### 2. PostgreSQL Not Linked
**Check:** Are both services in the same project?
- iBITS Portal (app)
- Postgres (database)

**Fix:** Both should be in the same Railway project

### 3. Variable Format Wrong
**Check:** In Variables tab, DATABASE_URL should show a reference like:
- `${{Postgres.DATABASE_URL}}` or
- Actual connection string starting with `postgresql://`

---

## Quick Debug Steps:

1. **iBITS Portal → Variables tab**
   - What variables do you see?
   - Is DATABASE_URL there?

2. **PostgreSQL → Variables tab**
   - Do you see DATABASE_URL there?
   - Copy that value

3. **Check if services are linked**
   - Both in same project?

---

Please tell me what you see in the Variables tab!
