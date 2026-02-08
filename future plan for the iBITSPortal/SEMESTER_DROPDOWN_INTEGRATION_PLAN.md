# Semester & Academic Year Dropdown Integration Plan

**Project**: iBITS Portal Enhancement  
**Feature**: Integrate Semester Selector into Existing Academic Year Dropdown  
**Date Created**: February 8, 2026  
**Estimated Duration**: 4-6 hours  
**Priority**: High  
**Status**: Planning Phase  

---

## 📋 Table of Contents

1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [Design Approach](#design-approach)
4. [Implementation Plan](#implementation-plan)
5. [Code Examples](#code-examples)
6. [Testing Strategy](#testing-strategy)
7. [Timeline & Deliverables](#timeline--deliverables)

---

## 🎯 Executive Summary

### Objective
Enhance the existing Academic Year dropdown (`id="academicYearSelect"`) in both Admin and Student layouts to include **Semester selection** functionality, enabling historical data viewing with read-only protection.

### Key Changes

**Admin Layout** (`_AdminLayout.cshtml`):
- Replace static Academic Year dropdown (lines 207-221) with dynamic Semester + Academic Year selector
- Enable semester switching with historical mode
- Add visual indicators for current vs. historical data
- Maintain existing styling and positioning

**Student Layout** (`_StudentLayout.cshtml`):
- Replace read-only Academic Year badge (lines 271-277) with Semester + Academic Year selector
- Enable Org Officers to switch semesters when viewing their management pages
- Keep it read-only for regular students
- Maintain existing badge styling

### Core Functionality

**For Admins**:
- ✅ Dropdown shows: "Academic Year 2025-2026 - 1st Semester ⭐"
- ✅ Click to select historical semesters
- ✅ System-wide data filtering by selected semester
- ✅ Historical mode auto-disables edit/delete actions

**For Org Officers** (via Student Layout):
- ✅ Same dropdown functionality as Admin
- ✅ Filter their fees, fines, events by semester
- ✅ Historical mode prevents modifications

**For Regular Students**:
- ✅ Display current semester only (read-only badge)
- ✅ No dropdown functionality needed

---

## 📊 Current State Analysis

### 1. Admin Layout (`_AdminLayout.cshtml`)

**Current Implementation** (Lines 207-221):
```html
<!-- Academic Year Selector -->
<div class="academic-year-selector d-none d-md-flex align-items-center gap-2">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
    <select id="academicYearSelect" class="form-select academic-year-dropdown" title="Set Current Academic Year">
        @{
            int currentYear = DateTime.Now.Year;
            string currentAY = ViewBag.CurrentAcademicYear ?? $"A.Y. {currentYear}-{currentYear + 1}";
            for (int year = currentYear + 1; year >= currentYear - 5; year--)
            {
                string ayValue = $"A.Y. {year}-{year + 1}";
                <option value="@ayValue" selected="@(currentAY == ayValue)">@ayValue</option>
            }
        }
    </select>
</div>
```

**Current JavaScript** (Lines 304-318):
```javascript
// Academic Year AJAX logic
$('#academicYearSelect').on('change', function () {
    var selectedYear = $(this).val();
    $.ajax({
        url: '@Url.Action("SetCurrentAcademicYear", "Admin")',
        type: 'POST',
        data: { academicYear: selectedYear },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function (response) {
            if (response.success && typeof showToast === 'function') {
                showToast('Academic year updated to ' + selectedYear, 'success');
            }
        }
    });
});
```

**Issues with Current Approach**:
- ❌ Only shows Academic Year, not Semester
- ❌ Generates years dynamically (2026, 2025, 2024...) instead of pulling from database
- ❌ No concept of "current" vs "historical"
- ❌ No visual indicator for which is active
- ❌ No read-only mode for historical data

### 2. Student Layout (`_StudentLayout.cshtml`)

**Current Implementation** (Lines 271-277):
```html
<!-- Academic Year Display (Read-Only for Students) -->
<div class="academic-year-badge d-none d-md-flex align-items-center gap-2" title="Current Academic Year">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1rem;"></i>
    <span id="currentAcademicYear" class="academic-year-text">
        @(ViewBag.CurrentAcademicYear ?? $"A.Y. {DateTime.Now.Year}-{DateTime.Now.Year + 1}")
    </span>
</div>
```

**Current Behavior**:
- ✅ Read-only display (no dropdown)
- ✅ Shows current academic year
- ❌ No semester information
- ❌ No selector for Org Officers

**Requirement**:
- ✅ Keep read-only badge for regular students
- ✅ Add dropdown for Org Officers (Org Treasurer, Org Secretary, Class Treasurer, Class Secretary)
- ✅ Show semester + academic year

---

## 🏗️ Design Approach

### Option 1: Replace with Database-Driven Semester Selector (RECOMMENDED)

**Advantages**:
- ✅ Single source of truth (database)
- ✅ Accurate semester data
- ✅ Supports historical viewing
- ✅ Works with existing semester infrastructure

**Implementation**:
```html
<!-- NEW: Semester + Academic Year Selector -->
<div class="semester-selector d-none d-md-flex align-items-center gap-2">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
    <select id="semesterSelect" class="form-select semester-dropdown" title="Select Semester">
        <option value="">Loading semesters...</option>
    </select>
</div>
```

**Data Format** (from database):
```json
[
  {
    "semesterId": 5,
    "displayName": "A.Y. 2025-2026 - 1st Semester",
    "isCurrent": true
  },
  {
    "semesterId": 4,
    "displayName": "A.Y. 2024-2025 - 2nd Semester",
    "isCurrent": false
  }
]
```

### Option 2: Keep Academic Year + Add Semester Separately

**Disadvantages**:
- ❌ Two dropdowns (clutters navbar)
- ❌ Need to sync both selections
- ❌ Confusing UX

**Verdict**: ❌ NOT RECOMMENDED

---

## 📋 Implementation Plan

### Phase 1: Backend Endpoints (1 hour)

#### Step 1.1: Create Semester Selector Endpoint

**File**: `Controllers/AdminController_Semester.cs`

Add this method:

```csharp
/// <summary>
/// Returns all semesters for dropdown selector (Admin and Org Officers)
/// </summary>
[HttpGet]
public async Task<IActionResult> GetAllSemestersForDropdown()
{
    try
    {
        var semesters = await _context.Semesters
            .Include(s => s.AcademicYear)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new 
            {
                s.SemesterId,
                s.SemesterName,
                AcademicYear = s.AcademicYear.YearName,
                s.IsCurrent,
                DisplayName = s.AcademicYear.YearName + " - " + s.SemesterName
            })
            .ToListAsync();
        
        return Json(semesters);
    }
    catch (Exception ex)
    {
        return Json(new { error = ex.Message });
    }
}
```

#### Step 1.2: Create Semester Switching Endpoint

**File**: `Controllers/AdminController_Semester.cs`

Add this method:

```csharp
/// <summary>
/// Sets the viewing semester for current user session
/// </summary>
[HttpPost]
public async Task<IActionResult> SetViewingSemester(int? semesterId)
{
    try
    {
        if (semesterId.HasValue)
        {
            var semester = await _context.Semesters
                .Include(s => s.AcademicYear)
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId.Value);
            
            if (semester == null)
                return Json(new { success = false, message = "Semester not found" });
            
            // Store in session
            HttpContext.Session.SetInt32("ViewingSemesterId", semesterId.Value);
            
            return Json(new 
            { 
                success = true, 
                semesterName = semester.SemesterName,
                academicYear = semester.AcademicYear.YearName,
                isHistorical = !semester.IsCurrent,
                displayName = $"{semester.AcademicYear.YearName} - {semester.SemesterName}"
            });
        }
        else
        {
            // Clear session (revert to current)
            HttpContext.Session.Remove("ViewingSemesterId");
            return Json(new { success = true, message = "Viewing current semester" });
        }
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

#### Step 1.3: Make Endpoints Accessible to Officers

**File**: `Controllers/OfficerController.cs`

Add these same methods (or redirect to AdminController endpoints):

```csharp
/// <summary>
/// Get semesters for Org Officers
/// </summary>
[HttpGet]
public async Task<IActionResult> GetAllSemestersForDropdown()
{
    // Reuse logic from AdminController_Semester
    return await RedirectToAction("GetAllSemestersForDropdown", "AdminController_Semester");
}

[HttpPost]
public async Task<IActionResult> SetViewingSemester(int? semesterId)
{
    // Reuse logic from AdminController_Semester
    return await RedirectToAction("SetViewingSemester", "AdminController_Semester", new { semesterId });
}
```

---

### Phase 2: Admin Layout Updates (1.5 hours)

#### Step 2.1: Replace Academic Year Dropdown

**File**: `Views/Shared/_AdminLayout.cshtml`

**FIND** (Lines 207-221):
```html
<!-- Academic Year Selector -->
<div class="academic-year-selector d-none d-md-flex align-items-center gap-2">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
    <select id="academicYearSelect" class="form-select academic-year-dropdown" title="Set Current Academic Year">
        @{
            int currentYear = DateTime.Now.Year;
            string currentAY = ViewBag.CurrentAcademicYear ?? $"A.Y. {currentYear}-{currentYear + 1}";
            for (int year = currentYear + 1; year >= currentYear - 5; year--)
            {
                string ayValue = $"A.Y. {year}-{year + 1}";
                <option value="@ayValue" selected="@(currentAY == ayValue)">@ayValue</option>
            }
        }
    </select>
</div>
```

**REPLACE WITH**:
```html
<!-- Semester + Academic Year Selector -->
<div class="semester-selector-wrapper d-none d-md-flex align-items-center gap-2">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
    <select id="semesterSelect" class="form-select semester-dropdown" title="Select Semester">
        <option value="">Loading semesters...</option>
    </select>
</div>

<!-- Historical Mode Warning (Hidden by default) -->
<div id="historicalModeIndicator" class="d-none align-items-center gap-2 ms-2" style="display: none;">
    <span class="badge bg-warning text-dark">
        <i class="bi bi-lock-fill"></i> Historical View
    </span>
</div>
```

#### Step 2.2: Update CSS Styles

**File**: `Views/Shared/_AdminLayout.cshtml`

**FIND** (Lines 75-90):
```css
/* Academic Year Dropdown Styles */
.academic-year-selector {
    background: rgba(11, 26, 51, 0.6);
    backdrop-filter: blur(20px);
    border: 1px solid rgba(212, 175, 55, 0.4);
    border-radius: 12px;
    padding: 10px 16px;
}

.academic-year-dropdown {
    background: transparent !important;
    border: none !important;
    color: var(--gold-primary) !important;
    font-weight: 700;
}
```

**REPLACE WITH**:
```css
/* Semester Dropdown Styles */
.semester-selector-wrapper {
    background: rgba(11, 26, 51, 0.6);
    backdrop-filter: blur(20px);
    border: 1px solid rgba(212, 175, 55, 0.4);
    border-radius: 12px;
    padding: 10px 16px;
    transition: all 0.3s ease;
}

.semester-selector-wrapper.historical-mode {
    border-color: rgba(255, 193, 7, 0.6);
    background: rgba(255, 193, 7, 0.1);
}

.semester-dropdown {
    background: transparent !important;
    border: none !important;
    color: var(--gold-primary) !important;
    font-weight: 700;
    min-width: 280px;
}

.semester-dropdown option {
    background: var(--bg-primary);
    color: var(--text-strong);
}

#historicalModeIndicator {
    animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
    from { opacity: 0; transform: translateX(-10px); }
    to { opacity: 1; transform: translateX(0); }
}
```

#### Step 2.3: Update JavaScript

**File**: `Views/Shared/_AdminLayout.cshtml`

**FIND** (Lines 304-318):
```javascript
// Academic Year AJAX logic
$('#academicYearSelect').on('change', function () {
    var selectedYear = $(this).val();
    $.ajax({
        url: '@Url.Action("SetCurrentAcademicYear", "Admin")',
        type: 'POST',
        data: { academicYear: selectedYear },
        headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() },
        success: function (response) {
            if (response.success && typeof showToast === 'function') {
                showToast('Academic year updated to ' + selectedYear, 'success');
            }
        }
    });
});
```

**REPLACE WITH**:
```javascript
// Semester Selector Logic
$(document).ready(function() {
    loadSemesters();
    
    $('#semesterSelect').on('change', function () {
        var semesterId = $(this).val();
        switchSemester(semesterId);
    });
});

function loadSemesters() {
    $.ajax({
        url: '@Url.Action("GetAllSemestersForDropdown", "Admin")',
        type: 'GET',
        success: function(data) {
            var dropdown = $('#semesterSelect');
            dropdown.empty();
            
            if (data.error) {
                dropdown.append('<option value="">Error loading semesters</option>');
                return;
            }
            
            $.each(data, function(i, semester) {
                var option = $('<option>')
                    .val(semester.semesterId)
                    .text(semester.displayName);
                
                if (semester.isCurrent) {
                    option.attr('selected', 'selected');
                    option.prepend('⭐ '); // Mark current semester
                }
                
                dropdown.append(option);
            });
        },
        error: function() {
            $('#semesterSelect').append('<option value="">Failed to load</option>');
        }
    });
}

function switchSemester(semesterId) {
    $.ajax({
        url: '@Url.Action("SetViewingSemester", "Admin")',
        type: 'POST',
        data: { semesterId: semesterId },
        headers: { 
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() 
        },
        success: function (response) {
            if (response.success) {
                // Update UI based on historical mode
                if (response.isHistorical) {
                    $('.semester-selector-wrapper').addClass('historical-mode');
                    $('#historicalModeIndicator').removeClass('d-none').css('display', 'flex');
                    
                    if (typeof showToast === 'function') {
                        showToast('Viewing historical data: ' + response.displayName, 'warning');
                    }
                } else {
                    $('.semester-selector-wrapper').removeClass('historical-mode');
                    $('#historicalModeIndicator').addClass('d-none');
                    
                    if (typeof showToast === 'function') {
                        showToast('Viewing current semester: ' + response.displayName, 'success');
                    }
                }
                
                // Reload page to refresh data
                setTimeout(function() {
                    location.reload();
                }, 800);
            } else {
                if (typeof showToast === 'function') {
                    showToast('Error: ' + response.message, 'error');
                }
            }
        },
        error: function() {
            if (typeof showToast === 'function') {
                showToast('Failed to switch semester', 'error');
            }
        }
    });
}
```

---

### Phase 3: Student Layout Updates (1.5 hours)

#### Step 3.1: Add Role Detection Logic

**File**: `Views/Shared/_StudentLayout.cshtml`

**Add after line 38** (after role detection logic):
```csharp
// Determine if user is an officer who can switch semesters
bool canSwitchSemester = User.IsInRole("Org Treasurer") 
    || User.IsInRole("Class Treasurer") 
    || User.IsInRole("Org Secretary") 
    || User.IsInRole("Class Secretary");
```

#### Step 3.2: Replace Academic Year Badge

**File**: `Views/Shared/_StudentLayout.cshtml`

**FIND** (Lines 271-277):
```html
<!-- Academic Year Display (Read-Only for Students) -->
<div class="academic-year-badge d-none d-md-flex align-items-center gap-2" title="Current Academic Year">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1rem;"></i>
    <span id="currentAcademicYear" class="academic-year-text">
        @(ViewBag.CurrentAcademicYear ?? $"A.Y. {DateTime.Now.Year}-{DateTime.Now.Year + 1}")
    </span>
</div>
```

**REPLACE WITH**:
```html
@if (canSwitchSemester)
{
    <!-- Semester Selector for Officers -->
    <div class="semester-selector-wrapper d-none d-md-flex align-items-center gap-2">
        <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
        <select id="semesterSelect" class="form-select semester-dropdown" title="Select Semester">
            <option value="">Loading semesters...</option>
        </select>
    </div>
    
    <!-- Historical Mode Indicator -->
    <div id="historicalModeIndicator" class="d-none align-items-center gap-2 ms-2">
        <span class="badge bg-warning text-dark">
            <i class="bi bi-lock-fill"></i> Historical View
        </span>
    </div>
}
else
{
    <!-- Read-Only Badge for Regular Students -->
    <div class="academic-year-badge d-none d-md-flex align-items-center gap-2" title="Current Semester">
        <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1rem;"></i>
        <span id="currentSemesterDisplay" class="academic-year-text">
            @(ViewBag.CurrentSemester ?? "Loading...")
        </span>
    </div>
}
```

#### Step 3.3: Add CSS Styles

**File**: `Views/Shared/_StudentLayout.cshtml`

**ADD** after the existing academic-year-badge styles (after line 97):
```css
/* Semester Selector for Officers */
.semester-selector-wrapper {
    background: rgba(11, 26, 51, 0.6);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border: 1px solid rgba(212, 175, 55, 0.4);
    border-radius: 12px;
    padding: 10px 16px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.2), inset 0 1px 0 rgba(255, 255, 255, 0.05);
    transition: all 0.3s ease;
}

.semester-selector-wrapper.historical-mode {
    border-color: rgba(255, 193, 7, 0.6);
    background: rgba(255, 193, 7, 0.1);
}

.semester-dropdown {
    background: transparent !important;
    border: none !important;
    color: var(--gold-primary) !important;
    font-weight: 700;
    font-size: 0.95rem;
    min-width: 260px;
}

.semester-dropdown option {
    background: var(--bg-primary);
    color: var(--text-strong);
}

/* Light theme adjustments */
[data-theme="light"] .semester-selector-wrapper {
    background: rgba(255, 255, 255, 0.7);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border-color: rgba(212, 175, 55, 0.5);
}

[data-theme="light"] .semester-dropdown {
    color: #b8860b !important;
}

#historicalModeIndicator {
    animation: fadeIn 0.3s ease;
}

@keyframes fadeIn {
    from { opacity: 0; transform: translateX(-10px); }
    to { opacity: 1; transform: translateX(0); }
}
```

#### Step 3.4: Add JavaScript

**File**: `Views/Shared/_StudentLayout.cshtml`

**ADD** before the closing `</body>` tag (around line 362):
```javascript
@if (canSwitchSemester)
{
    <script>
        // Semester Selector Logic for Officers
        $(document).ready(function() {
            loadSemesters();
            
            $('#semesterSelect').on('change', function () {
                var semesterId = $(this).val();
                switchSemester(semesterId);
            });
        });

        function loadSemesters() {
            $.ajax({
                url: '@Url.Action("GetAllSemestersForDropdown", "Officer")',
                type: 'GET',
                success: function(data) {
                    var dropdown = $('#semesterSelect');
                    dropdown.empty();
                    
                    if (data.error) {
                        dropdown.append('<option value="">Error loading semesters</option>');
                        return;
                    }
                    
                    $.each(data, function(i, semester) {
                        var option = $('<option>')
                            .val(semester.semesterId)
                            .text(semester.displayName);
                        
                        if (semester.isCurrent) {
                            option.attr('selected', 'selected');
                            option.prepend('⭐ ');
                        }
                        
                        dropdown.append(option);
                    });
                },
                error: function() {
                    $('#semesterSelect').append('<option value="">Failed to load</option>');
                }
            });
        }

        function switchSemester(semesterId) {
            $.ajax({
                url: '@Url.Action("SetViewingSemester", "Officer")',
                type: 'POST',
                data: { semesterId: semesterId },
                success: function (response) {
                    if (response.success) {
                        if (response.isHistorical) {
                            $('.semester-selector-wrapper').addClass('historical-mode');
                            $('#historicalModeIndicator').removeClass('d-none').css('display', 'flex');
                        } else {
                            $('.semester-selector-wrapper').removeClass('historical-mode');
                            $('#historicalModeIndicator').addClass('d-none');
                        }
                        
                        // Reload to refresh data
                        setTimeout(function() {
                            location.reload();
                        }, 800);
                    }
                }
            });
        }
    </script>
}
else
{
    <script>
        // Load current semester display for regular students
        $(document).ready(function() {
            $.ajax({
                url: '@Url.Action("GetCurrentSemesterDisplay", "Student")',
                type: 'GET',
                success: function(data) {
                    if (data.displayName) {
                        $('#currentSemesterDisplay').text(data.displayName);
                    }
                }
            });
        });
    </script>
}
```

---

### Phase 4: Controller Support for Students (30 minutes)

#### Step 4.1: Add Student Controller Method

**File**: `Controllers/StudentController.cs`

```csharp
/// <summary>
/// Get current semester display for regular students (read-only)
/// </summary>
[HttpGet]
public async Task<IActionResult> GetCurrentSemesterDisplay()
{
    try
    {
        var currentSemester = await _context.Semesters
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.IsCurrent && s.IsActive);
        
        if (currentSemester == null)
        {
            return Json(new { displayName = "No active semester" });
        }
        
        string displayName = $"{currentSemester.AcademicYear.YearName} - {currentSemester.SemesterName}";
        
        return Json(new { 
            displayName = displayName,
            semesterName = currentSemester.SemesterName,
            academicYear = currentSemester.AcademicYear.YearName
        });
    }
    catch (Exception ex)
    {
        return Json(new { displayName = "Error loading semester" });
    }
}
```

---


## ?? Code Examples

### Complete Admin Layout Replacement

**Before** (Current):
```html
<div class="academic-year-selector d-none d-md-flex align-items-center gap-2">
    <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
    <select id="academicYearSelect" class="form-select academic-year-dropdown">
        <!-- Hardcoded year options -->
    </select>
