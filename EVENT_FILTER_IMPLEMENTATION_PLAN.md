# 🔍 Event Filter Implementation Plan
## iBITS Portal - Event Management Enhancement

---

## 📋 **CURRENT STATE ANALYSIS**

### ✅ What Currently Exists:
- **Events Page:** `Views/Admin/Events.cshtml`
- **Controller Action:** `AdminController.Events(int pageNumber, int pageSize)` 
- **Pagination:** Basic pagination (10 events per page)
- **Event Display:** Card-based grid layout with status badges
- **Event Data Available:**
  - Academic Year (`AcadYear`)
  - Event Type (`EventType`: "iBITS Event", "Non-iBITS Event")
  - Event Date range (Feb 08, 2026 - Feb 17, 2026)
  - Event Name, Location, Description
  - Event Status (Upcoming, Happening Now, Completed)

### ❌ What's Missing:
- **No filtering UI** - Users cannot filter events
- **No search functionality** - Cannot search by event name
- **No date range filtering** - Cannot filter by date period
- **No academic year filter** - Cannot filter by year
- **No event type filter** - Cannot filter by iBITS/Non-iBITS
- **No status filter** - Cannot filter by Upcoming/Completed

---

## 🎯 **IMPLEMENTATION OBJECTIVES**

### Primary Goal:
Add comprehensive filtering to the Events page so admins can:
1. Filter by **Academic Year**
2. Filter by **Semester** (if implemented)
3. Filter by **Event Type** (iBITS Event / Non-iBITS Event)
4. Filter by **Event Status** (Upcoming / Happening Now / Completed)
5. Filter by **Date Range** (From date - To date)
6. **Search** by Event Name or Description
7. **Clear filters** with one click
8. See **filter result count**

---

## 🎨 **UI DESIGN MOCKUP**

### Filter Panel Design:

```
┌────────────────────────────────────────────────────────────────────────────┐
│  📅 Events                                                  [+ Create Event] │
│  Manage organization activities and schedules.                             │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  🔍 FILTERS                                          [Clear All Filters]    │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                                                                       │  │
│  │  🔎 Search Events                                                    │  │
│  │  [_________________________________]  [Search]                       │  │
│  │   Search by event name or description                                │  │
│  │                                                                       │  │
│  │  ─────────────────────────────────────────────────────────────────   │  │
│  │                                                                       │  │
│  │  📅 Academic Year      📚 Semester        📌 Event Type              │  │
│  │  [All Years     ▼]    [All Semesters ▼]  [All Types     ▼]          │  │
│  │                                                                       │  │
│  │  ─────────────────────────────────────────────────────────────────   │  │
│  │                                                                       │  │
│  │  🎯 Status              📆 Date Range                                │  │
│  │  [All Status    ▼]     From: [____/____/____]  To: [____/____/____] │  │
│  │                                                                       │  │
│  │  ─────────────────────────────────────────────────────────────────   │  │
│  │                                                                       │  │
│  │  🔹 Quick Filters:                                                   │  │
│  │     [This Week] [This Month] [Upcoming Only] [Completed Only]        │  │
│  │                                                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  📊 Showing 12 of 16 events                                                │
│                                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                        │
│  │ EVENT CARD  │  │ EVENT CARD  │  │ EVENT CARD  │                        │
│  │   (Filtered)│  │   (Filtered)│  │   (Filtered)│                        │
│  └─────────────┘  └─────────────┘  └─────────────┘                        │
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🛠️ **TECHNICAL IMPLEMENTATION**

### **1. Database Query (Controller Side)**

#### Updated Controller Method Signature:
```csharp
public async Task<IActionResult> Events(
    int pageNumber = 1, 
    int pageSize = 10,
    string? searchString = null,           // NEW
    string? academicYear = null,           // NEW
    string? semester = null,               // NEW
    string? eventType = null,              // NEW
    string? status = null,                 // NEW
    DateOnly? dateFrom = null,             // NEW
    DateOnly? dateTo = null                // NEW
)
```

#### Filtering Logic:
```csharp
var eventsQuery = _context.Events.AsQueryable();

// 1. Search Filter
if (!string.IsNullOrEmpty(searchString))
{
    eventsQuery = eventsQuery.Where(e => 
        e.EventName.Contains(searchString) || 
        (e.EventDesc != null && e.EventDesc.Contains(searchString)) ||
        (e.EventLocation != null && e.EventLocation.Contains(searchString))
    );
}

// 2. Academic Year Filter
if (!string.IsNullOrEmpty(academicYear))
{
    eventsQuery = eventsQuery.Where(e => e.AcadYear == academicYear);
}

