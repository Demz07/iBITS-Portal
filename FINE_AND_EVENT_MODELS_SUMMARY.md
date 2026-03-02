# iBITS Portal: Fine and Event Models & Controllers Summary

## Overview
This document summarizes the Fine and Event models, QR scan fine assignment mechanism, and role-based fine management in the iBITS Portal system.

---

## 1. EVENT MODEL (`Models/Event.cs`)

### Key Properties
- **EventId** (Primary Key)
- **EventName**: Name of the event
- **EventLocation**: Where the event takes place
- **EventDate**: DateOnly - the date of the event
- **EndDate**: DateOnly - end date (for multi-day events)
- **StartTime**: TimeOnly - event start time
- **EndTime**: TimeOnly - event end time
- **EventDuration**: String - duration in formats like "8 hours" or "9am-5pm"
- **CalculatedEndTime**: [NotMapped] Helper property that calculates actual end time
- **AcadYear**: Academic year of the event
- **EventDesc**: Event description
- **EventType**: "iBITS Event" or "Non-iBITS Event"
- **IsClosed**: Boolean - indicates if event is closed/force-stopped

### Role-Based Fine Amounts (NEW FEATURE)

#### iBITS Events:
- **FineForMember**: decimal - fine amount for regular members
- **FineForClassOfficer**: decimal - fine amount for class officers
- **FineForOrgOfficer**: decimal - fine amount for org officers

#### Non-iBITS Events:
- **NonIbitsFineForMember**: decimal
- **NonIbitsFineForClassOfficer**: decimal
- **NonIbitsFineForOrgOfficer**: decimal

### Relationships
- **Attendances**: ICollection<Attendance> - all attendance records for this event

### Helper Methods
- `CalculateDurationEndTime()`: Parses duration string and calculates end time
- `CalculatedEndTime` getter: Handles both time-based and duration-based calculations

---

## 2. FINE MODEL (`Models/Fine.cs`)

### Core Properties
- **FineId** (Primary Key)
- **Amount**: decimal(18,2) - fine amount
- **AmountPaid**: decimal - amount paid so far (disabled per business rule)
- **FinesStartDate**: DateOnly
- **FinesDueDate**: DateOnly
- **FinesStatus**: String - "Unpaid", "Paid", "Excused", "Waived"
- **Description**: String - reason for the fine (for manual fines)
- **StudentNum**: String - student who owes the fine
- **BatchId**: String - groups manually created fines together

### Attendance Link (for Event-Based Fines)
- **AttendanceId**: int? - links to Attendance record
- **Attendance**: virtual Attendance? - navigation property

### Remittance Tracking Fields
- **RemittanceStatus**: String - "NotRemitted", "PendingRemittance", "Remitted"
- **RemittanceId**: int? - foreign key to Remittance batch
- **CollectedBy**: StudentNum - Class Treasurer who collected
- **CollectionDate**: DateTime - when payment was collected
- **OfficialPaymentDate**: DateTime - official record date (set by Org Treasurer)

### Payment Lock Fields
- **IsPaymentLocked**: bool - cannot be revoked/edited when true
- **PaymentLockedDate**: DateTime
- **LockedBy**: StudentNum - Org Treasurer who locked it

### Navigation Properties
- **StudentNumNavigation**: virtual Student? - student who owes
- **Remittance**: virtual Remittance? - remittance batch
- **CollectedByNavigation**: virtual Student? - class treasurer
- **LockedByNavigation**: virtual Student? - org treasurer

### Computed Properties [NotMapped]
```csharp
public bool CanClassTreasurerEdit => RemittanceStatus == FeeRemittanceStatus.NotRemitted;
public bool IsLocked => IsPaymentLocked || RemittanceStatus != FeeRemittanceStatus.NotRemitted;
public bool IsOfficiallyPaid => RemittanceStatus == FeeRemittanceStatus.Remitted && OfficialPaymentDate.HasValue;
public bool CanOrgTreasurerEdit => FinesStatus?.ToUpper() != "PAID" 
                                    && RemittanceStatus != FeeRemittanceStatus.Remitted
                                    && !IsPaymentLocked;
public bool CanOrgTreasurerRevoke => FinesStatus?.ToUpper() == "PAID" 
                                      && !IsPaymentLocked 
                                      && RemittanceStatus == FeeRemittanceStatus.Remitted
                                      && RemittanceId == null;
public bool CanOrgTreasurerLock => FinesStatus?.ToUpper() == "PAID" 
                                    && !IsPaymentLocked 
                                    && RemittanceStatus == FeeRemittanceStatus.Remitted
                                    && RemittanceId == null;
public bool IsAwaitingValidation => RemittanceId.HasValue 
                                     && Remittance != null 
                                     && Remittance.Status == Models.RemittanceStatus.Pending;
public bool ShouldShowInOrgView => (RemittanceId.HasValue && Remittance != null && Remittance.Status != Models.RemittanceStatus.Rejected)
                                    || (RemittanceId == null && RemittanceStatus == FeeRemittanceStatus.Remitted);
```

