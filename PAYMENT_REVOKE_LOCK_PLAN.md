# 🔒 Payment Revoke & Lock Feature - Detailed Plan

**Date:** February 12, 2026  
**Module:** Org Treasurer - Fee/Fine Payment Management  
**Requirement:** Add ability to revoke payments (reversible) and lock payments (permanent/irreversible)

---

## 📋 Current System Analysis

### **Current Payment Flow:**

```
1. Class Treasurer marks fee as "Paid"
   ↓
   FeeStatus = "Paid"
   RemittanceStatus = "NotRemitted"
   CollectedBy = Class Treasurer StudentNum
   CollectionDate = Now
   ↓
2. Class Treasurer creates Remittance batch
   ↓
   RemittanceStatus = "PendingRemittance"
   ↓
3. Org Treasurer validates Remittance
   ↓
   RemittanceStatus = "Remitted"
   OfficialPaymentDate = Now
   ↓
4. Payment is LOCKED (cannot be edited by anyone)
```

### **Current Locking Logic:**
- **Line 320 (OrgFees.cshtml):** `var isLocked = isPaidByClassTreasurer || isOfficiallyPaid;`
- **Fee.cs Line 127:** `public bool IsLocked => RemittanceStatus != FeeRemittanceStatus.NotRemitted;`
- **Fee.cs Line 140:** `public bool CanOrgTreasurerEdit => FeeStatus?.ToUpper() != "PAID" && RemittanceStatus != FeeRemittanceStatus.Remitted;`

### **Current Edit Restrictions:**
- ✅ Org Treasurer CAN mark unpaid fees as paid
- ❌ Org Treasurer CANNOT edit fees already marked as "Paid" by Class Treasurer
- ❌ Org Treasurer CANNOT edit fees that are "Remitted" (officially validated)

---

## 🎯 New Requirements (Clarified)

Based on your request: *"when the org Treasurer handles a payment it must be still revoke it, if it wanted to lockout it will another options for making it and it should had a modal for th confirmation and it will not be undo it."*

### **Interpretation:**

1. **Revoke Payment** (Reversible Action)
   - Org Treasurer marks a fee as "Paid"
   - Later, Org Treasurer realizes it was a mistake
   - **Can revoke** the payment (set it back to "Unpaid")
   - This is **before** the payment is locked

2. **Lock Payment** (Permanent/Irreversible Action)
   - Org Treasurer marks a fee as "Paid"
   - After verifying it's correct, Org Treasurer **locks** the payment
   - Once locked, **CANNOT be revoked or edited** (permanent)
   - Requires **confirmation modal** (warning that this action cannot be undone)

---

## 💡 Proposed Solution

### **New Payment States:**

```
State 1: UNPAID
  ↓ (Org Treasurer marks as paid)
State 2: PAID (UNLOCKED) ← NEW STATE
  ↓ (Can be revoked back to UNPAID)
  ↓ OR
  ↓ (Org Treasurer locks payment with confirmation)
State 3: PAID (LOCKED) ← NEW STATE
  ↓ (PERMANENT - Cannot be revoked)
```

### **Database Schema Changes:**

Add new field to `Fee` model:

```csharp
/// <summary>
/// Indicates if this payment has been locked by Org Treasurer (permanent)
/// Once locked, payment cannot be revoked or edited
/// </summary>
public bool IsPaymentLocked { get; set; } = false;

/// <summary>
/// Date when payment was locked (for audit trail)
/// </summary>
public DateTime? PaymentLockedDate { get; set; }

/// <summary>
/// Who locked the payment (StudentNum of Org Treasurer)
/// </summary>
[StringLength(450)]
public string? LockedBy { get; set; }
```

---

## 🎨 UI/UX Design

### **A. Fee Record Actions (in Table)**

#### **Current UI:**
```
[Checkbox] | Ref ID | Student | ... | Status | [Action: Mark as Paid]
```

#### **Proposed UI:**

**For UNPAID fees:**
```
[Checkbox] | Ref ID | Student | ... | Status: Unpaid | [✅ Mark as Paid]
```

