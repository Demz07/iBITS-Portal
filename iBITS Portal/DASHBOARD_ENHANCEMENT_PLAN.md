# Org Treasurer Dashboard Enhancement Plan (REVISED)
**Date:** 2026-02-11  
**Version:** 2.0 (Revised with enhanced filtering)
**Status:** Planning Phase - Awaiting Approval  
**Requested By:** User

---

## 📋 Requirements Summary

### Requirement 1: Collection Trends Chart Filters
- **Current State:** Shows all months from Aug (academic year start) to present
- **Issue:** Too many data points, lines may be hard to see
- **Requested:** Add filter to display specific date ranges (by month or by year)

### Requirement 2: Export Functionality Enhancement
- **Current State:** Export button exists in "Paid/Unpaid by Program-Year" section (line 256-257)
- **Issue:** Export is section-specific, not dashboard-wide
- **Requested:** 
  - Move export button to main dashboard header
  - Export should capture charts as images
  - Export entire dashboard data

---

## 🎯 Proposed Solution

### FEATURE 1: Collection Trends - Date Range Filter

#### **Option A: Dropdown Filter (RECOMMENDED)**
**UI Location:** Inside the "Collection Trends" panel header

**Filter Options:**
```
┌─────────────────────────────────────────────────────┐
│ Collection Trends (Current Academic Year)          │
│ ┌───────────────────┐  ┌──────────────────┐       │
│ │ View: Last 3 Months▼│  │ Year: 2025-2026 ▼│       │
│ └───────────────────┘  └──────────────────┘       │
└─────────────────────────────────────────────────────┘
```

**Dropdown 1: Time Range**
- Last Month
- Last 3 Months ✓ (Default)
- Last 6 Months
- Current Semester (Aug-Jan or Feb-Jul)
- Full Academic Year (Aug-Jul)
- Custom Range (shows date pickers)

**Dropdown 2: Academic Year**
- 2025-2026 (Current) ✓
- 2024-2025
- 2023-2024
- (Load dynamically from database)

**Behavior:**
1. User selects time range and year
2. JavaScript filters the existing data OR makes AJAX call to fetch new data
3. Chart updates dynamically without page reload
4. Month labels on X-axis update accordingly

**Technical Implementation:**
- **Backend:** Add new action `GetMonthlyTrends(string timeRange, string academicYear)`
- **Frontend:** AJAX call + Chart.js `chart.update()` method
- **Data:** Filter based on `CollectionDate` field

---

#### **Option B: Date Range Picker (Alternative)**
**UI Location:** Inside the "Collection Trends" panel header

```
┌─────────────────────────────────────────────────────┐
│ Collection Trends                                    │
│ From: [Aug 2025 ▼]  To: [Feb 2026 ▼]  [Apply Filter]│
└─────────────────────────────────────────────────────┘
```

**Pros:** More flexible, user can select exact months
**Cons:** More clicks required, less user-friendly

**RECOMMENDATION:** Use Option A for better UX

---

### FEATURE 2: Dashboard Export Functionality

#### **Export Button Placement**
**Location:** Main dashboard header (top of page, next to title)

**Before:**
```html
<h4>Organization Treasury Dashboard</h4>
<p>Overview of all collections...</p>
```

**After:**
```html
<div class="d-flex justify-content-between align-items-center">
    <div>
        <h4>Organization Treasury Dashboard</h4>
        <p>Overview of all collections...</p>
    </div>
    <div class="dropdown">
        <button class="btn btn-outline-gold dropdown-toggle" data-bs-toggle="dropdown">
            <i class="bi bi-download me-2"></i> Export Dashboard
        </button>
        <ul class="dropdown-menu">
            <li><a class="dropdown-item" onclick="exportAsImages()">
                <i class="bi bi-image"></i> Export Charts as Images (PNG)
            </a></li>
            <li><a class="dropdown-item" onclick="exportAsPDF()">
                <i class="bi bi-file-pdf"></i> Export as PDF Report
            </a></li>
            <li><a class="dropdown-item" onclick="exportAsExcel()">
                <i class="bi bi-file-excel"></i> Export Data as Excel
            </a></li>
        </ul>
    </div>
</div>
```