</div>
```

**After** (New):
```html
<div class="d-flex align-items-center gap-2">
    <!-- Semester Selector -->
    <div class="semester-selector-wrapper d-none d-md-flex align-items-center gap-2">
        <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
        <select id="semesterSelect" class="form-select semester-dropdown" title="Select Semester">
            <option value="">Loading semesters...</option>
        </select>
    </div>
    
    <!-- Historical Mode Indicator -->
    <div id="historicalModeIndicator" class="d-none align-items-center gap-2" style="display: none;">
        <span class="badge bg-warning text-dark">
            <i class="bi bi-lock-fill"></i> Historical View
        </span>
    </div>
</div>
```

### Complete Student Layout Implementation

**For Org Officers** (conditional rendering):
```html
@{
    bool canSwitchSemester = User.IsInRole("Org Treasurer") 
        || User.IsInRole("Class Treasurer") 
        || User.IsInRole("Org Secretary") 
        || User.IsInRole("Class Secretary");
}

@if (canSwitchSemester)
{
    <!-- Same as Admin Layout -->
    <div class="semester-selector-wrapper d-none d-md-flex align-items-center gap-2">
        <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1.1rem;"></i>
        <select id="semesterSelect" class="form-select semester-dropdown" title="Select Semester">
            <option value="">Loading semesters...</option>
        </select>
    </div>
}
else
{
    <!-- Read-Only for Regular Students -->
    <div class="academic-year-badge d-none d-md-flex align-items-center gap-2" title="Current Semester">
        <i class="bi bi-calendar-range" style="color: var(--gold-primary); font-size: 1rem;"></i>
        <span id="currentSemesterDisplay" class="academic-year-text">
            Loading...
        </span>
    </div>
}
```

---

## ? Testing Strategy

### Test Case 1: Admin Semester Switching

**Steps**:
1. Login as Admin
2. Navigate to Admin Dashboard
3. Check navbar - verify semester dropdown appears
4. Verify current semester is marked with ?
5. Select a historical semester from dropdown
6. Verify "Historical View" badge appears
7. Navigate to Fees Management
8. Verify only fees from selected semester are shown
9. Attempt to create a new fee
10. Verify error message: "Cannot create fees in historical view mode"

**Expected Result**:
- ? Dropdown loads with all semesters
- ? Current semester pre-selected
- ? Historical badge appears when switching
- ? Data filtered correctly
- ? Create/Edit/Delete blocked in historical mode

### Test Case 2: Org Officer Semester Switching

**Steps**:
1. Login as Org Treasurer
2. Navigate to Dashboard (Student Layout)
3. Verify semester dropdown appears in navbar
4. Select historical semester
5. Navigate to Fees Management
6. Verify fees from selected semester shown
7. Attempt to create new fee
8. Verify blocked with error message

**Expected Result**:
- ? Dropdown appears for officers
- ? Historical mode works correctly
- ? Data filtered by semester
- ? Modifications blocked

### Test Case 3: Regular Student View

**Steps**:
1. Login as regular Student (no officer roles)
2. Navigate to Dashboard
3. Check navbar

**Expected Result**:
- ? Only current semester displayed (read-only badge)
- ? No dropdown selector
- ? Display shows: "A.Y. 2025-2026 - 1st Semester"

### Test Case 4: Database-Driven Dropdown

**Steps**:
1. Access database
2. Create new semester: "A.Y. 2026-2027 - 1st Semester"
3. Refresh admin page
4. Check dropdown

**Expected Result**:
- ? New semester appears in dropdown immediately
- ? No code changes needed
- ? Sorted by date (newest first)

### Test Case 5: Session Persistence

**Steps**:
1. Select historical semester
2. Navigate between pages (Dashboard ? Fees ? Events)
3. Check if selection persists

**Expected Result**:
- ? Selected semester remains across navigation
- ? Historical badge visible on all pages
- ? Data consistently filtered

### Test Case 6: Multiple Admins

**Steps**:
1. Admin A selects Semester 1
2. Admin B selects Semester 2 (different browser)
3. Verify isolation

**Expected Result**:
- ? Each admin sees their selected semester
- ? No cross-contamination
- ? Session isolation working

---

## ?? Timeline & Deliverables

### Implementation Timeline

| Phase | Task | Duration | Assignee |
|-------|------|----------|----------|
| **Phase 1** | Backend endpoints (AdminController_Semester, OfficerController) | 1 hour | Developer |
| **Phase 2** | Admin Layout updates (HTML, CSS, JS) | 1.5 hours | Developer |
| **Phase 3** | Student Layout updates (conditional rendering) | 1.5 hours | Developer |
| **Phase 4** | Student Controller support | 30 mins | Developer |
| **Phase 5** | Testing all scenarios | 1 hour | QA/Developer |
| **Phase 6** | Bug fixes and polish | 30 mins | Developer |
| **TOTAL** | | **6 hours** | |

### Deliverables Checklist

**Backend Files** (Modified):
- [ ] `Controllers/AdminController_Semester.cs` - Add GetAllSemestersForDropdown()
- [ ] `Controllers/AdminController_Semester.cs` - Add SetViewingSemester()
- [ ] `Controllers/OfficerController.cs` - Add same methods
- [ ] `Controllers/StudentController.cs` - Add GetCurrentSemesterDisplay()

**Frontend Files** (Modified):
- [ ] `Views/Shared/_AdminLayout.cshtml` - Replace dropdown (lines 207-221)
- [ ] `Views/Shared/_AdminLayout.cshtml` - Update CSS (lines 75-90)
- [ ] `Views/Shared/_AdminLayout.cshtml` - Update JavaScript (lines 304-318)
- [ ] `Views/Shared/_StudentLayout.cshtml` - Add role detection (after line 38)
- [ ] `Views/Shared/_StudentLayout.cshtml` - Replace badge (lines 271-277)
- [ ] `Views/Shared/_StudentLayout.cshtml` - Add CSS styles
- [ ] `Views/Shared/_StudentLayout.cshtml` - Add JavaScript

**Total Files Modified**: 4 files  
**Lines of Code Changed**: ~300 lines

---

## ?? Visual Design

### Current vs. New Design

**Current Admin Navbar**:
```
[Menu] [PUP Logo] ............... [A.Y. 2025-2026 ?] | [Admin] [??]
```

**New Admin Navbar** (Current Semester):
```
[Menu] [PUP Logo] ............... [? A.Y. 2025-2026 - 1st Semester ?] | [Admin] [??]
```

**New Admin Navbar** (Historical Mode):
```
[Menu] [PUP Logo] ........ [A.Y. 2024-2025 - 2nd Semester ?] [?? Historical View] | [Admin] [??]
```

**Current Student Navbar** (Regular Student):
```
[Menu] [PUP Logo] ................... [?? A.Y. 2025-2026] | [John Doe] [??]
```

**New Student Navbar** (Regular Student - Read-Only):
```
[Menu] [PUP Logo] ............. [?? A.Y. 2025-2026 - 1st Semester] | [John Doe] [??]
```

**New Student Navbar** (Org Officer):
```
[Menu] [PUP Logo] ........ [? A.Y. 2025-2026 - 1st Semester ?] | [Jane Doe] [??]
```

---

## ?? Important Notes

### 1. Backward Compatibility

**Issue**: Existing controllers may not have semester filtering
**Solution**: Implement gradually with fallback

```csharp
public async Task<IActionResult> Payments()
{
    // NEW: Get selected semester (with fallback to current)
    var viewingSemester = await _semesterService.GetSelectedSemesterAsync();
    
    if (viewingSemester != null)
    {
        // Filter by semester
        var fees = await _context.Fees
            .Where(f => f.SemesterId == viewingSemester.SemesterId)
            .ToListAsync();
    }
    else
    {
        // Fallback: Show all fees (backward compatible)
        var fees = await _context.Fees.ToListAsync();
    }
    
    return View(fees);
}
```

### 2. Old SetCurrentAcademicYear Method

**Action**: Keep or Remove?

**Option A**: Remove completely (RECOMMENDED)
```csharp
// DELETE THIS METHOD from AdminController.cs
[HttpPost]
public async Task<IActionResult> SetCurrentAcademicYear(string academicYear)
{
    // This is no longer needed
}
```

**Option B**: Keep for backward compatibility
```csharp
// DEPRECATE but keep for now
[Obsolete("Use SetViewingSemester instead")]
[HttpPost]
public async Task<IActionResult> SetCurrentAcademicYear(string academicYear)
{
    // Redirect to new method
    return await SetViewingSemester(null);
}
```

**Recommendation**: Remove it since we're replacing the entire dropdown.

### 3. Session Configuration

**Ensure session is enabled** in `Program.cs`:

```csharp
// Session Configuration (should already exist)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Use session middleware
app.UseSession();
```

If not present, add these lines.

### 4. ViewBag.CurrentAcademicYear

**Issue**: Many views may reference `ViewBag.CurrentAcademicYear`
**Action**: Update or deprecate

**Search and Replace**:
```bash
# Find all references
grep -r "ViewBag.CurrentAcademicYear" Views/