**For PAID (UNLOCKED) fees:**
```
[Checkbox] | Ref ID | Student | ... | Status: Paid ⚠️ | [🔒 Lock Payment] [↩️ Revoke]
```
- ⚠️ icon indicates payment is not locked yet
- Yellow/orange badge color

**For PAID (LOCKED) fees:**
```
[Checkbox] | Ref ID | Student | ... | Status: Paid ✅🔒 | [No actions - grayed out]
```
- ✅ checkmark + 🔒 padlock icon
- Green badge color
- Row has locked appearance (different background)

---

### **B. Revoke Payment Modal**

**Trigger:** Click "Revoke" button on a PAID (UNLOCKED) fee

**Modal Design:**
```
┌──────────────────────────────────────────────────┐
│ ⚠️  Revoke Payment                        [X]    │
├──────────────────────────────────────────────────┤
│                                                   │
│         [⚠️ Warning Icon - Yellow/Orange]        │
│                                                   │
│  Are you sure you want to revoke this payment?   │
│                                                   │
│  ┌────────────────────────────────────────────┐  │
│  │ Student:    Juan Dela Cruz                │  │
│  │ Fee:        Laboratory Fee                │  │
│  │ Amount:     ₱500.00                       │  │
│  │ Paid Date:  Feb 12, 2026 10:30 AM        │  │
│  └────────────────────────────────────────────┘  │
│                                                   │
│  This will set the fee back to "Unpaid" status.  │
│  You can mark it as paid again later if needed.  │
│                                                   │
│  Reason for revocation (optional):               │
│  ┌────────────────────────────────────────────┐  │
│  │ [Text area for notes/reason]              │  │
│  └────────────────────────────────────────────┘  │
│                                                   │
├──────────────────────────────────────────────────┤
│          [Cancel]         [⚠️ Revoke Payment]    │
└──────────────────────────────────────────────────┘
```

**Behavior:**
- Sets `FeeStatus` back to `"Unpaid"` or `"Pending"`
- Clears `CollectionDate`, `CollectedBy`, `OfficialPaymentDate`
- Creates audit log entry
- Shows success message: "Payment revoked. Fee is now Unpaid."

---

### **C. Lock Payment Modal** (Most Important - Permanent Action)

**Trigger:** Click "Lock Payment" button on a PAID (UNLOCKED) fee

**Modal Design:**
```
┌──────────────────────────────────────────────────┐
│ 🔒 Lock Payment - PERMANENT ACTION        [X]   │
├──────────────────────────────────────────────────┤
│                                                   │
│         [🔒 Padlock Icon - Large, Red]           │
│                                                   │
│  ⚠️ WARNING: THIS ACTION CANNOT BE UNDONE! ⚠️    │
│                                                   │
│  ┌────────────────────────────────────────────┐  │
│  │ Student:    Juan Dela Cruz                │  │
│  │ Fee:        Laboratory Fee                │  │
│  │ Amount:     ₱500.00                       │  │
│  │ Paid Date:  Feb 12, 2026 10:30 AM        │  │
│  └────────────────────────────────────────────┘  │
│                                                   │
│  Once locked, this payment:                      │
│  ❌ CANNOT be revoked                            │
│  ❌ CANNOT be edited                             │
│  ❌ CANNOT be deleted                            │
│  ✅ Will be permanently marked as PAID           │
│                                                   │
│  Please verify the payment details are correct   │
│  before locking.                                 │
│                                                   │
│  Type "LOCK" to confirm:                         │
│  ┌────────────────────────────────────────────┐  │
│  │ [Text input - must type "LOCK"]           │  │
│  └────────────────────────────────────────────┘  │
│                                                   │
├──────────────────────────────────────────────────┤
│          [Cancel]    [🔒 Lock Payment - DISABLED]│
│                      ↑ Enabled only when         │
│                        "LOCK" is typed           │
└──────────────────────────────────────────────────┘
```

**Behavior:**
- User MUST type "LOCK" (case-insensitive) to enable the button
- Sets `IsPaymentLocked = true`
- Sets `PaymentLockedDate = DateTime.Now`
- Sets `LockedBy = Org Treasurer StudentNum`
- Creates audit log entry
- Shows success message: "Payment locked successfully. This action is permanent."
- Row appearance changes to locked state (green badge, locked icon, grayed out)

---