---

#### **Export Options**

##### **Option 1: Export Charts as Images (PNG)**
**What it does:**
- Captures Collection Trends chart as PNG
- Captures Paid/Unpaid by Program-Year section as PNG
- Downloads as ZIP file: `OrgTreasury_Charts_2026-02-11.zip`
  - `collection_trends.png`
  - `program_year_stats.png`

**Technology:**
- **Library:** `html2canvas` (already popular, easy to use)
- **Implementation:**
  ```javascript
  function exportAsImages() {
      // Capture Collection Trends chart
      html2canvas(document.getElementById('trendsChart')).then(canvas => {
          downloadImage(canvas, 'collection_trends.png');
      });
      
      // Capture Program-Year section
      html2canvas(document.getElementById('programYearSection')).then(canvas => {
          downloadImage(canvas, 'program_year_stats.png');
      });
  }
  ```

---

##### **Option 2: Export as PDF Report**
**What it does:**
- Generates a professional PDF report
- Includes:
  - Dashboard header with date/time
  - Quick Stats table
  - Collection Trends chart (as image)
  - Program-Year breakdown (as table + chart)
  - Footer with generated timestamp

**Technology:**
- **Library:** `jsPDF` + `html2canvas`
- **Layout:** Portrait A4 with iBITS branding

**Sample Structure:**
```
┌────────────────────────────────────────┐
│ iBITS Organization Treasury Dashboard  │
│ Report Generated: Feb 11, 2026         │
├────────────────────────────────────────┤
│ Quick Stats                            │
│ • Total Collected: ₱XX,XXX.XX          │
│ • Pending: ₱XX,XXX.XX                  │
├────────────────────────────────────────┤
│ [Collection Trends Chart Image]        │
├────────────────────────────────────────┤
│ Program-Year Breakdown                 │
│ [Table with data]                      │
└────────────────────────────────────────┘
```

---

##### **Option 3: Export Data as Excel**
**What it does:**
- Exports raw data to Excel (.xlsx)
- Multiple sheets:
  - **Sheet 1:** Quick Stats summary
  - **Sheet 2:** Monthly Trends (Month, Fees, Fines columns)
  - **Sheet 3:** Program-Year Breakdown (Program, Total, Paid, Unpaid)

**Technology:**
- **Library:** `xlsx` (SheetJS)
- **File:** `OrgTreasury_Data_2026-02-11.xlsx`

---

#### **Remove Export from Program-Year Section**
**Current:** Line 256-257 has export button  
**Action:** Delete this button, functionality moved to main header

---

## 🎨 UI/UX Design Plan

### Collection Trends Panel (Enhanced)
```html
<div class="glass-panel">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="glass-header-title">
            <i class="bi bi-graph-up me-2 text-gold"></i> 
            Collection Trends (Current Academic Year)
        </h5>
        <div class="d-flex gap-2">
            <!-- Filter Controls -->
            <select id="timeRangeFilter" class="form-select form-select-sm" style="width: 150px;">
                <option value="1">Last Month</option>
                <option value="3" selected>Last 3 Months</option>
                <option value="6">Last 6 Months</option>
                <option value="semester">Current Semester</option>
                <option value="all">Full Academic Year</option>
            </select>
            <select id="yearFilter" class="form-select form-select-sm" style="width: 140px;">
                <option value="2025-2026" selected>2025-2026</option>
                <option value="2024-2025">2024-2025</option>
            </select>
            <button class="btn btn-sm btn-outline-gold" onclick="applyTrendsFilter()">
                <i class="bi bi-funnel"></i> Apply
            </button>
        </div>
    </div>
    <div style="position: relative; height: 350px;">
        <canvas id="trendsChart"></canvas>
    </div>
</div>
```

