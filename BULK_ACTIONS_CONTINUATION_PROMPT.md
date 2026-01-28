# Bulk Actions Implementation - Continuation Prompt

## Context
I'm working on the iBITS Portal project located at: `C:\Users\Dave\source\repos\iBITS Portal`

We have **partially implemented** bulk selection and actions for fees and fines management. The **ClassFines view is COMPLETE**, and we need to replicate the same functionality to the remaining 3 views.

## What's Already Complete ✅

### ClassFines View - FULLY IMPLEMENTED
1. **UI Components:**
   - Select All checkbox in table header (per category)
   - Individual checkboxes for each fine row
   - Bulk actions toolbar that appears when items are selected
   - Shows count of selected items
   - "Mark as Paid" and "Revoke" bulk action buttons

2. **Remittance Locking:**
   - Remitted fines show lock icon instead of checkbox
   - Remitted fines cannot be selected or modified
   - Action buttons show "Remitted" badge for locked items
   - Prevents editing of items that have been remitted to Org Treasurer

3. **JavaScript (in ClassFines.cshtml):**
   - Select All functionality per category
   - Individual checkbox change handlers
   - Indeterminate state for partial selection
   - Real-time selected count updates
   - Show/hide toolbar based on selection
   - Bulk action functions with confirmation dialogs
   - AJAX calls to controller methods

4. **Controller Methods (OfficerController.cs):**
   - `BulkMarkFinesAsPaid(BulkFineActionRequest)` - Marks multiple fines as paid
   - `BulkRevokeFines(BulkFineActionRequest)` - Revokes multiple fine payments
   - Request model classes: `BulkFineActionRequest` and `BulkFeeActionRequest`

## What Needs to Be Done ⏳

### Task 1: ClassFees View
**File:** `Views/Officer/ClassFees.cshtml`

Apply the same bulk actions pattern from ClassFines:

1. **Add checkbox column to table:**
   - Select All checkbox in `<thead>`
   - Individual checkboxes in each `<tr>` (only for unlocked items)
   - Lock icon for remitted fees

2. **Add bulk actions toolbar:**
   - Hidden by default
   - Shows selected count
   - "Mark as Paid" button
   - "Revoke" button

3. **Add JavaScript:**
   - Copy bulk selection logic from ClassFines
   - Adjust selectors for fees (`.fee-checkbox` instead of `.fine-checkbox`)
   - Update bulk action calls to use `BulkMarkFeesAsPaid` and `BulkRevokeFees`

4. **Add controller methods:**
   - `BulkMarkFeesAsPaid(BulkFeeActionRequest)`
   - `BulkRevokeFees(BulkFeeActionRequest)`

### Task 2: OrgFines View
**File:** `Views/Officer/OrgFines.cshtml`

Same pattern as ClassFines, but for Org Treasurer:

1. Add checkbox column with Select All
2. Add bulk actions toolbar
3. Add JavaScript for bulk selection
4. Add controller methods:
   - `BulkMarkOrgFinesAsPaid(BulkFineActionRequest)` - for Org Treasurer
   - `BulkRevokeOrgFines(BulkFineActionRequest)`

### Task 3: OrgFees View
**File:** `Views/Officer/OrgFees.cshtml`

Same pattern as ClassFees, but for Org Treasurer:

1. Add checkbox column with Select All
2. Add bulk actions toolbar
3. Add JavaScript for bulk selection
4. Add controller methods:
   - `BulkMarkOrgFeesAsPaid(BulkFeeActionRequest)`
   - `BulkRevokeOrgFees(BulkFeeActionRequest)`

## Implementation Pattern to Follow

### 1. Table Structure Changes
```html
<!-- Add to <thead> -->
<th style="width: 40px;">
    <input type="checkbox" class="form-check-input select-all-category" data-category="@categoryName" title="Select All">
</th>

<!-- Add to each <tr> in <tbody> -->
<td>
    @if (!isLocked)
    {
        <input type="checkbox" class="form-check-input [fine/fee]-checkbox" data-[fine/fee]-id="@item.Id" data-category="@categoryName">
    }
    else
    {
        <i class="bi bi-lock-fill text-muted" title="Remitted - Cannot modify"></i>
    }
</td>
```

### 2. Bulk Toolbar Template
```html
<!-- Add before category table -->
<div class="bulk-actions-toolbar" style="display: none; background: rgba(59, 130, 246, 0.15); border: 1px solid rgba(59, 130, 246, 0.3); border-radius: 8px; padding: 12px; margin-bottom: 15px;">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <span class="text-white fw-bold"><span id="selectedCount-@categoryName.Replace(" ", "-")">0</span> selected</span>
        </div>
        <div>
            <button type="button" class="btn btn-sm btn-success me-2 bulk-mark-paid" data-category="@categoryName">
                <i class="bi bi-check-circle me-1"></i> Mark as Paid
            </button>
            <button type="button" class="btn btn-sm btn-warning bulk-revoke" data-category="@categoryName">
                <i class="bi bi-x-circle me-1"></i> Revoke
            </button>
        </div>
    </div>
</div>
```

