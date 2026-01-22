# 🔍 Search & Filter Testing Guide

**Your Application:** http://localhost:5242

---

## ✅ What I Just Fixed

### **Issue:** 
The dropdown selections weren't being preserved because the syntax was incorrect.

### **Solution:**
Changed from:
```html
<option value="1-" selected="@IsSelected("YearFilter", "1-")">1st Year</option>
```

To:
```html
<option value="1-" @(ViewBag.YearFilter == "1-" ? "selected" : "")>1st Year</option>
```

This properly checks if the filter value matches and adds the `selected` attribute.

---

## 🧪 How to Test Search & Filters

### **Step 1: Access the Page**
1. Open: http://localhost:5242
2. Login as **Admin**
3. Go to: **Admin Dashboard** → **Student Records**

---

### **Step 2: Test Search (Should Work)**

**Test A: Search by Name**
1. Type **"John"** in the search box
2. Press **Enter** or click the **Filter** button (funnel icon)
3. ✅ **Expected:** Only students with "John" in their name appear
4. ✅ **Check:** URL should show `?searchString=John`

**Test B: Search by Student ID**
1. Clear search, type a student ID (e.g., **"2023-00001"**)
2. Press Enter
3. ✅ **Expected:** That specific student appears

---

### **Step 3: Test Year Level Filter**

**Test C: Filter by Year**
1. Clear any search text
2. Select **"2nd Year"** from the Year Level dropdown
3. Click the **Filter** button (funnel icon)
4. ✅ **Expected:** Only 2nd year students appear
5. ✅ **Check:** 
   - URL should show `?yearFilter=2-`
   - Dropdown should still show "2nd Year" selected
   - All students shown should have sections starting with "2-"

---

### **Step 4: Test Status Filter**

**Test D: Filter by Status**
1. Select **"Active"** from Status dropdown
2. Click Filter
3. ✅ **Expected:** Only active students appear
4. ✅ **Check:** URL should show `?statusFilter=Active`

---

### **Step 5: Test Role Filter**

**Test E: Filter by Role**
1. Select **"Officer"** from Role dropdown
2. Click Filter
3. ✅ **Expected:** Only students who are officers appear
4. ✅ **Check:** 
   - URL should show `?roleFilter=Officer`
   - All shown students should have "Officer" badge in the Role column

---

### **Step 6: Test Combined Filters**

**Test F: Search + Multiple Filters**
1. Type **"BSIT"** in search box
2. Select **"3rd Year"** from Year Level
3. Select **"Active"** from Status
4. Select **"Officer"** from Role
5. Click Filter
6. ✅ **Expected:** Only active 3rd year BSIT officers appear
7. ✅ **Check:** URL should have all parameters:
   ```
   ?searchString=BSIT&yearFilter=3-&statusFilter=Active&roleFilter=Officer
   ```

---

### **Step 7: Test Pagination with Filters**

**Test G: Filters Persist Across Pages**
1. Apply a filter (e.g., "2nd Year")
2. Go to **Page 2** using pagination
3. ✅ **Expected:** 
   - Still seeing 2nd year students
   - Filter dropdown still shows "2nd Year" selected
   - URL maintains `yearFilter=2-`

---

### **Step 8: Test Reset Button**

**Test H: Reset All Filters**
1. Apply multiple filters
2. Click the **Reset** button (circular arrow icon)
3. ✅ **Expected:** 
   - All dropdowns reset to "All..."
   - Search box cleared
   - All students appear
   - URL has no filter parameters

---

### **Step 9: Test Excel Export with Filters**

**Test I: Export Filtered Data**
1. Apply a filter (e.g., "Active" status)
2. Click **"Export to Excel"** button
3. ✅ **Expected:**
   - Excel file downloads
   - File contains ONLY filtered students
   - Filename includes timestamp

---

## 🐛 Troubleshooting

### **If Search Doesn't Work:**
- **Check:** Did you press Enter or click the Filter button?
- **Check:** Look at the URL - does it show `?searchString=...`?
- **Try:** Refresh the page (F5) and try again

### **If Filters Don't Work:**
- **Check:** Did you click the Filter button (funnel icon)?
- **Check:** Look at the URL - does it show filter parameters?
- **Check:** Are the dropdowns showing your selection after clicking Filter?
- **Try:** Use browser DevTools (F12) → Network tab to see the request

### **If Dropdowns Reset After Filtering:**
- **This was the bug I just fixed!**
- **Try:** Hard refresh the page (Ctrl+F5)
- **Check:** Make sure the app reloaded with changes

---

## 📊 What Each Filter Does

| Filter | Parameter | Logic | Example |
|--------|-----------|-------|---------|
| **Search** | `searchString` | Searches across 7 fields | `?searchString=John` |
| **Year Level** | `yearFilter` | Matches start of YearLevelSection | `?yearFilter=2-` finds "2-A", "2-B" |
| **Status** | `statusFilter` | Exact match on Classification | `?statusFilter=Active` |
| **Role** | `roleFilter` | Checks if OfficerId is null/not null | `?roleFilter=Officer` finds students with OfficerId |

---

## ✅ Expected Behavior

### **After Applying Filters:**
1. ✅ Results should update immediately
2. ✅ Dropdowns should show selected values
3. ✅ URL should reflect all parameters
4. ✅ Pagination should maintain filters
5. ✅ Export should respect filters
6. ✅ Total count badge should update

### **After Clicking Reset:**
1. ✅ All filters cleared
2. ✅ Search box empty
3. ✅ All students visible
4. ✅ URL clean (no parameters)

---

## 🎯 Quick Test Checklist

- [ ] Search by name works
- [ ] Search by ID works
- [ ] Year Level filter works
- [ ] Status filter works
- [ ] Role filter works
- [ ] Combined search + filters work
- [ ] Filters persist on page 2
- [ ] Dropdowns stay selected after filtering
- [ ] Reset button clears everything
- [ ] Export respects current filters
- [ ] Total count updates with filters

---

## 🚀 If Everything Works

Congratulations! Your search and filters are now fully functional. You can:
- Filter students by year, status, and role
- Search across multiple fields
- Combine search with filters
- Export filtered data to Excel
- Navigate pages while keeping filters

---

## 📞 If Issues Persist

If search/filters still don't work after testing:

1. **Hard refresh the page:** Ctrl+F5
2. **Check browser console:** F12 → Console tab (look for errors)
3. **Check the URL:** After clicking Filter, does it change?
4. **Test in incognito mode:** Rule out caching issues

Let me know what specific behavior you're seeing!

---

**Application URL:** http://localhost:5242  
**Page to Test:** Admin → Student Records  
**Status:** Code updated, hot reload should apply changes  
**Date:** January 10, 2026