### **D. Bulk Actions Update**

**Current Bulk Actions:**
```
[✅ Mark as Paid]  [❌ Revoke]
```

**Proposed Bulk Actions:**
```
[✅ Mark as Paid]  [🔒 Lock Selected]  [↩️ Revoke Selected]
```

**Bulk Lock Behavior:**
- Shows confirmation modal listing all fees to be locked
- Requires typing "LOCK ALL" to confirm
- Only locks fees that are PAID and UNLOCKED
- Skips already locked fees
- Shows summary: "5 fees locked, 2 skipped (already locked)"

**Bulk Revoke Behavior:**
- Shows confirmation modal listing all fees to be revoked
- Only revokes fees that are PAID and UNLOCKED
- Skips locked fees with warning: "2 fees cannot be revoked (locked)"
- Shows summary: "3 fees revoked, 2 skipped (locked)"

---

## 🔧 Backend Implementation

### **A. Database Migration**

**Migration Script:**
```sql
-- Add new columns to Fee table
ALTER TABLE Fee
ADD IsPaymentLocked BIT NOT NULL DEFAULT 0;

ALTER TABLE Fee
ADD PaymentLockedDate DATETIME2 NULL;

ALTER TABLE Fee
ADD LockedBy NVARCHAR(450) NULL;

-- Add foreign key for LockedBy
ALTER TABLE Fee
ADD CONSTRAINT FK_Fee_LockedBy_Student 
FOREIGN KEY (LockedBy) REFERENCES Student(StudentNum);

-- Add index for faster queries
CREATE INDEX IX_Fee_IsPaymentLocked ON Fee(IsPaymentLocked);
```

---

### **B. Model Updates**

**File:** `Models/Fee.cs`

```csharp
/// <summary>
/// Indicates if this payment has been locked by Org Treasurer (permanent)
/// </summary>
public bool IsPaymentLocked { get; set; } = false;

/// <summary>
/// Date when payment was locked
/// </summary>
public DateTime? PaymentLockedDate { get; set; }

/// <summary>
/// Org Treasurer who locked the payment
/// </summary>
[StringLength(450)]
public string? LockedBy { get; set; }

/// <summary>
/// Navigation property to the treasurer who locked
/// </summary>
[ForeignKey("LockedBy")]
public virtual Student? LockedByNavigation { get; set; }

// ============================================================
// Updated Computed Properties
// ============================================================

/// <summary>
/// Checks if Org Treasurer can revoke this payment
/// Only unlocked paid fees can be revoked
/// </summary>
[NotMapped]
public bool CanOrgTreasurerRevoke => FeeStatus?.ToUpper() == "PAID" 
                                      && !IsPaymentLocked 
                                      && RemittanceStatus != FeeRemittanceStatus.Remitted;

/// <summary>
/// Checks if Org Treasurer can lock this payment
/// Only unlocked paid fees can be locked
/// </summary>
[NotMapped]
public bool CanOrgTreasurerLock => FeeStatus?.ToUpper() == "PAID" 
                                    && !IsPaymentLocked 
                                    && RemittanceStatus != FeeRemittanceStatus.Remitted;

/// <summary>
/// Updated: Payment is locked if manually locked OR officially remitted
/// </summary>
[NotMapped]
public bool IsLocked => IsPaymentLocked || RemittanceStatus == FeeRemittanceStatus.Remitted;
```

---

### **C. Controller Actions**

**File:** `Controllers/OfficerController.cs`

#### **1. Revoke Payment Action**