### Dashboard Header (With Export)
```html
<div class="glass-panel d-flex justify-content-between align-items-center mb-4">
    <div>
        <h4 class="glass-header-title">
            <i class="bi bi-bank me-2 text-gold"></i> 
            Organization Treasury Dashboard
        </h4>
        <p class="mb-0 small text-muted">
            Overview of all fee and fine collections across programs
        </p>
    </div>
    <div class="dropdown">
        <button class="btn btn-outline-gold dropdown-toggle" 
                data-bs-toggle="dropdown" aria-expanded="false">
            <i class="bi bi-download me-2"></i> Export Dashboard
        </button>
        <ul class="dropdown-menu dropdown-menu-end">
            <li>
                <a class="dropdown-item" href="#" onclick="exportChartImages()">
                    <i class="bi bi-image me-2"></i> Export Charts as Images
                </a>
            </li>
            <li>
                <a class="dropdown-item" href="#" onclick="exportPDF()">
                    <i class="bi bi-file-pdf me-2"></i> Export as PDF Report
                </a>
            </li>
            <li>
                <a class="dropdown-item" href="#" onclick="exportExcel()">
                    <i class="bi bi-file-excel me-2"></i> Export Data as Excel
                </a>
            </li>
        </ul>
    </div>
</div>
```

---

## 🔧 Technical Implementation Plan

### Phase 1: Collection Trends Filter
**Files to Modify:**
1. `Controllers/OfficerController.cs`
   - Add new action: `GetMonthlyTrends(string timeRange, string academicYear)`
   - Return JSON with filtered monthly data

2. `Views/Officer/OrgTreasurerDashboard.cshtml`
   - Add filter dropdowns in Collection Trends header
   - Add JavaScript function `applyTrendsFilter()`

3. New JavaScript file: `wwwroot/js/org-treasurer-dashboard.js`
   - Handle filter change events
   - Make AJAX call to `GetMonthlyTrends`
   - Update chart with `trendsChart.data.labels = ...` + `trendsChart.update()`

**Data Flow:**
```
User selects filter → applyTrendsFilter() → AJAX GET /Officer/GetMonthlyTrends?timeRange=3&year=2025-2026
→ Controller filters data → Returns JSON → JavaScript updates chart
```

---

### Phase 2: Export Functionality
**Files to Modify:**
1. `Views/Officer/OrgTreasurerDashboard.cshtml`
   - Add export dropdown button in main header
   - Remove export button from Program-Year section (line 256-257)
   - Add IDs to sections for capture: `id="trendsChartSection"`, `id="programYearSection"`

2. New JavaScript file: `wwwroot/js/dashboard-export.js`
   - Function: `exportChartImages()` - uses html2canvas
   - Function: `exportPDF()` - uses jsPDF + html2canvas
   - Function: `exportExcel()` - uses SheetJS (xlsx)

3. Add CDN/NPM packages:
   - html2canvas: `<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>`
   - jsPDF: `<script src="https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js"></script>`
   - SheetJS: `<script src="https://cdn.sheetjs.com/xlsx-0.20.1/package/dist/xlsx.full.min.js"></script>`

