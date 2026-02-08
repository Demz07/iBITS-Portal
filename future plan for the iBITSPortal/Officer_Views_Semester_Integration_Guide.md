# Officer Views - Semester Integration Implementation Guide
**Date:** February 6, 2026  
**Status:** Admin Complete, Officer Pending

---

## ✅ COMPLETED: Admin Controllers

### Successfully Updated:
1. **AdminController.Events()** - Lines 2192-2211
   - Added `semesterFilter` parameter
   - Loads `ViewBag.Semesters` and `ViewBag.SemesterFilter`
   - Includes `.Include(e => e.Semester).ThenInclude(s => s.AcademicYear)`
   - Filters by semester when provided

2. **AdminController.Payments()** - Lines 2721-2740
   - Added `semesterFilter` parameter
   - Loads `ViewBag.Semesters` and `ViewBag.SemesterFilter`
   - Includes `.Include(f => f.Semester).ThenInclude(s => s.AcademicYear)`
   - Filters fees by semester

3. **AdminController.Fines()** - Lines 2839-2863
   - Added `semesterFilter` parameter
   - Loads `ViewBag.Semesters` and `ViewBag.SemesterFilter`
   - Includes `.Include(f => f.Semester).ThenInclude(s => s.AcademicYear)`
   - Filters fines by semester

### Admin Views Updated:
- ✅ Admin/Events.cshtml - Filter + badges + Create/Edit modals
- ✅ Admin/Payments.cshtml - Filter + column + badges
- ✅ Admin/Fines.cshtml - Filter + column + badges

---

## 📋 OFFICER VIEWS TO UPDATE

### Files Requiring Integration:
1. `Views/Officer/ClassFees.cshtml`
2. `Views/Officer/ClassFines.cshtml`
3. `Views/Officer/OrgFees.cshtml`
4. `Views/Officer/OrgFines.cshtml`
5. `Views/Officer/ClassTreasuryDashboard.cshtml`
6. `Views/Officer/OrgTreasurerDashboard.cshtml`
7. `Views/Officer/OrgSecretaryDashboard.cshtml`

---

## 🔧 IMPLEMENTATION PATTERN (Apply to Each File)

### Step 1: Add Semester Filter to View

**Location:** Add after existing filters (around line 400-416 for ClassFees/ClassFines)

```html
<!-- Semester Filter -->
<div class="col-md-4">
    <label class="form-label small text-muted">Semester</label>
    <select id="semesterFilter" name="semesterFilter" class="form-select form-select-sm" onchange="this.form.submit()">
        <option value="">All Semesters</option>
        @if (ViewBag.Semesters != null)
        {
            foreach (var sem in ViewBag.Semesters)
            {
                <option value="@sem.SemesterId" selected="@(ViewBag.SemesterFilter?.ToString() == sem.SemesterId.ToString())">
                    @sem.SemesterName (@sem.AcademicYear.YearName)
                </option>
            }
        }
    </select>
</div>
```

**Note:** Wrap filters in a `<form method="get">` if not already present.

### Step 2: Add Semester Column/Badge to Display

**For table views:** Add column header and data cell showing semester badge  
**For card views:** Add semester badge inside each card

```html
<!-- In table row -->
<td>
    @if (fee.Semester != null)
    {
        <span class="badge bg-info">@fee.Semester.SemesterName</span>
        <small class="text-muted d-block">@fee.Semester.AcademicYear?.YearName</small>
    }
    else
    {
        <span class="text-muted">—</span>
    }
</td>
```

### Step 3: Update Controller Action

**File:** `Controllers/OfficerController.cs`

**Pattern for ClassFees:**
```csharp
public async Task<IActionResult> ClassFees(int? semesterFilter)
{
    // Existing authentication and section detection...
    
    ViewBag.Semesters = await GetActiveSemesters();
    ViewBag.SemesterFilter = semesterFilter;
    
    var feesQuery = _context.Fees
        .Include(f => f.StudentNumNavigation)
        .Include(f => f.Semester).ThenInclude(s => s.AcademicYear)
        .Where(f => f.StudentNumNavigation.YearLevelSection == section);
    
    if (semesterFilter.HasValue)
    {
        feesQuery = feesQuery.Where(f => f.SemesterId == semesterFilter.Value);
    }
    
    var fees = await feesQuery.ToListAsync();
    
    // Existing ViewBag calculations...
    
    return View(fees);
}
```

**Pattern for ClassFines:**
```csharp
public async Task<IActionResult> ClassFines(int? semesterFilter)
{
    // Existing authentication and section detection...
    
    ViewBag.Semesters = await GetActiveSemesters();
    ViewBag.SemesterFilter = semesterFilter;
    
    var finesQuery = _context.Fines
        .Include(f => f.StudentNumNavigation)
        .Include(f => f.Attendance).ThenInclude(a => a.Event)
        .Include(f => f.Semester).ThenInclude(s => s.AcademicYear)
        .Where(f => /* existing section logic */);
    
    if (semesterFilter.HasValue)
    {
        finesQuery = finesQuery.Where(f => f.SemesterId == semesterFilter.Value);
    }
    
    var fines = await finesQuery.ToListAsync();
    
    return View(fines);
}
```

**Pattern for OrgFees/OrgFines:**
```csharp
public async Task<IActionResult> OrgFees(int? semesterFilter)
{
    ViewBag.Semesters = await GetActiveSemesters();
    ViewBag.SemesterFilter = semesterFilter;
    
    var feesQuery = _context.Fees
        .Include(f => f.StudentNumNavigation)
        .Include(f => f.Semester).ThenInclude(s => s.AcademicYear)
        .Where(f => f.FeeName == "iBITS Org Fee" || f.FeeName == "Org Fee");
    
    if (semesterFilter.HasValue)
    {
        feesQuery = feesQuery.Where(f => f.SemesterId == semesterFilter.Value);
    }
    
    var fees = await feesQuery.ToListAsync();
    
    return View(fees);
}
```

