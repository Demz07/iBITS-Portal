# Event Filter Implementation Plan (Without Semester)
## iBITS Portal - Event Management Enhancement

---

## 📊 Current Database Analysis

### Event Table Structure (Available Fields)
```
✅ EventId (int) - Primary Key
✅ EventName (nvarchar) - Event title
✅ EventLocation (nvarchar) - Event venue
✅ EventDate (date) - Event start date
✅ EndDate (date) - Event end date
✅ StartTime (time) - Event start time
✅ EndTime (time) - Event end time
✅ EventDuration (nvarchar) - Duration description
✅ AcadYear (nvarchar) - Academic Year (e.g., "2025-2026")
✅ EventDesc (nvarchar) - Event description
✅ EventType (nvarchar) - "iBITS Event" or "Non-iBITS Event"
✅ IsClosed (bit) - Event closure status
✅ Fine amounts for different classifications
```

### Current Data Insights
- **Event Types**: iBITS Event, Non-iBITS Event
- **Academic Years**: 2025-2026 (expandable)
- **Date Range**: 2026-02-08 to 2026-02-17
- **Total Events**: 16 events

---

## 🎯 Implementation Plan Overview

### Filter Capabilities (WITHOUT Semester)
1. **🔍 Search Bar** - Search by event name or description
2. **📅 Academic Year Filter** - Filter by academic year
3. **🏷️ Event Type Filter** - iBITS Event vs Non-iBITS Event
4. **📊 Status Filter** - Upcoming, Today, Past, All
5. **📆 Date Range Filter** - Custom date selection (From/To)
6. **🔒 Closure Status** - Open Events vs Closed Events
7. **⚡ Quick Filters** - Preset filters (This Week, This Month, etc.)

---

## 📐 Implementation Steps

### **Phase 1: Create ViewModel**
**File**: `iBITS Portal/ViewModels/EventFilterViewModel.cs`

```csharp
public class EventFilterViewModel
{
    // Search
    public string? SearchTerm { get; set; }
    
    // Filters
    public string? AcademicYear { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; } // "upcoming", "today", "past", "all"
    public bool? IsClosedFilter { get; set; } // null = all, true = closed, false = open
    
    // Date Range
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    
    // Quick Filters
    public string? QuickFilter { get; set; } // "thisweek", "thismonth", "upcoming", etc.
    
    // Results
    public List<Event> Events { get; set; } = new List<Event>();
    public int TotalCount { get; set; }
    
    // Available Options (for dropdowns)
    public List<string> AvailableAcademicYears { get; set; } = new List<string>();
    public List<string> AvailableEventTypes { get; set; } = new List<string>();
}
```

---

### **Phase 2: Update Controller**
**File**: `iBITS Portal/Controllers/AdminController.cs`

**Update the Events action:**