---

## 3. ATTENDANCE MODEL (`Models/Attendance.cs`)

### Properties
- **AttendanceId**: int (Primary Key)
- **AttendanceStatus**: String - "Present", "Absent"
- **StudentNum**: String - student who attended
- **EventId**: int? - event they attended

### Relationships
- **Event**: virtual Event?
- **StudentNumNavigation**: virtual Student?
- **Fines**: ICollection<Fine> - fines linked to this attendance

---

## 4. QR SCANNER & FINE ASSIGNMENT (`OfficerController`)

### Scanner View (`Scanner()` action)
- **Authorization**: "Org Secretary, Class Secretary"
- **Functionality**:
  - Fetches all ACTIVE events for today (not closed)
  - Shows only events where StartTime <= Now < CalculatedEndTime
  - Filters by course/section for Class Secretaries
  - Returns list of active events to Scanner.cshtml view

### QR Code Processing (`ProcessScan()` action)
- **Authorization**: "Org Secretary, Class Secretary"
- **Input**: ScanRequest with ScannedData and EventId
- **QR Format**: "iBITS:" prefix (parsed by `ParseStudentNumFromQr()`)
- **Process**:
  1. Validates QR code format
  2. Looks up student in database
  3. Security check: Class Secretary can only scan students from their course/section
  4. Checks for duplicate attendance
  5. Creates new Attendance record with "Present" status
  6. Returns: StudentNum, FullName, ProfileImage, Section, ScanTime
  7. **NO FINE CREATION AT THIS STAGE** - only attendance record

### Key Classes
```csharp
public class ScanRequest 
{ 
    public string ScannedData { get; set; } = "";
    public int EventId { get; set; } 
}

public class VerifyRequest 
{ 
    public string ScannedData { get; set; } = "" 
}
```

---

## 5. FINE MANAGEMENT ENDPOINTS

### Create Manual Fine (`CreateManualFine()`)
- **Authorization**: Officers (Class/Org Treasurers)
- **Route**: POST to `CreateManualFine`
- **Parameters**:
  - `studentNum`: Target student
  - `amount`: Decimal fine amount
  - `dueDate`: DateOnly
  - `reason`: String - reason for fine
- **View**: `Views/Officer/CreateManualFine.cshtml`
- **Logic**: Creates manual Fine record with StudentNum (not linked to Attendance)

### Mark Fine as Paid (`MarkFineAsPaid()`)
- **Authorization**: "Class Treasurer, Org Treasurer"
- **Parameters**:
  - `fineId`: Fine ID
  - `paymentMethod`: Cash/Online/etc
  - `transactionRef`: Reference number
  - `notes`: Additional notes
- **Logic**:
  - Security: Class Treasurer can only mark fines for their section
  - Category Closure Check: Cannot edit if category validated
  - Sets `FinesStatus = "Paid"`
  - **Class Treasurer path**: Sets `CollectedBy` and `CollectionDate`, RemittanceStatus stays "NotRemitted"
  - **Org Treasurer path**: Sets `RemittanceStatus = "Remitted"`, `OfficialPaymentDate = Now`, `RemittanceId = null`
  - Creates `FinePaymentTransaction` audit record
  - Sends notification to student

### Get Fines Data (`GetFinesData()`)
- **Authorization**: "Org Treasurer, Class Treasurer"
- **Returns**: JSON with list of fines filtered by role/section
- **Used by**: AJAX endpoint for dynamic fine display

### Class Fines View (`ClassFines()`)
- **Authorization**: "Class Treasurer"
- **Shows**: Fines for students in Class Treasurer's section
- **Supports**: Export, bulk actions, filtering

### Org Fines View (`OrgFines()`)
- **Authorization**: "Org Treasurer"
- **Shows**: All organization-wide fines
- **Supports**: Export, bulk validation, bulk payment marking

---