// 3. Semester Filter (if column exists)
if (!string.IsNullOrEmpty(semester))
{
    eventsQuery = eventsQuery.Where(e => e.Semester == semester);
}

// 4. Event Type Filter
if (!string.IsNullOrEmpty(eventType))
{
    eventsQuery = eventsQuery.Where(e => e.EventType == eventType);
}

// 5. Status Filter
if (!string.IsNullOrEmpty(status))
{
    var today = DateOnly.FromDateTime(DateTime.Now);
    
    if (status == "Upcoming")
    {
        eventsQuery = eventsQuery.Where(e => 
            e.EventDate.HasValue && e.EventDate.Value > today
        );
    }
    else if (status == "Today")
    {
        eventsQuery = eventsQuery.Where(e => 
            e.EventDate.HasValue && e.EventDate.Value == today
        );
    }
    else if (status == "Completed")
    {
        eventsQuery = eventsQuery.Where(e => 
            e.IsClosed || 
            (e.EventDate.HasValue && e.EventDate.Value < today)
        );
    }
}

// 6. Date Range Filter
if (dateFrom.HasValue)
{
    eventsQuery = eventsQuery.Where(e => 
        e.EventDate.HasValue && e.EventDate.Value >= dateFrom.Value
    );
}

if (dateTo.HasValue)
{
    eventsQuery = eventsQuery.Where(e => 
        e.EventDate.HasValue && e.EventDate.Value <= dateTo.Value
    );
}

// Get total count BEFORE pagination
var totalEvents = await eventsQuery.CountAsync();

// Apply pagination
var events = await eventsQuery
    .OrderByDescending(e => e.EventDate)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// Pass filter values to view
ViewBag.CurrentSearch = searchString;
ViewBag.CurrentAcademicYear = academicYear;
ViewBag.CurrentSemester = semester;
ViewBag.CurrentEventType = eventType;
ViewBag.CurrentStatus = status;
ViewBag.CurrentDateFrom = dateFrom;
ViewBag.CurrentDateTo = dateTo;
ViewBag.TotalEvents = totalEvents;
ViewBag.FilteredCount = events.Count;
```

---

### **2. View (UI Side)**

#### Filter Panel HTML:
```html
<!-- FILTER PANEL -->
<div class="glass-panel mb-4 p-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="mb-0"><i class="bi bi-funnel me-2"></i> Filters</h5>
        <a href="@Url.Action("Events")" class="btn btn-sm btn-outline-gold">
            <i class="bi bi-x-circle me-1"></i> Clear All
        </a>
    </div>

    <form method="get" action="@Url.Action("Events")" id="filterForm">
        <!-- Search Bar -->
        <div class="row g-3 mb-3">
            <div class="col-12">
                <div class="input-group">
                    <span class="input-group-text bg-dark border-gold">
                        <i class="bi bi-search"></i>
                    </span>
                    <input type="text" 
                           name="searchString" 
                           class="form-control" 
                           placeholder="Search by event name or description..." 
                           value="@ViewBag.CurrentSearch">
                </div>
            </div>
        </div>

        <!-- Filter Dropdowns Row 1 -->
        <div class="row g-3 mb-3">
            <!-- Academic Year -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-calendar-range me-1"></i> Academic Year
                </label>
                <select name="academicYear" class="form-select">
                    <option value="">All Years</option>
                    @if (ViewBag.AcademicYearOptions != null)
                    {
                        foreach (var year in ViewBag.AcademicYearOptions)
                        {
                            <option value="@year" selected="@(ViewBag.CurrentAcademicYear == year)">
                                @year
                            </option>
                        }
                    }
                </select>
            </div>

            <!-- Semester (if applicable) -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-bookmark me-1"></i> Semester
                </label>
                <select name="semester" class="form-select">
                    <option value="">All Semesters</option>
                    <option value="1st Semester" selected="@(ViewBag.CurrentSemester == "1st Semester")">
                        1st Semester
                    </option>
                    <option value="2nd Semester" selected="@(ViewBag.CurrentSemester == "2nd Semester")">
                        2nd Semester
                    </option>
                    <option value="Summer" selected="@(ViewBag.CurrentSemester == "Summer")">
                        Summer
                    </option>
                </select>
            </div>

            <!-- Event Type -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-tag me-1"></i> Event Type
                </label>
                <select name="eventType" class="form-select">
                    <option value="">All Types</option>
                    <option value="iBITS Event" selected="@(ViewBag.CurrentEventType == "iBITS Event")">
                        iBITS Event
                    </option>
                    <option value="Non-iBITS Event" selected="@(ViewBag.CurrentEventType == "Non-iBITS Event")">
                        Non-iBITS Event
                    </option>
                </select>
            </div>
        </div>

        <!-- Filter Dropdowns Row 2 -->
        <div class="row g-3 mb-3">
            <!-- Status -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-flag me-1"></i> Status
                </label>
                <select name="status" class="form-select">
                    <option value="">All Status</option>
                    <option value="Upcoming" selected="@(ViewBag.CurrentStatus == "Upcoming")">
                        Upcoming
                    </option>
                    <option value="Today" selected="@(ViewBag.CurrentStatus == "Today")">
                        Happening Today
                    </option>
                    <option value="Completed" selected="@(ViewBag.CurrentStatus == "Completed")">
                        Completed
                    </option>
                </select>
            </div>

            <!-- Date From -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-calendar-check me-1"></i> Date From
                </label>
                <input type="date" 
                       name="dateFrom" 
                       class="form-control" 
                       value="@ViewBag.CurrentDateFrom?.ToString("yyyy-MM-dd")">
            </div>

            <!-- Date To -->
            <div class="col-md-4">
                <label class="form-label small text-muted">
                    <i class="bi bi-calendar-x me-1"></i> Date To
                </label>
                <input type="date" 
                       name="dateTo" 
                       class="form-control" 
                       value="@ViewBag.CurrentDateTo?.ToString("yyyy-MM-dd")">
            </div>
        </div>

        <!-- Quick Filters -->
        <div class="d-flex gap-2 flex-wrap mb-3">
            <span class="small text-muted me-2">Quick Filters:</span>
            <button type="button" class="btn btn-sm btn-outline-gold quick-filter" data-filter="thisWeek">
                This Week
            </button>
            <button type="button" class="btn btn-sm btn-outline-gold quick-filter" data-filter="thisMonth">
                This Month
            </button>
            <button type="button" class="btn btn-sm btn-outline-gold quick-filter" data-filter="upcoming">
                Upcoming Only
            </button>
            <button type="button" class="btn btn-sm btn-outline-gold quick-filter" data-filter="completed">
                Completed Only
            </button>
        </div>

        <!-- Apply Button -->
        <div class="text-end">
            <button type="submit" class="btn btn-gold">
                <i class="bi bi-funnel-fill me-1"></i> Apply Filters
            </button>
        </div>
    </form>