**Export Functions:**
```javascript
// Export charts as images
async function exportChartImages() {
    const zip = new JSZip(); // If bundling multiple images
    
    // Capture Collection Trends
    const trendsCanvas = await html2canvas(document.getElementById('trendsChartSection'));
    const trendsImg = trendsCanvas.toDataURL('image/png');
    
    // Capture Program-Year Section
    const pyCanvas = await html2canvas(document.getElementById('programYearSection'));
    const pyImg = pyCanvas.toDataURL('image/png');
    
    // Download or zip
    downloadImage(trendsImg, 'collection_trends.png');
    downloadImage(pyImg, 'program_year_stats.png');
}

// Export as PDF
async function exportPDF() {
    const { jsPDF } = window.jspdf;
    const pdf = new jsPDF('portrait', 'mm', 'a4');
    
    // Add header
    pdf.setFontSize(18);
    pdf.text('Organization Treasury Dashboard', 20, 20);
    pdf.setFontSize(10);
    pdf.text('Generated: ' + new Date().toLocaleString(), 20, 28);
    
    // Capture and add charts
    const trendsCanvas = await html2canvas(document.getElementById('trendsChartSection'));
    const trendsImg = trendsCanvas.toDataURL('image/png');
    pdf.addImage(trendsImg, 'PNG', 20, 40, 170, 80);
    
    // Add more sections...
    
    pdf.save('OrgTreasury_Report_' + new Date().toISOString().split('T')[0] + '.pdf');
}

// Export to Excel
function exportExcel() {
    const wb = XLSX.utils.book_new();
    
    // Sheet 1: Quick Stats
    const statsData = [
        ['Metric', 'Amount'],
        ['Total Fees Collected', '@ViewBag.TotalFeesCollected'],
        ['Total Fines Collected', '@ViewBag.TotalFinesCollected'],
        // ...
    ];
    const ws1 = XLSX.utils.aoa_to_sheet(statsData);
    XLSX.utils.book_append_sheet(wb, ws1, 'Quick Stats');
    
    // Sheet 2: Monthly Trends
    const monthlyData = @Html.Raw(Json.Serialize(ViewBag.MonthlyTrends));
    const ws2 = XLSX.utils.json_to_sheet(monthlyData);
    XLSX.utils.book_append_sheet(wb, ws2, 'Monthly Trends');
    
    // Download
    XLSX.writeFile(wb, 'OrgTreasury_Data_' + new Date().toISOString().split('T')[0] + '.xlsx');
}
```

---

## 📊 Data Structure

### GetMonthlyTrends API Response
```json
{
  "success": true,
  "data": [
    {
      "Month": "Aug 2025",
      "FeesCollected": 15000.00,
      "FinesCollected": 2500.00
    },
    {
      "Month": "Sep 2025",
      "FeesCollected": 18000.00,
      "FinesCollected": 3000.00
    }
    // ...
  ],
  "timeRange": "Last 3 Months",
  "academicYear": "2025-2026"
}
```

---

## 🎯 User Experience Flow

### Scenario 1: Filtering Collection Trends
1. User lands on Org Treasurer Dashboard
2. Sees Collection Trends showing "Last 3 Months" by default
3. Wants to see full academic year trends
4. Clicks "Time Range" dropdown → Selects "Full Academic Year"
5. Clicks "Apply" button
6. Chart smoothly updates to show Aug-Feb data (current academic year)
7. X-axis now shows all months from Aug 2025 to present

### Scenario 2: Exporting Dashboard
1. User reviews dashboard data
2. Clicks "Export Dashboard" button (top right)
3. Dropdown menu appears with 3 options
4. User selects "Export Charts as Images"
5. Browser downloads 2 PNG files:
   - `collection_trends.png` - screenshot of the chart
   - `program_year_stats.png` - screenshot of program-year cards
6. User can use these images in reports/presentations

### Scenario 3: Generating PDF Report
1. User clicks "Export Dashboard" → "Export as PDF Report"
2. JavaScript captures all dashboard sections
3. Generates professional PDF with:
   - iBITS header
   - Quick stats summary
   - Chart images
   - Program-year table
   - Timestamp footer
4. PDF downloads as `OrgTreasury_Report_2026-02-11.pdf`
5. User can print or email the report

---

## ⚡ Performance Considerations

### Collection Trends Filter
- **Initial Load:** Fetch full academic year data (Aug-Present)
- **Filter Change:** 
  - **Option A (Client-side):** Filter existing data in JavaScript (FAST)
  - **Option B (Server-side):** Make AJAX call for new data (MORE ACCURATE)
  - **RECOMMENDATION:** Use Option B for accuracy and to reduce initial payload

### Export Operations
- **html2canvas:** 
  - Pro: Works client-side, no server load
  - Con: Can be slow for complex layouts (2-3 seconds)
  - **Solution:** Show loading spinner during capture
  
- **PDF Generation:**
  - Pro: Creates professional reports
  - Con: Large file size if many charts
  - **Solution:** Compress images to JPEG before adding to PDF

---

## 🧪 Testing Plan

### Filter Testing
- [ ] Test "Last Month" filter
- [ ] Test "Last 3 Months" filter (default)
- [ ] Test "Last 6 Months" filter
- [ ] Test "Current Semester" filter
- [ ] Test "Full Academic Year" filter
- [ ] Test switching academic years (2025-2026, 2024-2025)
- [ ] Test with empty data (no collections in selected range)
- [ ] Test chart updates without page reload