```csharp
/// <summary>
/// Revoke a payment (set back to Unpaid)
/// Only allowed for PAID and UNLOCKED fees
/// </summary>
[HttpPost]
[Authorize(Roles = "Org Treasurer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RevokePayment(int feeId, string? reason)
{
    var fee = await _context.Fees
        .Include(f => f.StudentNumNavigation)
        .FirstOrDefaultAsync(f => f.FeeId == feeId);

    if (fee == null)
    {
        TempData["Error"] = "Fee not found.";
        return RedirectToAction("OrgFees");
    }

    // Check if fee is locked
    if (fee.IsPaymentLocked)
    {
        TempData["Error"] = "This payment is locked and cannot be revoked.";
        return RedirectToAction("OrgFees");
    }

    // Check if already remitted (also locked)
    if (fee.RemittanceStatus == FeeRemittanceStatus.Remitted)
    {
        TempData["Error"] = "This payment has been remitted and cannot be revoked.";
        return RedirectToAction("OrgFees");
    }

    // Check if already unpaid
    if (fee.FeeStatus?.ToUpper() != "PAID")
    {
        TempData["Warning"] = "This fee is already unpaid.";
        return RedirectToAction("OrgFees");
    }

    var user = await _userManager.GetUserAsync(User);
    var treasurer = await _context.Students.FindAsync(user.UserName);

    // Revoke the payment
    fee.FeeStatus = "Pending"; // or "Unpaid"
    fee.CollectedBy = null;
    fee.CollectionDate = null;
    fee.OfficialPaymentDate = null;
    fee.RemittanceStatus = FeeRemittanceStatus.NotRemitted;
    fee.RemittanceId = null;

    _context.Fees.Update(fee);

    // Create audit log
    var auditLog = new AuditLog
    {
        Action = "Payment Revoked",
        PerformedBy = treasurer?.StudentNum,
        PerformedAt = DateTime.Now,
        EntityType = "Fee",
        EntityId = feeId.ToString(),
        Details = $"Fee #{feeId} for {fee.StudentNumNavigation?.FullName} - {fee.FeeName} (₱{fee.Amount:N2}) revoked. Reason: {reason ?? "No reason provided"}"
    };
    _context.AuditLogs.Add(auditLog);

    // Notify student
    if (!string.IsNullOrEmpty(fee.StudentNum))
    {
        _context.Notifications.Add(new Notification
        {
            StudentNum = fee.StudentNum,
            Title = "Payment Revoked",
            Message = $"The payment for '{fee.FeeName}' (₱{fee.Amount:N2}) has been revoked by the Org Treasurer. Please contact your treasurer for clarification.",
            NotificationType = "Payment",
            NotificationDate = DateTime.Now,
            IsRead = false,
            SentBy = treasurer?.StudentNum
        });
    }

    await _context.SaveChangesAsync();

    TempData["Success"] = $"Payment revoked successfully. Fee is now Unpaid.";
    return RedirectToAction("OrgFees");
}
```

#### **2. Lock Payment Action**

```csharp
/// <summary>
/// Lock a payment permanently (cannot be undone)
/// Only allowed for PAID and UNLOCKED fees
/// </summary>
[HttpPost]
[Authorize(Roles = "Org Treasurer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> LockPayment(int feeId, string confirmation)
{
    // Verify confirmation word
    if (string.IsNullOrWhiteSpace(confirmation) || !confirmation.Equals("LOCK", StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"] = "You must type 'LOCK' to confirm this permanent action.";
        return RedirectToAction("OrgFees");
    }

    var fee = await _context.Fees
        .Include(f => f.StudentNumNavigation)
        .FirstOrDefaultAsync(f => f.FeeId == feeId);

    if (fee == null)
    {
        TempData["Error"] = "Fee not found.";
        return RedirectToAction("OrgFees");
    }

    // Check if already locked
    if (fee.IsPaymentLocked)
    {
        TempData["Warning"] = "This payment is already locked.";
        return RedirectToAction("OrgFees");
    }

    // Check if not paid
    if (fee.FeeStatus?.ToUpper() != "PAID")
    {
        TempData["Error"] = "Only paid fees can be locked.";
        return RedirectToAction("OrgFees");
    }

    var user = await _userManager.GetUserAsync(User);
    var treasurer = await _context.Students.FindAsync(user.UserName);

    // Lock the payment
    fee.IsPaymentLocked = true;
    fee.PaymentLockedDate = DateTime.Now;
    fee.LockedBy = treasurer?.StudentNum;

    _context.Fees.Update(fee);

    // Create audit log
    var auditLog = new AuditLog
    {
        Action = "Payment Locked",
        PerformedBy = treasurer?.StudentNum,
        PerformedAt = DateTime.Now,
        EntityType = "Fee",
        EntityId = feeId.ToString(),
        Details = $"Fee #{feeId} for {fee.StudentNumNavigation?.FullName} - {fee.FeeName} (₱{fee.Amount:N2}) PERMANENTLY LOCKED by {treasurer?.FullName}"
    };
    _context.AuditLogs.Add(auditLog);

    await _context.SaveChangesAsync();

    TempData["Success"] = $"Payment locked successfully. This action is permanent and cannot be undone.";
    return RedirectToAction("OrgFees");
}
```

