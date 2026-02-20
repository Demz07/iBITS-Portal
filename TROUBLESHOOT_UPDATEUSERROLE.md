# UpdateUserRole Troubleshooting Guide

## Current Status
✅ **Method exists**: Line 4200 in AdminController.cs
✅ **HTTP attributes**: `[HttpPost]` and `[ValidateAntiForgeryToken]`
✅ **Authorization**: `[Authorize(Roles = "Admin")]` on controller
✅ **Form is correct**: Has `asp-action="UpdateUserRole"`, antiforgery token, and all required fields

## Expected Route
```
POST /Admin/UpdateUserRole
```

## Method Signature
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateUserRole(string studentNum, string newRole, string adminPassword)
```

## Form Fields (from StudentRecords.cshtml line 592-647)
- `studentNum` (hidden input, id="modalStudentNum")
- `newRole` (select dropdown, id="modalRoleSelect")
- `adminPassword` (password input, id="adminPasswordInput", **required**)

## Common Issues & Solutions

### 1. **404 Not Found Error**
**Symptom**: Page shows "404" or "Not Found"
**Cause**: Route mismatch
**Solution**: 
- The form uses `asp-action="UpdateUserRole"` which should route to `/Admin/UpdateUserRole`
- Make sure you're logged in as Admin
- Check browser network tab for actual URL being posted

### 2. **405 Method Not Allowed**
**Symptom**: "HTTP 405 Method Not Allowed"
**Cause**: POST request not accepted
**Solution**:
- Verify `[HttpPost]` attribute exists (✅ confirmed at line 4198)
- Check for any route attribute overrides

### 3. **400 Bad Request / Antiforgery Token Error**
**Symptom**: "The required antiforgery token was not supplied"
**Cause**: Missing or invalid antiforgery token
**Solution**:
- Form has `@Html.AntiForgeryToken()` (✅ confirmed at line 593)
- Clear browser cookies and try again
- Check if HTTPS is configured correctly

### 4. **403 Forbidden**
**Symptom**: "Access Denied" or 403 error
**Cause**: Not logged in as Admin
**Solution**:
- Verify you're logged in with admin@ibits.edu.ph
- Check that user has "Admin" role in database

### 5. **Password Validation Fails**
**Symptom**: Redirects back with error "Invalid admin password"
**Cause**: Wrong password entered
**Solution**:
- Use the correct admin password: `Admin@123`
- Password field is **required** (line 628)

### 6. **User Not Found**
**Symptom**: Error "User {studentNum} not found"
**Cause**: Student doesn't have Identity user account
**Solution**:
- Student must be registered in AspNetUsers table
- Check if student has an account created

## How to Test

### Test 1: Check if route is accessible
```powershell
# On Railway, check logs for routing errors
# Look for "No action matched" or similar routing errors
```

### Test 2: Verify Admin is logged in
```sql
-- Run in Railway PostgreSQL Data tab
SELECT u."Email", r."Name" 
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@ibits.edu.ph';
```
Expected result: Should show "Admin" role

### Test 3: Check browser console
1. Open browser Developer Tools (F12)
2. Go to Network tab
3. Try to update a role
4. Look for the POST request to `/Admin/UpdateUserRole`
5. Check:
   - **Status Code**: Should be 302 (redirect) on success, not 404/405/500
   - **Form Data**: Should include studentNum, newRole, adminPassword
   - **Response**: Check for error messages

### Test 4: Check Railway logs
1. Go to Railway dashboard
2. Click your web service
3. Click "Deployments" → Latest deployment
4. Click "View Logs"
5. Look for errors when you submit the form

## What to Look For in Logs

**Success:**
```
POST /Admin/UpdateUserRole - 302 Redirect
Activity Log: Updated user role...
```

**Failure Examples:**
```
POST /Admin/UpdateUserRole - 404 Not Found
→ Route not registered correctly

POST /Admin/UpdateUserRole - 500 Internal Server Error
→ Exception in method (check full stack trace)

POST /Admin/UpdateUserRole - 403 Forbidden
→ Not logged in as Admin
```

## Quick Diagnostic Commands

Run these in Railway's PostgreSQL:

```sql
-- 1. Check if admin exists and has correct role
SELECT u."Email", u."UserName", r."Name" as Role
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@ibits.edu.ph';

-- 2. Check if a test student has an Identity account
SELECT "Email", "UserName", "EmailConfirmed"
FROM "AspNetUsers"
WHERE "UserName" LIKE 'IBITS-%'
LIMIT 5;

-- 3. Check available roles
SELECT "Id", "Name" FROM "AspNetRoles" ORDER BY "Name";
```

## Next Steps

Please provide:
1. **Exact error message** you see (screenshot if possible)
2. **HTTP status code** from browser Network tab
3. **Railway logs** at the time of the error
4. **Browser console errors** (if any)

This will help me pinpoint the exact issue!