```csharp
public async Task<IActionResult> Events(EventFilterViewModel filter)
{
    var query = _context.Events.AsQueryable();
    
    // 1. Search Filter
    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
    {
        var searchLower = filter.SearchTerm.ToLower();
        query = query.Where(e => 
            e.EventName.ToLower().Contains(searchLower) ||
            (e.EventDesc != null && e.EventDesc.ToLower().Contains(searchLower)) ||
            (e.EventLocation != null && e.EventLocation.ToLower().Contains(searchLower))
        );
    }
    
    // 2. Academic Year Filter
    if (!string.IsNullOrWhiteSpace(filter.AcademicYear))
    {
        query = query.Where(e => e.AcadYear == filter.AcademicYear);
    }
    
    // 3. Event Type Filter
    if (!string.IsNullOrWhiteSpace(filter.EventType))
    {
        query = query.Where(e => e.EventType == filter.EventType);
    }
    
    // 4. Status Filter (Upcoming/Today/Past)
    var today = DateOnly.FromDateTime(DateTime.Today);
    
    if (!string.IsNullOrWhiteSpace(filter.Status))
    {
        query = filter.Status.ToLower() switch
        {
            "upcoming" => query.Where(e => e.EventDate > today),
            "today" => query.Where(e => e.EventDate == today),
            "past" => query.Where(e => e.EventDate < today),
            _ => query
        };
    }
    
    // 5. Closure Status Filter
    if (filter.IsClosedFilter.HasValue)
    {
        query = query.Where(e => e.IsClosed == filter.IsClosedFilter.Value);
    }
    
    // 6. Date Range Filter
    if (filter.DateFrom.HasValue)
    {
        var dateFrom = DateOnly.FromDateTime(filter.DateFrom.Value);
        query = query.Where(e => e.EventDate >= dateFrom);
    }
    
    if (filter.DateTo.HasValue)
    {
        var dateTo = DateOnly.FromDateTime(filter.DateTo.Value);
        query = query.Where(e => e.EventDate <= dateTo);
    }
    
    // 7. Quick Filters
    if (!string.IsNullOrWhiteSpace(filter.QuickFilter))
    {
        var now = DateTime.Now;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        
        query = filter.QuickFilter.ToLower() switch
        {
            "thisweek" => query.Where(e => 
                e.EventDate >= DateOnly.FromDateTime(startOfWeek) &&
                e.EventDate <= DateOnly.FromDateTime(endOfWeek)
            ),
            "thismonth" => query.Where(e => 
                e.EventDate >= DateOnly.FromDateTime(startOfMonth) &&
                e.EventDate <= DateOnly.FromDateTime(endOfMonth)
            ),
            "upcoming" => query.Where(e => e.EventDate >= today),
            "past" => query.Where(e => e.EventDate < today),
            "open" => query.Where(e => e.IsClosed == false),
            "closed" => query.Where(e => e.IsClosed == true),
            _ => query
        };
    }
    
    // Get filtered events
    filter.Events = await query
        .OrderByDescending(e => e.EventDate)
        .ThenBy(e => e.StartTime)
        .ToListAsync();
    
    filter.TotalCount = filter.Events.Count;
    
    // Populate dropdown options
    filter.AvailableAcademicYears = await _context.Events
        .Where(e => e.AcadYear != null)
        .Select(e => e.AcadYear)
        .Distinct()
        .OrderByDescending(y => y)
        .ToListAsync();
    
    filter.AvailableEventTypes = await _context.Events
        .Where(e => e.EventType != null)
        .Select(e => e.EventType)
        .Distinct()
        .ToListAsync();
    
    return View(filter);
}
```

---

### **Phase 3: Update View UI**
**File**: `iBITS Portal/Views/Admin/Events.cshtml`

**Add Filter Section (before the events table):**