#### **3. Bulk Revoke Action**

```csharp
[HttpPost]
[Authorize(Roles = "Org Treasurer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BulkRevokePayments(int[] feeIds)
{
    if (feeIds == null || feeIds.Length == 0)
    {
        TempData["Error"] = "No fees selected.";
        return RedirectToAction("OrgFees");
    }

    var fees = await _context.Fees
        .Where(f => feeIds.Contains(f.FeeId))
        .ToListAsync();

    int revoked = 0;
    int skipped = 0;
    var skippedReasons = new List<string>();

    foreach (var fee in fees)
    {
        if (fee.IsPaymentLocked || fee.RemittanceStatus == FeeRemittanceStatus.Remitted)
        {
            skipped++;
            skippedReasons.Add($"Fee #{fee.FeeId} (locked)");
            continue;
        }

        if (fee.FeeStatus?.ToUpper() != "PAID")
        {
            skipped++;
            skippedReasons.Add($"Fee #{fee.FeeId} (not paid)");
            continue;
        }

        fee.FeeStatus = "Pending";
        fee.CollectedBy = null;
        fee.CollectionDate = null;
        fee.OfficialPaymentDate = null;
        fee.RemittanceStatus = FeeRemittanceStatus.NotRemitted;
        fee.RemittanceId = null;

        revoked++;
    }

    await _context.SaveChangesAsync();

    var message = $"{revoked} payment(s) revoked successfully.";
    if (skipped > 0)
    {
        message += $" {skipped} skipped (locked or not paid).";
    }

    TempData["Success"] = message;
    return RedirectToAction("OrgFees");
}
```

#### **4. Bulk Lock Action**

```csharp
[HttpPost]
[Authorize(Roles = "Org Treasurer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BulkLockPayments(int[] feeIds, string confirmation)
{
    if (string.IsNullOrWhiteSpace(confirmation) || !confirmation.Equals("LOCK ALL", StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"] = "You must type 'LOCK ALL' to confirm this permanent action.";
        return RedirectToAction("OrgFees");
    }

    if (feeIds == null || feeIds.Length == 0)
    {
        TempData["Error"] = "No fees selected.";
        return RedirectToAction("OrgFees");
    }

    var fees = await _context.Fees
        .Where(f => feeIds.Contains(f.FeeId))
        .ToListAsync();

    var user = await _userManager.GetUserAsync(User);
    var treasurer = await _context.Students.FindAsync(user.UserName);

    int locked = 0;
    int skipped = 0;

    foreach (var fee in fees)
    {
        if (fee.IsPaymentLocked || fee.FeeStatus?.ToUpper() != "PAID")
        {
            skipped++;
            continue;
        }

        fee.IsPaymentLocked = true;
        fee.PaymentLockedDate = DateTime.Now;
        fee.LockedBy = treasurer?.StudentNum;

        locked++;
    }

    await _context.SaveChangesAsync();

    var message = $"{locked} payment(s) locked successfully.";
    if (skipped > 0)
    {
        message += $" {skipped} skipped (already locked or not paid).";
    }

    TempData["Success"] = message;
    return RedirectToAction("OrgFees");
}
```

---

## 📊 Visual Status Indicators

### **Badge Colors:**

| Status | Badge Color | Icon | Example |
|--------|-------------|------|---------|
| Unpaid | Red (`badge-danger`) | ❌ | `❌ Unpaid` |
| Paid (Unlocked) | Yellow/Orange (`badge-warning`) | ⚠️ | `⚠️ Paid` |
| Paid (Locked) | Green (`badge-success`) | ✅🔒 | `✅🔒 Paid (Locked)` |
| Remitted | Blue (`badge-primary`) | 🔒 | `🔒 Remitted` |

---

## 🧪 Testing Scenarios