### 3. JavaScript Template (add before closing </script>)
```javascript
// BULK SELECTION FUNCTIONALITY
document.addEventListener('DOMContentLoaded', function() {
    // Select All checkbox handler
    document.querySelectorAll('.select-all-category').forEach(selectAll => {
        selectAll.addEventListener('change', function() {
            const category = this.dataset.category;
            const table = document.querySelector(`table[data-category-table="${category}"]`);
            const checkboxes = table.querySelectorAll('.[fee/fine]-checkbox:not(:disabled)');
            checkboxes.forEach(cb => cb.checked = this.checked);
            updateBulkToolbar(category);
        });
    });

    // Individual checkbox handler
    document.querySelectorAll('.[fee/fine]-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            const category = this.dataset.category;
            updateBulkToolbar(category);
            updateSelectAllState(category);
        });
    });

    // Bulk Mark as Paid
    document.querySelectorAll('.bulk-mark-paid').forEach(btn => {
        btn.addEventListener('click', async function() {
            const category = this.dataset.category;
            const selectedIds = getSelected[Fee/Fine]Ids(category);
            if (selectedIds.length === 0) {
                showToast('warning', 'Please select items to mark as paid');
                return;
            }
            if (!confirm(`Mark ${selectedIds.length} item(s) as paid?`)) return;
            await bulkAction('Bulk[Action]Name', selectedIds, category);
        });
    });

    // Bulk Revoke
    document.querySelectorAll('.bulk-revoke').forEach(btn => {
        btn.addEventListener('click', async function() {
            const category = this.dataset.category;
            const selectedIds = getSelected[Fee/Fine]Ids(category);
            if (selectedIds.length === 0) {
                showToast('warning', 'Please select items to revoke');
                return;
            }
            if (!confirm(`Revoke payment for ${selectedIds.length} item(s)?`)) return;
            await bulkAction('Bulk[Revoke]Name', selectedIds, category);
        });
    });
});

// Helper functions: getSelectedIds, updateBulkToolbar, updateSelectAllState, bulkAction
// (Copy from ClassFines.cshtml)
```

### 4. Controller Method Template
```csharp
[HttpPost]
[Authorize(Roles = "[Class/Org] Treasurer")]
public async Task<IActionResult> Bulk[Action]Name([FromBody] Bulk[Fee/Fine]ActionRequest request)
{
    try
    {
        if (request.[Fee/Fine]Ids == null || request.[Fee/Fine]Ids.Count == 0)
        {
            return Json(new { success = false, message = "No items selected" });
        }

        var user = await _userManager.GetUserAsync(User);
        var treasurer = await _context.Students.FindAsync(user?.UserName);

        if (treasurer == null)
        {
            return Json(new { success = false, message = "Treasurer profile not found" });
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var id in request.[Fee/Fine]Ids)
        {
            var item = await _context.[Fees/Fines]
                .Include(f => f.StudentNumNavigation)
                .FirstOrDefaultAsync(f => f.[Fee/Fine]Id == id);

            if (item == null || item.[Fees/Fines]Status == "Paid" || item.RemittanceStatus != FeeRemittanceStatus.NotRemitted)
            {
                failCount++;
                continue;
            }

            // Mark as paid
            item.[Fees/Fines]Status = "Paid";
            item.CollectionDate = DateTime.Now;
            item.CollectedBy = treasurer.StudentNum;

            successCount++;
        }

        await _context.SaveChangesAsync();

        return Json(new { 
            success = true, 
            message = $"Successfully marked {successCount} item(s) as paid" + (failCount > 0 ? $" ({failCount} skipped)" : "")
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = $"Error: {ex.Message}" });
    }
}
```

## Key Points to Remember

1. **Locking Logic:**
   - `isLocked = fine.RemittanceStatus != FeeRemittanceStatus.NotRemitted || isClosed;`
   - Locked items show lock icon, cannot be selected

2. **Table Attribute:**
   - Add `data-category-table="@categoryName"` to `<table>` tag

3. **Row Attributes:**
   - Add `data-is-locked="@isLocked.ToString().ToLower()"`
   - Add `data-category="@categoryName"`

4. **Authorization:**
   - ClassFees/ClassFines: `[Authorize(Roles = "Class Treasurer")]`
   - OrgFees/OrgFines: `[Authorize(Roles = "Org Treasurer")]`

## Testing Checklist

After implementing each view, test:
- ✅ Checkboxes appear for unlocked items
- ✅ Lock icons appear for remitted items
- ✅ Select All checkbox works
- ✅ Indeterminate state shows for partial selection
- ✅ Bulk toolbar appears when items selected
- ✅ Selected count updates correctly
- ✅ Bulk Mark as Paid works
- ✅ Bulk Revoke works
- ✅ Remitted items cannot be modified
- ✅ Page reloads after successful action

## Files to Modify

1. `Views/Officer/ClassFees.cshtml`
2. `Views/Officer/OrgFines.cshtml`
3. `Views/Officer/OrgFees.cshtml`
4. `Controllers/OfficerController.cs` (add 6 new methods)

## Estimated Effort

- ClassFees: ~8-10 iterations
- OrgFines: ~8-10 iterations
- OrgFees: ~8-10 iterations
- **Total: ~25-30 iterations**

## Reference Implementation

The complete working example is in `Views/Officer/ClassFines.cshtml`. Use it as a reference for:
- Exact HTML structure
- JavaScript functions
- Data attributes
- Styling

---

## Session Prompt

Please implement bulk selection and actions for the remaining 3 views (ClassFees, OrgFines, OrgFees) following the exact pattern used in ClassFines. Start with ClassFees, then OrgFines, then OrgFees. For each view:

1. Add checkbox column and bulk toolbar to the view
2. Add JavaScript for bulk selection logic
3. Add controller methods for bulk actions
4. Test that remittance locking works correctly

The ClassFines implementation is complete and working - replicate it exactly to the other 3 views.