```html
@model EventFilterViewModel

<!-- Filter Section -->
<div class="card mb-4 shadow-sm">
    <div class="card-header bg-primary text-white">
        <h5 class="mb-0">
            <i class="bi bi-funnel"></i> Filter Events
        </h5>
    </div>
    <div class="card-body">
        <form method="get" asp-action="Events" id="filterForm">
            <div class="row g-3">
                
                <!-- Search Bar -->
                <div class="col-md-4">
                    <label class="form-label">
                        <i class="bi bi-search"></i> Search
                    </label>
                    <input type="text" 
                           class="form-control" 
                           name="SearchTerm" 
                           value="@Model.SearchTerm"
                           placeholder="Search by name, description, location..." />
                </div>
                
                <!-- Academic Year -->
                <div class="col-md-2">
                    <label class="form-label">
                        <i class="bi bi-calendar-range"></i> Academic Year
                    </label>
                    <select class="form-select" name="AcademicYear">
                        <option value="">All Years</option>
                        @foreach (var year in Model.AvailableAcademicYears)
                        {
                            <option value="@year" selected="@(year == Model.AcademicYear)">
                                @year
                            </option>
                        }
                    </select>
                </div>
                
                <!-- Event Type -->
                <div class="col-md-2">
                    <label class="form-label">
                        <i class="bi bi-tag"></i> Event Type
                    </label>
                    <select class="form-select" name="EventType">
                        <option value="">All Types</option>
                        @foreach (var type in Model.AvailableEventTypes)
                        {
                            <option value="@type" selected="@(type == Model.EventType)">
                                @type
                            </option>
                        }
                    </select>
                </div>
                
                <!-- Status -->
                <div class="col-md-2">
                    <label class="form-label">
                        <i class="bi bi-clock"></i> Status
                    </label>
                    <select class="form-select" name="Status">
                        <option value="">All Status</option>
                        <option value="upcoming" selected="@(Model.Status == "upcoming")">Upcoming</option>
                        <option value="today" selected="@(Model.Status == "today")">Today</option>
                        <option value="past" selected="@(Model.Status == "past")">Past</option>
                    </select>
                </div>
                
                <!-- Closure Status -->
                <div class="col-md-2">
                    <label class="form-label">
                        <i class="bi bi-lock"></i> Closure
                    </label>
                    <select class="form-select" name="IsClosedFilter">
                        <option value="">All Events</option>
                        <option value="false" selected="@(Model.IsClosedFilter == false)">Open</option>
                        <option value="true" selected="@(Model.IsClosedFilter == true)">Closed</option>
                    </select>
                </div>
                
            </div>
            
            <!-- Date Range Row -->
            <div class="row g-3 mt-2">
                <div class="col-md-3">
                    <label class="form-label">
                        <i class="bi bi-calendar-check"></i> Date From
                    </label>
                    <input type="date" 
                           class="form-control" 
                           name="DateFrom" 
                           value="@Model.DateFrom?.ToString("yyyy-MM-dd")" />
                </div>
                
                <div class="col-md-3">
                    <label class="form-label">
                        <i class="bi bi-calendar-x"></i> Date To
                    </label>
                    <input type="date" 
                           class="form-control" 
                           name="DateTo" 
                           value="@Model.DateTo?.ToString("yyyy-MM-dd")" />
                </div>
                
                <div class="col-md-6 d-flex align-items-end gap-2">
                    <button type="submit" class="btn btn-primary">
                        <i class="bi bi-search"></i> Apply Filters
                    </button>
                    <a href="@Url.Action("Events")" class="btn btn-secondary">
                        <i class="bi bi-x-circle"></i> Clear All
                    </a>
                </div>
            </div>
            
            <!-- Quick Filters -->
            <div class="row mt-3">
                <div class="col-12">
                    <label class="form-label fw-bold">Quick Filters:</label>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-primary quick-filter" data-filter="thisweek">
                            This Week
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-primary quick-filter" data-filter="thismonth">
                            This Month
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-success quick-filter" data-filter="upcoming">
                            Upcoming Only
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-warning quick-filter" data-filter="past">
                            Past Events
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-info quick-filter" data-filter="open">
                            Open Events
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger quick-filter" data-filter="closed">
                            Closed Events
                        </button>
                    </div>
                </div>
            </div>
            
            <!-- Hidden field for quick filter -->
            <input type="hidden" name="QuickFilter" id="quickFilterInput" value="@Model.QuickFilter" />
        </form>
    </div>
</div>

<!-- Results Summary -->
<div class="alert alert-info">
    <i class="bi bi-info-circle"></i> 
    Showing <strong>@Model.TotalCount</strong> event(s)
    @if (!string.IsNullOrEmpty(Model.SearchTerm) || 
         !string.IsNullOrEmpty(Model.AcademicYear) || 
         !string.IsNullOrEmpty(Model.EventType) ||
         !string.IsNullOrEmpty(Model.Status) ||
         Model.IsClosedFilter.HasValue ||
         Model.DateFrom.HasValue ||
         Model.DateTo.HasValue)
    {
        <span class="text-muted">(filtered)</span>
    }
</div>

<!-- Events Table (your existing table) -->
<div class="card">
    <div class="card-body">
        <table class="table table-hover">
            <thead>
                <tr>
                    <th>Event Name</th>
                    <th>Location</th>
                    <th>Date</th>
                    <th>Time</th>
                    <th>Academic Year</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var evt in Model.Events)
                {
                    <tr>
                        <td>@evt.EventName</td>
                        <td>@evt.EventLocation</td>
                        <td>@evt.EventDate?.ToString("MMM dd, yyyy")</td>
                        <td>@evt.StartTime?.ToString("hh:mm tt") - @evt.EndTime?.ToString("hh:mm tt")</td>
                        <td>@evt.AcadYear</td>
                        <td>
                            @if (evt.EventType == "iBITS Event")
                            {
                                <span class="badge bg-primary">@evt.EventType</span>
                            }
                            else
                            {
                                <span class="badge bg-secondary">@evt.EventType</span>
                            }
                        </td>
                        <td>
                            @if (evt.IsClosed)
                            {
                                <span class="badge bg-danger">Closed</span>
                            }
                            else
                            {
                                @if (evt.EventDate < DateOnly.FromDateTime(DateTime.Today))
                                {
                                    <span class="badge bg-secondary">Past</span>
                                }
                                else if (evt.EventDate == DateOnly.FromDateTime(DateTime.Today))
                                {
                                    <span class="badge bg-warning">Today</span>
                                }
                                else
                                {
                                    <span class="badge bg-success">Upcoming</span>
                                }
                            }
                        </td>
                        <td>
                            <!-- Your existing action buttons -->
                        </td>
                    </tr>
                }
            </tbody>
        </table>
        
        @if (!Model.Events.Any())
        {
            <div class="alert alert-warning text-center">
                <i class="bi bi-exclamation-triangle"></i>
                No events found matching your filters.
            </div>
        }
    </div>
</div>
```