</div>

<!-- Filter Results Summary -->
@if (ViewBag.TotalEvents != null)
{
    <div class="glass-panel p-3 mb-4">
        <div class="d-flex justify-content-between align-items-center">
            <span>
                <i class="bi bi-info-circle me-2"></i>
                Showing <strong>@ViewBag.FilteredCount</strong> of <strong>@ViewBag.TotalEvents</strong> events
            </span>
            @if (!string.IsNullOrEmpty(ViewBag.CurrentSearch as string) || 
                 !string.IsNullOrEmpty(ViewBag.CurrentAcademicYear as string) ||
                 !string.IsNullOrEmpty(ViewBag.CurrentEventType as string) ||
                 !string.IsNullOrEmpty(ViewBag.CurrentStatus as string))
            {
                <span class="badge bg-gold">
                    <i class="bi bi-check-circle me-1"></i> Filters Active
                </span>
            }
        </div>
    </div>
}
```

---

### **3. JavaScript (Interaction)**

```javascript
<script>
    $(document).ready(function() {
        // Quick Filter Buttons
        $('.quick-filter').click(function() {
            var filter = $(this).data('filter');
            var today = new Date();
            
            if (filter === 'thisWeek') {
                // Set date range for this week
                var startOfWeek = new Date(today.setDate(today.getDate() - today.getDay()));
                var endOfWeek = new Date(today.setDate(today.getDate() - today.getDay() + 6));
                $('input[name="dateFrom"]').val(formatDate(startOfWeek));
                $('input[name="dateTo"]').val(formatDate(endOfWeek));
            } 
            else if (filter === 'thisMonth') {
                // Set date range for this month
                var startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
                var endOfMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0);
                $('input[name="dateFrom"]').val(formatDate(startOfMonth));
                $('input[name="dateTo"]').val(formatDate(endOfMonth));
            }
            else if (filter === 'upcoming') {
                $('select[name="status"]').val('Upcoming');
            }
            else if (filter === 'completed') {
                $('select[name="status"]').val('Completed');
            }
            
            // Submit form
            $('#filterForm').submit();
        });
        
        // Auto-submit on dropdown change (optional)
        $('.form-select').change(function() {
            // Uncomment to enable auto-submit
            // $('#filterForm').submit();
        });
        
        // Helper function to format date
        function formatDate(date) {
            var year = date.getFullYear();
            var month = String(date.getMonth() + 1).padStart(2, '0');
            var day = String(date.getDate()).padStart(2, '0');
            return year + '-' + month + '-' + day;
        }
    });