# Replace with semester-aware version
ViewBag.CurrentSemester = await _semesterService.GetSelectedSemesterAsync();
```

---

## ?? Comparison: Before vs. After

| Feature | Before | After |
|---------|--------|-------|
| **Data Source** | Hardcoded years (2026, 2025, 2024...) | Database-driven semesters |
| **Granularity** | Year only | Semester + Year |
| **Historical View** | ? Not supported | ? Fully supported |
| **Read-Only Mode** | ? Not enforced | ? Auto-enforced |
| **Visual Indicator** | None | ? for current, ?? for historical |
| **User Roles** | Admin only | Admin + All Officers |
| **Session Persistence** | Page-level | Session-level |
| **Database Integration** | ? Disconnected | ? Fully integrated |
| **Dynamic Updates** | Requires code change | Auto-updates from DB |

---

## ?? Success Criteria

### Functional Requirements

1. ? **Admin Layout**
   - Semester dropdown replaces Academic Year dropdown
   - Loads all semesters from database
   - Current semester marked with ?
   - Historical badge appears when selecting past semester
   - Selection persists across pages

2. ? **Student Layout (Officers)**
   - Org Officers see semester dropdown
   - Same functionality as Admin
   - Filters their management pages by semester

3. ? **Student Layout (Regular Students)**
   - Read-only badge shows current semester
   - No dropdown (no selection capability)
   - Display format: "A.Y. 2025-2026 - 1st Semester"

4. ? **Session Management**
   - Selected semester stored in session
   - Persists across page navigation
   - Isolated per user (no cross-contamination)

5. ? **Database Integration**
   - Dropdown populated from Semesters table
   - No hardcoded values
   - Auto-updates when new semester created

### Non-Functional Requirements

1. ? **Performance**
   - Dropdown loads within 500ms
   - Page reload completes within 2 seconds
   - No noticeable lag

2. ? **User Experience**
   - Intuitive interface
   - Clear visual feedback
   - Smooth animations

3. ? **Maintainability**
   - Clean code
   - Well-documented
   - Easy to extend

---

## ?? Troubleshooting Guide

### Issue 1: Dropdown Shows "Loading semesters..." Forever

**Cause**: AJAX call failing
**Debug**:
```javascript
// Add error handling
$.ajax({
    url: '/Admin/GetAllSemestersForDropdown',
    type: 'GET',
    success: function(data) {
        console.log('Semesters loaded:', data);
    },
    error: function(xhr, status, error) {
        console.error('Failed to load semesters:', error);
        console.error('Status:', status);
        console.error('Response:', xhr.responseText);
    }
});
```

**Solutions**:
- Check if endpoint exists in controller
- Verify database has semesters
- Check browser console for errors

### Issue 2: Historical Mode Not Working

**Cause**: Session not configured
**Solution**:
```csharp
// In Program.cs, ensure this exists:
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// And this:
app.UseSession();
```

### Issue 3: Regular Students See Dropdown

**Cause**: Role detection logic incorrect
**Debug**:
```csharp
// Check roles
bool canSwitchSemester = User.IsInRole("Org Treasurer") 
    || User.IsInRole("Class Treasurer") 
    || User.IsInRole("Org Secretary") 
    || User.IsInRole("Class Secretary");

