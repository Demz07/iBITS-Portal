# SEMESTER SWITCHING - TESTING GUIDE

## 🧪 Test Plan for Semester Context System

---

## Pre-Test Setup

### 1. Verify Database Has Multiple Semesters
\\\sql
SELECT * FROM Semesters ORDER BY StartDate DESC;
SELECT * FROM AcademicYears;
\\\

Expected: At least 2 semesters (one current, one past)

### 2. Verify Service Registration
Check \Program.cs\ for:
\\\csharp
builder.Services.AddScoped<ISemesterContextService, SemesterContextService>();
\\\

### 3. Verify Controllers Have Service Injected
- AdminController should have \ISemesterContextService _semesterContext\
- OfficerController should have \ISemesterContextService _semesterContext\

---

## Test Suite 1: Semester Context Service

### Test 1.1: Get Current Semester
**Steps**:
1. Log in as Admin
2. Navigate to \/Admin/Index\
3. Check console/debug logs for current semester

**Expected**:
- Service returns semester with \IsCurrent = true\
- ViewBag.CurrentSemester is populated
- Dashboard shows current semester data

### Test 1.2: Get Selected Semester (No Selection)
**Steps**:
1. Fresh browser session
2. Navigate to Admin Dashboard

**Expected**:
- Selected semester defaults to current semester
- No historical mode banner appears

### Test 1.3: Set Viewing Semester
**Steps**:
1. Select a past semester from dropdown in navbar
2. Observe page reload

**Expected**:
- Session stores selected semester ID
- ViewBag.SelectedSemester changes
- ViewBag.IsHistoricalMode = true

---

## Test Suite 2: Data Filtering

### Test 2.1: Admin Dashboard - Current Semester
**Steps**:
1. Ensure current semester is selected
2. Navigate to \/Admin/Index\
3. Check student counts, fee/fine totals

**Expected**:
- Counts show only current semester data
- Charts display current semester statistics
- No historical mode warning

### Test 2.2: Admin Dashboard - Historical Semester
**Steps**:
1. Select a past semester from dropdown
2. Navigate to \/Admin/Index\

**Expected**:
- ⚠️ Yellow warning banner: "Historical Mode: Viewing past semester data (read-only)"
- Counts show only selected semester data
- Dashboard updates to show historical data

### Test 2.3: Events - Semester Filtering
**Steps**:
1. Navigate to \/Admin/Events\
2. Switch between semesters using dropdown

**Expected**:
- Events list updates to show only events from selected semester
- Event count changes based on semester
- Filter persists across page navigation

### Test 2.4: Student Records - Semester Filtering
**Steps**:
1. Navigate to \/Admin/StudentRecords\
2. Switch semesters

**Expected**:
- Student list updates based on semester enrollment
- Only students enrolled in selected semester appear
- StudentSemester relationship is used for filtering

### Test 2.5: Officer Fees - Semester Filtering
**Steps**:
1. Log in as Org Treasurer
2. Navigate to \/Officer/OrgFees\
3. Switch semesters

**Expected**:
- Fees list updates to selected semester
- Totals recalculate for selected semester
- Remittance stats update accordingly

---

## Test Suite 3: Read-Only Mode Enforcement

### Test 3.1: Admin Events - Historical Mode
**Steps**:
1. Select past semester
2. Navigate to \/Admin/Events\

**Expected**:
- ❌ "Create New Event" button is HIDDEN
- ❌ "Manage" button is HIDDEN on event cards
- ❌ "Close Event" button is HIDDEN
- ✅ "View Only" badge appears instead
- ⚠️ Warning banner shows historical mode

### Test 3.2: Admin Events - Current Mode
**Steps**:
1. Select current semester
2. Navigate to \/Admin/Events\

**Expected**:
- ✅ "Create New Event" button is VISIBLE
- ✅ "Manage" button is VISIBLE on event cards
- ✅ "Close Event" button is VISIBLE (for open events)
- ❌ No warning banner
- ❌ No "View Only" badges

### Test 3.3: Admin Student Records - Historical Mode
**Steps**:
1. Select past semester
2. Navigate to \/Admin/StudentRecords\

**Expected**:
- ❌ "Add New Student" button is HIDDEN
- ❌ Action dropdown (Edit, Manage Role, Reset Password) is HIDDEN
- ✅ "Read Only" badge appears in action column
- ⚠️ Warning banner visible

### Test 3.4: Admin Student Records - Current Mode
**Steps**:
1. Select current semester
2. Navigate to \/Admin/StudentRecords\

**Expected**:
- ✅ "Add New Student" button is VISIBLE
- ✅ Action dropdown is VISIBLE with all options
- ❌ No "Read Only" badges
- ❌ No warning banner

### Test 3.5: Officer Fees - Historical Mode
**Steps**:
1. Log in as Org Treasurer
2. Select past semester
3. Navigate to \/Officer/OrgFees\

**Expected**:
- ❌ "Create Fee" button is HIDDEN
- ⚠️ Warning banner shows historical mode
- ✅ Export button still visible (read-only operation)

### Test 3.6: Officer Fees - Current Mode
**Steps**:
1. Select current semester
2. Navigate to \/Officer/OrgFees\

**Expected**:
- ✅ "Create Fee" button is VISIBLE
- ✅ All fee management actions available
- ❌ No warning banner

---

## Test Suite 4: Auto-Assignment of Current Semester

### Test 4.1: Create Event - Auto-Assignment
**Steps**:
1. Ensure current semester is set (e.g., ID = 3)
2. Navigate to \/Admin/Events\
3. Click "Create New Event"
4. Fill form WITHOUT selecting semester
5. Submit