## 6. ROLE-BASED FINE ASSIGNMENT (NOT IMPLEMENTED IN QR)

**Important**: The Event model has role-based fine properties (`FineForMember`, `FineForClassOfficer`, `FineForOrgOfficer`) but the current QR scanning process does NOT automatically create fines based on these amounts.

**Current Approach**: Fines are created MANUALLY via `CreateManualFine()` or through other manual processes. The QR scanner only records attendance.

**Potential Enhancement**: Could be implemented to:
1. Detect student's role (Member/Class Officer/Org Officer) from `Student.Officer` relationship
2. Look up appropriate fine amount from Event model based on role
3. Auto-create Fine record linked to Attendance
4. (Currently not implemented)

---

## 7. STUDENT MODEL RELATIONSHIPS

### Fine-Related Properties
```csharp
public virtual ICollection<Attendance> Attendances { get; set; }
public virtual ICollection<Fee> Fees { get; set; }
public virtual ICollection<Fine> Fines { get; set; }  // Direct collection
public virtual Officer? Officer { get; set; }  // Links to officer record
```

### Student Details for Fine Context
- **StudentNum**: String (Primary Key)
- **StudentFn, StudentLn, StudentMn**: Names
- **YearLevelSection**: String like "3-1" (Year-Section)
- **Course**: String like "BSIT" or "DIT"
- **Officer**: Optional Officer record (if class/org officer)
- **FullName**: Computed property combining names

---

## 8. OFFICER MODEL

### Properties
```csharp
[Key]
[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
public int OfficerId { get; set; }
public string? Classification { get; set; }  // "Class Officer" or "Org Officer"
public string? Position { get; set; }
public virtual ICollection<Student> Students { get; set; }  // Officers in this group
```

### Usage in Fine Context
- Used to determine student role: Is this student a class officer, org officer, or member?
- Could be used to assign appropriate fine amount from Event model
- Currently NOT integrated with QR fine assignment

---

## 9. KEY FINDINGS

### ✅ Working Features
1. **QR Scanning**: Records attendance successfully
2. **Manual Fine Creation**: Via `CreateManualFine()`
3. **Fine Payment Tracking**: Via `MarkFineAsPaid()`
4. **Role-Based Treasurer Views**: Class vs Org Treasurer views
5. **Remittance System**: Tracks fine validation workflow
6. **Payment Locking**: Org Treasurer can lock finalized payments

### ⚠️ Not Yet Implemented
1. **Automatic Fine Creation**: QR scan doesn't auto-create fines based on event role amounts
2. **Role-Based Amount Selection**: Event fine amounts (`FineForMember`, `FineForClassOfficer`, etc.) are defined but not used
3. **Event-to-Fine Linking**: Manual process only, not automatic based on attendance

### 🔍 Note on Role Detection
- Student has `Officer` navigation property
- Officer has `Classification` field ("Class Officer", "Org Officer")
- Could determine if student is Member/ClassOfficer/OrgOfficer by checking `Student.Officer` relationship

---

## 10. WORKFLOW SUMMARY

### Current (Actual) Workflow
```
1. Event Created (with role-based fine amounts)
2. QR Scanning → Attendance Record Created
3. Manual Fine Creation → Fine Record Created (OR auto-created by admin)
4. Fine Payment → Mark as Paid (Class Treasurer)
5. Remittance Submission → Class Treasurer submits batch
6. Fine Validation → Org Treasurer validates & locks
```

### Potential Future Workflow (with Auto-Assignment)
```
1. Event Created (with role-based fine amounts)
2. QR Scanning → Attendance Record Created
3. [AUTO] Check Student Role → Select appropriate fine amount
4. [AUTO] Create Fine Record linked to Attendance
5. Fine Payment → Mark as Paid (Class Treasurer)
6. Remittance Submission → Class Treasurer submits batch
7. Fine Validation → Org Treasurer validates & locks
```

---

## 11. DATABASE RELATIONSHIPS

```
Event (1) ─── (N) Attendance
                     │
                     └─── (1) Student
                     │
                     └─── (N) Fine

Student (1) ─── (N) Fine
   │
   └─── (1) Officer
```

---

## Summary
The Fine and Event models are well-structured with comprehensive role-based fine amounts and remittance tracking. However, the automatic assignment of fines based on student role and event fine amounts is not currently implemented in the QR scanning process. Fines are currently created manually, though the infrastructure supports automatic fine creation based on the role-based amounts defined in the Event model.