### Export Testing
- [ ] Test "Export Charts as Images" - verify 2 PNGs download
- [ ] Test "Export as PDF" - verify PDF structure and content
- [ ] Test "Export Data as Excel" - verify 3 sheets with correct data
- [ ] Test with different screen sizes (responsive)
- [ ] Test with large datasets (performance)
- [ ] Test download file naming (correct date format)

---

## 📦 Required Libraries/CDNs

### For Export Functionality
```html
<!-- html2canvas - Screenshot capture -->
<script src="https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js"></script>

<!-- jsPDF - PDF generation -->
<script src="https://cdn.jsdelivr.net/npm/jspdf@2.5.1/dist/jspdf.umd.min.js"></script>

<!-- SheetJS - Excel generation -->
<script src="https://cdn.sheetjs.com/xlsx-0.20.1/package/dist/xlsx.full.min.js"></script>

<!-- JSZip (Optional - if bundling multiple images) -->
<script src="https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js"></script>

<!-- FileSaver.js (Helper for downloads) -->
<script src="https://cdn.jsdelivr.net/npm/file-saver@2.0.5/dist/FileSaver.min.js"></script>
```

---

## 📅 Implementation Timeline (Estimated)

### Phase 1: Collection Trends Filter
- **Backend (Controller):** 1 hour
- **Frontend (UI + JavaScript):** 2 hours
- **Testing:** 1 hour
- **Total:** ~4 hours

### Phase 2: Export Functionality
- **Setup libraries:** 30 minutes
- **Export Charts as Images:** 2 hours
- **Export as PDF:** 3 hours
- **Export to Excel:** 2 hours
- **UI Integration:** 1 hour
- **Testing:** 2 hours
- **Total:** ~10 hours

### Total Estimated Time: 14 hours

---

## 🎨 Color Scheme (Consistent with iBITS Portal)

### Buttons
- **Export Button:** `btn-outline-gold` (gold border, white text)
- **Apply Filter Button:** `btn-outline-gold` (matches export)

### Dropdowns
- **Background:** Dark glass panel (existing theme)
- **Text:** White
- **Hover:** Gold highlight

### Charts
- **Fees Line:** Blue (#3b82f6)
- **Fines Line:** Orange (#f59e0b)
- **Grid:** White with 10% opacity

---

## ✅ Success Criteria

### Collection Trends Filter
- ✅ User can select different time ranges
- ✅ Chart updates within 1 second of clicking "Apply"
- ✅ X-axis labels show correct months for selected range
- ✅ Works with different academic years
- ✅ Handles empty data gracefully (shows message)

### Export Functionality
- ✅ Export button visible in dashboard header
- ✅ Old export button removed from Program-Year section
- ✅ All 3 export options work correctly
- ✅ Downloaded files have correct names with dates
- ✅ Images are high quality (readable text)
- ✅ PDF has proper formatting and branding
- ✅ Excel has multiple sheets with organized data

---

## 🚀 Next Steps

**Before proceeding with implementation, please confirm:**

1. **Collection Trends Filter:**
   - ✅ Approve the dropdown approach (Time Range + Academic Year)?
   - ✅ Approve the filter options (Last Month, 3 Months, 6 Months, etc.)?
   - 🤔 Any additional filter options needed?

2. **Export Functionality:**
   - ✅ Approve moving export to dashboard header?
   - ✅ Approve the 3 export options (Images, PDF, Excel)?
   - 🤔 Any specific requirements for PDF layout?
   - 🤔 Any additional data to include in Excel export?

3. **Priority:**
   - 🤔 Implement both features together, or one at a time?
   - 🤔 Which is more urgent: Filter or Export?

---

**Once approved, I will proceed with implementation in this order:**
1. Collection Trends Filter (Backend + Frontend)
2. Export Functionality (Images → PDF → Excel)
3. Testing and refinement

Please review this plan and let me know if you'd like any changes or have additional requirements!