**Database Check**:
\\\sql
SELECT TOP 1 EventId, EventName, SemesterId 
FROM Events 
ORDER BY EventId DESC;
\\\

**Expected**:
- New event has \SemesterId = 3\ (current semester)
- Event automatically assigned even though not manually selected

### Test 4.2: Create Student - Auto-Enrollment
**Steps**:
1. Current semester ID = 3
2. Navigate to \/Admin/StudentRecords\
3. Click "Add New Student"
4. Fill form, submit

**Database Check**:
\\\sql
SELECT * FROM StudentSemesters 
WHERE StudentNum = 'NEW_STUDENT_NUM';
\\\

**Expected**:
- StudentSemester record created with \SemesterId = 3\
- Student automatically enrolled in current semester

### Test 4.3: Create Manual Fee - Auto-Assignment
**Steps**:
1. Log in as Org Treasurer
2. Current semester ID = 3
3. Navigate to Create Manual Fee
4. Fill form, submit

**Database Check**:
\\\sql
SELECT TOP 1 FeeId, FeeName, SemesterId 
FROM Fees 
ORDER BY FeeId DESC;
\\\

**Expected**:
- Fee has \SemesterId = 3\
- Auto-assigned to current semester

### Test 4.4: Create Manual Fine - Auto-Assignment
**Steps**:
1. Log in as Officer
2. Current semester ID = 3
3. Create manual fine
4. Submit

**Database Check**:
\\\sql
SELECT TOP 1 FineId, Description, SemesterId 
FROM Fines 
ORDER BY FineId DESC;
\\\

**Expected**:
- Fine has \SemesterId = 3\

---

## Test Suite 5: Data Isolation

### Test 5.1: Students Appear in Correct Semester Only
**Steps**:
1. Create student "Test Student A" while Semester 3 is current
2. Switch to Semester 2
3. Check if "Test Student A" appears

**Expected**:
- Student appears in Semester 3 list
- Student DOES NOT appear in Semester 2 list
- StudentSemester record only exists for Semester 3

### Test 5.2: Events Isolated by Semester
**Steps**:
1. Create event "Test Event A" in Semester 3
2. Switch to Semester 2
3. Check events list

**Expected**:
- Event appears in Semester 3
- Event DOES NOT appear in Semester 2
- Event has \SemesterId = 3\

### Test 5.3: Fees Isolated by Semester
**Steps**:
1. Create fee in Semester 3
2. Switch to Semester 2
3. Check fees list

**Expected**:
- Fee appears only in Semester 3
- Totals in Semester 2 don't include the new fee

---

## Test Suite 6: Semester Switching Persistence

### Test 6.1: Session Persistence
**Steps**:
1. Select Semester 2
2. Navigate to different pages (Events, Students, Fees)
3. Check if semester selection persists

**Expected**:
- Semester selection persists across page navigation
- ViewBag.SelectedSemester remains Semester 2
- Historical mode banner appears on all pages

### Test 6.2: Clear Session - Revert to Current
**Steps**:
1. Select Semester 2
2. Clear session/logout and login again
3. Check selected semester

**Expected**:
- Defaults back to current semester
- No historical mode

---

## Test Suite 7: Multi-User Scenarios

### Test 7.1: Admin and Officer See Same Semester
**Steps**:
1. Admin selects Semester 2
2. Officer (different user) views fees

**Expected**:
- Each user has independent semester selection (session-based)
- Officer sees current semester (their own session)
- Admin sees Semester 2 (their session)

### Test 7.2: Admin Changes Current Semester - System Wide Effect
**Steps**:
1. Admin sets Semester 4 as Current
2. Check behavior for all users

**Expected**:
- New records created by anyone go to Semester 4
- Semester 3 becomes historical
- \IsCurrent\ flag updates in database

---

## Regression Tests

### RT-1: Existing Data Not Affected
**Before**: Count records in each table
**After**: All changes - count should match
**Expected**: No data loss, only new columns populated

### RT-2: Null SemesterId Handling
**Test**: Query records with \SemesterId IS NULL\
**Expected**: System handles gracefully, treats as "All Semesters"

### RT-3: Export Functions Still Work
**Test**: Export students, events, fees in historical mode
**Expected**: Exports work, contain only selected semester data

---

## Sign-Off Checklist

- [ ] All Test Suite 1 tests pass (Service functionality)
- [ ] All Test Suite 2 tests pass (Data filtering)
- [ ] All Test Suite 3 tests pass (Read-only enforcement)
- [ ] All Test Suite 4 tests pass (Auto-assignment)
- [ ] All Test Suite 5 tests pass (Data isolation)
- [ ] All Test Suite 6 tests pass (Session persistence)
- [ ] All Test Suite 7 tests pass (Multi-user scenarios)
- [ ] All Regression Tests pass
- [ ] No console errors during semester switching
- [ ] Performance acceptable (< 1 second for semester switch)

---

## Known Limitations

1. **No Cross-Semester Reports**: Cannot generate reports across multiple semesters in one view
2. **Session-Based Selection**: Clearing browser data resets to current semester
3. **No Semester Archival**: Cannot delete or permanently hide old semesters
4. **Limited Bulk Operations**: Cannot move records between semesters

---

**Test Execution Date**: _____________  
**Tester Name**: _____________  
**Results**: ☐ Pass  ☐ Fail  ☐ Partial  
**Notes**: ___________________________________