---

### **Phase 4: Add JavaScript**
**Add to bottom of Events.cshtml:**

```html
@section Scripts {
    <script>
        $(document).ready(function() {
            // Quick Filter Buttons
            $('.quick-filter').click(function() {
                var filterValue = $(this).data('filter');
                $('#quickFilterInput').val(filterValue);
                $('#filterForm').submit();
            });
            
            // Highlight active quick filter
            var currentQuickFilter = '@Model.QuickFilter';
            if (currentQuickFilter) {
                $('.quick-filter[data-filter="' + currentQuickFilter + '"]').addClass('active');
            }
            
            // Auto-submit on select change (optional - for better UX)
            $('.form-select').change(function() {
                // Uncomment below to enable auto-submit on dropdown change
                // $('#filterForm').submit();
            });
        });
    </script>
}
```

---

## 📊 UI Design Features

### Visual Enhancements
- ✅ **Card-based filter panel** with primary color header
- ✅ **Icon-enhanced labels** for better visual recognition
- ✅ **Quick filter buttons** with color-coded badges
- ✅ **Results counter** showing filtered vs total
- ✅ **Status badges** (Upcoming/Today/Past/Closed)
- ✅ **Event type badges** (iBITS vs Non-iBITS)
- ✅ **Responsive grid layout** (mobile-friendly)

### User Experience
- ✅ **Clear All button** to reset filters instantly
- ✅ **Active filter indication** on quick buttons
- ✅ **Empty state message** when no results found
- ✅ **Form persistence** - filters stay after submission
- ✅ **Optional auto-submit** on dropdown change

---

## 🚀 Implementation Timeline

| Phase | Task | Estimated Time |
|-------|------|----------------|
| 1 | Create EventFilterViewModel | 15 minutes |
| 2 | Update AdminController | 30 minutes |
| 3 | Update Events.cshtml UI | 45 minutes |
| 4 | Add JavaScript functionality | 15 minutes |
| 5 | Testing all filters | 30 minutes |
| **Total** | | **~2.5 hours** |

---

## ✅ Testing Checklist

After implementation, test:

- [ ] Search by event name works
- [ ] Search by description works
- [ ] Search by location works
- [ ] Academic Year filter works
- [ ] Event Type filter works
- [ ] Status filter (Upcoming/Today/Past) works
- [ ] Closure status filter works
- [ ] Date From filter works
- [ ] Date To filter works
- [ ] Date range combination works
- [ ] Quick filter: This Week works
- [ ] Quick filter: This Month works
- [ ] Quick filter: Upcoming Only works
- [ ] Quick filter: Past Events works
- [ ] Quick filter: Open Events works
- [ ] Quick filter: Closed Events works
- [ ] Clear All button resets filters
- [ ] Multiple filters work together
- [ ] Results counter is accurate
- [ ] Status badges display correctly
- [ ] Empty state shows when no results
- [ ] Filters persist after page reload

---

## 📝 Notes

### Benefits of This Approach
✅ **No database changes required** - uses existing Event table structure
✅ **Backward compatible** - doesn't break existing functionality
✅ **Flexible filtering** - supports multiple filter combinations
✅ **User-friendly** - quick filters for common scenarios
✅ **Performant** - uses IQueryable for efficient database queries
✅ **Maintainable** - clean separation of concerns with ViewModel

### Future Enhancements
- Add pagination for large event lists
- Add export filtered results to CSV/PDF
- Add saved filter presets
- Add sort options (by date, name, type)
- Add event count by type chart
- Add calendar view of filtered events

---

## 🎯 Ready to Implement!

This plan provides a complete, production-ready event filtering system without requiring semester functionality or any database schema changes.

**All filters work with your current database structure!**

Would you like to proceed with implementation?