### Step 4: Update Dashboards

**ClassTreasuryDashboard, OrgTreasurerDashboard, OrgSecretaryDashboard:**

1. Add semester filter dropdown at top
2. Update aggregation queries to filter by semester
3. Display semester context in header (e.g., "Showing data for: [Semester Name]")

```csharp
public async Task<IActionResult> ClassTreasuryDashboard(int? semesterFilter)
{
    ViewBag.Semesters = await GetActiveSemesters();
    ViewBag.SemesterFilter = semesterFilter;
    
    // Apply semesterFilter to all stat queries
    var totalFees = await _context.Fees
        .Where(f => /* section match */ && (!semesterFilter.HasValue || f.SemesterId == semesterFilter.Value))
        .SumAsync(f => f.Amount ?? 0);
    
    // ... other stats with same pattern
    
    return View();
}
```

---

## 🎯 DETAILED FILE-BY-FILE CHECKLIST

### 1. ClassFees.cshtml (Lines ~400-416)
- [x] Add semester filter dropdown (after Status filter)
- [ ] Add semester badge in fee records (inside `.fee-category-card` loop)
- [ ] Wrap filters in `<form method="get" asp-action="ClassFees">`

**Controller:** `OfficerController.ClassFees()` (Line ~2668)
- [ ] Add `semesterFilter` parameter
- [ ] Load `ViewBag.Semesters` and `ViewBag.SemesterFilter`
- [ ] Include `.Include(f => f.Semester).ThenInclude(s => s.AcademicYear)`
- [ ] Apply `.Where(f => !semesterFilter.HasValue || f.SemesterId == semesterFilter.Value)`

### 2. ClassFines.cshtml
- [ ] Add semester filter dropdown
- [ ] Add semester column/badge in fines table
- [ ] Wrap filters in form

**Controller:** `OfficerController.ClassFines()` (Line ~2743)
- [ ] Add `semesterFilter` parameter
- [ ] Load ViewBag
- [ ] Include Semester navigation
- [ ] Apply filter

### 3. OrgFees.cshtml
- [ ] Add semester filter
- [ ] Add semester badge display

**Controller:** `OfficerController.OrgFees()` (Line ~1737)
- [ ] Add `semesterFilter` parameter
- [ ] Load ViewBag
- [ ] Include Semester
- [ ] Apply filter

### 4. OrgFines.cshtml
- [ ] Add semester filter
- [ ] Add semester badge

**Controller:** `OfficerController.OrgFines()` (Line ~1808)
- [ ] Add `semesterFilter` parameter
- [ ] Load ViewBag
- [ ] Include Semester
- [ ] Apply filter

### 5. ClassTreasuryDashboard.cshtml
- [ ] Add semester filter at top
- [ ] Show active semester context

**Controller:** `OfficerController.ClassTreasuryDashboard()` (Line ~1245)
- [ ] Add `semesterFilter` parameter
- [ ] Apply to all aggregation queries

### 6. OrgTreasurerDashboard.cshtml
- [ ] Add semester filter
- [ ] Show semester context

**Controller:** `OfficerController.OrgTreasurerDashboard()` (Line ~661)
- [ ] Add `semesterFilter` parameter
- [ ] Apply to aggregations

### 7. OrgSecretaryDashboard.cshtml
- [ ] Add semester filter
- [ ] Filter attendance/event data by semester

**Controller:** `OfficerController.OrgSecretaryDashboard()` (Line ~922)
- [ ] Add `semesterFilter` parameter
- [ ] Apply to queries

---

## 📊 HELPER METHOD

**Add to OfficerController if not present:**

```csharp
private async Task<List<Semester>> GetActiveSemesters()
{
    return await _context.Semesters
        .Include(s => s.AcademicYear)
        .Where(s => s.IsActive)
        .OrderByDescending(s => s.StartDate)
        .ToListAsync();
}
```

---

## 🧪 TESTING CHECKLIST

After each file update:
- [ ] Page loads without errors
- [ ] Semester dropdown populates
- [ ] Filtering by semester works
- [ ] Data displays correctly
- [ ] Stats/totals reflect filtered data
- [ ] Reset/clear filter returns to "All Semesters"

---

## 📝 NOTES

1. **Export Functions:** Update these too:
   - `ExportOrgFees()` (Line ~2835)
   - `ExportOrgFines()` (Line ~2860)
   - `ExportClassFees()` (Line ~2890)
   - `ExportClassFines()` (Line ~2926)
   - Add `semesterFilter` parameter and apply to queries

2. **Bulk Actions:** Verify these still work after semester integration:
   - `BulkMarkOrgFinesAsPaid()` (Line ~4340)
   - `BulkRevokeOrgFines()` (Line ~4413)
   - `BulkMarkOrgFeesAsPaid()` (Line ~4464)
   - `BulkRevokeOrgFees()` (Line ~4535)

3. **Remittance:** If remittance pages exist, add semester context there too.

---

## 🚀 QUICK START

1. Start with ClassFees (easiest, already has filters)
2. Copy pattern to ClassFines
3. Apply same to OrgFees/OrgFines
4. Update dashboards last

**Estimated Time:** 2-3 hours for all Officer views

---

## ✅ AFTER COMPLETION

- Test end-to-end workflows
- Update any remaining exports
- Move to Scanner integration (Phase 3)
- Document any issues or edge cases

---

**Questions?** Refer back to Admin controller patterns (Events/Payments/Fines) as reference examples.