</script>
```

---

## 📊 **FILTER OPTIONS DATA**

### Populate Filter Dropdowns in Controller:
```csharp
// Populate Academic Year Options
ViewBag.AcademicYearOptions = await _context.Events
    .Where(e => e.AcadYear != null)
    .Select(e => e.AcadYear)
    .Distinct()
    .OrderBy(y => y)
    .ToListAsync();

// Populate Semester Options (if column exists)
ViewBag.SemesterOptions = new List<string> 
{ 
    "1st Semester", 
    "2nd Semester", 
    "Summer" 
};

// Populate Event Type Options
ViewBag.EventTypeOptions = await _context.Events
    .Where(e => e.EventType != null)
    .Select(e => e.EventType)
    .Distinct()
    .OrderBy(t => t)
    .ToListAsync();
```

---

## 🎯 **IMPLEMENTATION CHECKLIST**

### Phase 1: Controller Updates
- [ ] Update `Events()` method to accept filter parameters
- [ ] Add filtering logic for each parameter
- [ ] Populate ViewBag with current filter values
- [ ] Populate ViewBag with filter dropdown options
- [ ] Add total count and filtered count to ViewBag
- [ ] Test filtering logic with sample data

### Phase 2: View Updates
- [ ] Add filter panel above event grid
- [ ] Add search input field
- [ ] Add Academic Year dropdown
- [ ] Add Semester dropdown (optional for now)
- [ ] Add Event Type dropdown
- [ ] Add Status dropdown
- [ ] Add Date From/To inputs
- [ ] Add Quick Filter buttons
- [ ] Add "Clear All Filters" button
- [ ] Add filter results summary
- [ ] Update pagination to maintain filters

### Phase 3: JavaScript Enhancement
- [ ] Implement quick filter button handlers
- [ ] Add date range calculation for "This Week" and "This Month"
- [ ] Optional: Add auto-submit on dropdown change
- [ ] Add smooth scrolling to results
- [ ] Add loading indicator during filter

### Phase 4: Styling
- [ ] Style filter panel with glass-panel design
- [ ] Style form inputs to match theme
- [ ] Add responsive design for mobile
- [ ] Add icons to filter labels
- [ ] Add active filter badge

### Phase 5: Testing
- [ ] Test each filter individually
- [ ] Test combined filters
- [ ] Test pagination with filters
- [ ] Test "Clear All" functionality
- [ ] Test quick filter buttons
- [ ] Test with empty results
- [ ] Test with large datasets

---

## 🎨 **CSS STYLING**

```css
/* Filter Panel Styling */
.filter-panel {
    background: var(--glass-bg);
    backdrop-filter: blur(10px);
    border: 1px solid var(--glass-border);
    border-radius: 12px;
    padding: 1.5rem;
}

.quick-filter {
    transition: all 0.3s ease;
}

.quick-filter:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 8px rgba(255, 215, 0, 0.3);
}

.form-select, .form-control {
    background: rgba(0, 0, 0, 0.3);
    border: 1px solid var(--gold-accent);
    color: var(--text-strong);
}

.form-select:focus, .form-control:focus {
    border-color: var(--gold-accent);
    box-shadow: 0 0 0 0.2rem rgba(255, 215, 0, 0.25);
}
```

---

## 📈 **EXPECTED BENEFITS**

### For Users:
✅ **Faster Event Discovery** - Find events quickly with filters  
✅ **Better Organization** - View events by academic period  
✅ **Improved Planning** - Filter upcoming vs completed events  
✅ **Enhanced Productivity** - Less scrolling, more filtering  

### For System:
✅ **Better Performance** - Filtered queries load faster  
✅ **Reduced Load** - Less data transferred per page  
✅ **Better UX** - Professional, modern filtering interface  
✅ **Scalability** - System handles large datasets better  

---

## 🚀 **DEPLOYMENT STEPS**

1. **Backup Current Code** - Create backup of AdminController.cs and Events.cshtml
2. **Update Controller** - Add filtering parameters and logic
3. **Update View** - Add filter panel UI
4. **Test Locally** - Verify all filters work correctly
5. **Deploy to Production** - Push changes to live server
6. **User Training** - Brief demo for admins on new filters

---

## 📝 **FUTURE ENHANCEMENTS**

- Export filtered events to Excel/CSV
- Save filter presets (favorites)
- Bulk operations on filtered events
- Advanced filters (by location, by fine amount, etc.)
- Filter by attendance rate
- Filter by number of participants

---

**Created:** February 16, 2026  
**Status:** Planning Phase  
**Priority:** High  
**Estimated Time:** 4-6 hours