// Log for debugging
Console.WriteLine($"User: {User.Identity.Name}, CanSwitch: {canSwitchSemester}");
```

### Issue 4: Selected Semester Not Persisting

**Cause**: Session cleared or not saved
**Solution**:
```csharp
// Ensure session is being set
HttpContext.Session.SetInt32("ViewingSemesterId", semesterId);

// Verify it's being retrieved
var storedId = HttpContext.Session.GetInt32("ViewingSemesterId");
Console.WriteLine($"Stored Semester ID: {storedId}");
```

---

## ?? Future Enhancements

### Phase 2 Enhancements (Post-Launch)

1. **Keyboard Shortcuts**
   - `Ctrl + ?` : Previous semester
   - `Ctrl + ?` : Next semester
   - `Ctrl + Home` : Current semester

2. **Quick Filters**
   - "Show all semesters from this year"
   - "Compare with previous semester"

3. **Semester Details on Hover**
   ```
   A.Y. 2025-2026 - 1st Semester
   ? (hover)
   Start: Aug 1, 2025
   End: Dec 15, 2025
   Total Students: 542
   Total Events: 28
   ```

4. **Semester Analytics**
   - Show event count per semester
   - Show total fees collected
   - Show attendance rate

5. **Export Historical Data**
   - "Export this semester's data to Excel"
   - "Generate semester report PDF"

---

## ?? Conclusion

This implementation plan provides a **seamless integration** of semester selection into the existing Academic Year dropdown locations. The approach:

? **Minimal Disruption**: Replaces existing dropdown in same location  
? **Role-Aware**: Different behavior for Admin, Officers, and Students  
? **Database-Driven**: No hardcoded values, fully dynamic  
? **Session-Based**: Persistent selection across pages  
? **Backward Compatible**: Existing code continues to work  

**Estimated Effort**: 6 hours  
**Risk Level**: Low  
**User Impact**: High Positive  

---

**Ready to Implement?** 
Review this plan and let me know if you'd like to proceed with the implementation or if you need any modifications!

---

*End of Semester Dropdown Integration Plan*