### **Scenario 1: Revoke Payment**
1. Org Treasurer marks fee as Paid
2. Realizes it was wrong student
3. Clicks "Revoke" button
4. Confirms revocation in modal
5. ✅ Fee returns to Unpaid status
6. ✅ Student gets notification

### **Scenario 2: Lock Payment**
1. Org Treasurer marks fee as Paid
2. Verifies payment is correct
3. Clicks "Lock Payment" button
4. Types "LOCK" in confirmation modal
5. Confirms lock action
6. ✅ Fee becomes permanently locked
7. ✅ Revoke button disappears
8. ✅ Lock button disappears

### **Scenario 3: Try to Revoke Locked Payment**
1. Payment is already locked
2. Org Treasurer tries to revoke
3. ❌ System shows error: "This payment is locked and cannot be revoked."

### **Scenario 4: Bulk Lock**
1. Select 5 paid fees (3 unlocked, 2 already locked)
2. Click "Lock Selected"
3. Type "LOCK ALL" in modal
4. ✅ 3 fees locked
5. ✅ 2 skipped (already locked)
6. Shows summary message

---

## 🔐 Security & Audit

### **Audit Logging:**
Every payment action is logged:
- Who performed the action
- When it was performed
- Which fee was affected
- Old value vs new value
- Reason (for revocations)

### **Permissions:**
- **Org Treasurer ONLY** can revoke and lock payments
- **Class Treasurer** cannot revoke or lock
- **Admin** can view audit logs

### **Data Integrity:**
- Once locked, fee record is immutable
- Audit trail is permanent
- Database constraints prevent accidental unlocking

---

## 📝 UI/UX Improvements

1. **Visual Feedback:**
   - Locked rows have darker/grayed background
   - Hover tooltips explain why actions are disabled
   - Success animations for lock/revoke actions

2. **User Guidance:**
   - Help icon (?) next to "Lock" button explaining it's permanent
   - Warning banners before bulk actions
   - Confirmation dialogs with clear consequences

3. **Accessibility:**
   - ARIA labels for screen readers
   - Keyboard navigation support
   - Color-blind friendly badge colors

---

## ✅ Implementation Checklist

### **Phase 1: Database**
- [ ] Create migration script
- [ ] Add `IsPaymentLocked`, `PaymentLockedDate`, `LockedBy` columns
- [ ] Add indexes
- [ ] Test migration on dev database

### **Phase 2: Backend**
- [ ] Update `Fee.cs` model
- [ ] Add `CanOrgTreasurerRevoke` property
- [ ] Add `CanOrgTreasurerLock` property
- [ ] Create `RevokePayment` action
- [ ] Create `LockPayment` action
- [ ] Create `BulkRevokePayments` action
- [ ] Create `BulkLockPayments` action
- [ ] Add audit logging

### **Phase 3: Frontend**
- [ ] Update `OrgFees.cshtml` table to show lock status
- [ ] Add "Revoke" button (conditional)
- [ ] Add "Lock Payment" button (conditional)
- [ ] Create "Revoke Payment" modal
- [ ] Create "Lock Payment" modal (with "LOCK" confirmation)
- [ ] Create "Bulk Lock" modal (with "LOCK ALL" confirmation)
- [ ] Update badge colors and icons
- [ ] Add JavaScript validation

### **Phase 4: Testing**
- [ ] Test revoke payment (single)
- [ ] Test lock payment (single)
- [ ] Test bulk revoke
- [ ] Test bulk lock
- [ ] Test locked payment cannot be revoked
- [ ] Test audit logs are created
- [ ] Test student notifications
- [ ] Test UI/UX on different screen sizes

### **Phase 5: Documentation**
- [ ] Update user manual
- [ ] Create training materials for Org Treasurers
- [ ] Document audit procedures

---

## 🎯 Summary

This plan provides:
✅ **Reversible action** - Revoke payment (can be undone)  
✅ **Permanent action** - Lock payment (cannot be undone)  
✅ **Confirmation modals** - With type-to-confirm for lock action  
✅ **Audit trail** - Full logging of all actions  
✅ **User-friendly UI** - Clear visual indicators  
✅ **Bulk operations** - Lock/revoke multiple payments  
✅ **Security** - Proper authorization and validation  

---

**Ready for implementation! 🚀**
