# QA Testing Guide - Semester System

**Date**: February 8, 2026  
**Purpose**: Complete testing workflow for semester management system

---

## 🎯 Testing Workflow

### **Phase 1: Initial Setup (One-time)**

1. **Run Database Setup**
   ```sql
   Run: FINAL_Setup_All_Records_Current_Semester.sql
   ```
   - This moves ALL records to Semester 13 (current)
   - Clean starting point with no historical data

2. **Build & Run Application**
   - Visual Studio → `Ctrl + Shift + B` (Build)
   - Press `F5` to run
   - Login as Admin

---

### **Phase 2: Test Creating New Semester**

#### ✅ Test Case 1: Create Semester via Modal

**Steps:**
1. Login as Admin
2. Look at navbar
3. See dropdown showing current semester (e.g., "⭐ A.Y. 2025-2026 - 1st Semester")
4. Click **"New Semester"** button next to dropdown
5. Modal opens

**Modal Should Show:**
```
┌─────────────────────────────────────┐
│ Create New Semester            ✕    │
├─────────────────────────────────────┤
│ Academic Year                       │
│ [A.Y.] [2025] - [2026]             │
│ (Auto-calculates end year)          │
│                                     │
│ Semester                            │
│ [Dropdown: 1st/2nd/Summer]         │
│                                     │
│ Start Date: [Pick date]            │
│ End Date: [Pick date]              │
│                                     │
│ ☑ Set as current semester          │
│                                     │
│ Note: Old records preserved         │
├─────────────────────────────────────┤
│ [Cancel]  [Create Semester]         │
└─────────────────────────────────────┘
```

**Fill in:**
- Start Year: `2025` (auto-fills end year: `2026`)
- Semester: Select `2nd Semester`
- Start Date: `2026-02-08`
- End Date: `2026-06-30`
- ✓ Check "Set as current"

**Click**: "Create Semester"

**Expected Result:**
- ✅ Success message appears
- ✅ Modal closes
- ✅ Page reloads
- ✅ Dropdown now shows: "⭐ A.Y. 2025-2026 - 2nd Semester"
- ✅ Old semester (1st Semester) appears in dropdown without ⭐

---

### **Phase 3: Test Historical Viewing**

#### ✅ Test Case 2: View Historical Semester

**Steps:**
1. Click semester dropdown
2. Select the OLD semester (without ⭐)
3. Page reloads

**Expected Result:**
- ✅ Badge appears: "👁 Historical View"
- ✅ Data from old semester shows
- ✅ Can view all old fees, fines, events
- ✅ Edit/Delete buttons should be disabled (if implemented)

#### ✅ Test Case 3: Switch Back to Current

**Steps:**
1. Click semester dropdown
2. Select semester with ⭐
3. Page reloads

**Expected Result:**
- ✅ Historical badge disappears
- ✅ Shows current semester data
- ✅ Create/Edit/Delete buttons enabled

---

### **Phase 4: Test Data Isolation**

#### ✅ Test Case 4: Create Records in New Semester

**Steps:**
1. Make sure current semester is selected (⭐)
2. Go to Fees Management
3. Create a new fee
4. Note the fee details

**Switch to old semester:**
1. Select old semester from dropdown
2. Go to Fees Management

**Expected Result:**
- ✅ NEW fee does NOT appear in old semester
- ✅ Only OLD fees appear
- ✅ Data is isolated

**Switch back to current:**
1. Select current semester (⭐)
2. Go to Fees Management

**Expected Result:**
- ✅ NEW fee appears here
- ✅ Correct semester assignment

---

### **Phase 5: Rollback & Cleanup**

#### ✅ Test Case 5: Delete Test Semester

**When to use:**
After testing, you want to delete the test semester and restore original state.

**Steps:**
1. Open SQL Server Management Studio
2. Open: `ROLLBACK_Delete_Test_Semester.sql`
3. Check the semester list in the script comments
4. Update these lines:
   ```sql
   DECLARE @SemesterIdToDelete INT = 14;  -- Test semester
   DECLARE @RestoreCurrentSemesterId INT = 13;  -- Original
   ```
5. Run the script

**Expected Result:**
- ✅ Test semester deleted
- ✅ Original semester set as current
- ✅ All records back in original semester
- ✅ System restored to pre-test state

---

## 📊 Improved Modal Features

### **Academic Year Input:**
- ✅ **No more typing "A.Y. 2025-2026"**
- ✅ Just enter start year: `2025`
- ✅ End year auto-calculates: `2026`
- ✅ Format auto-generated: `A.Y. 2025-2026`

### **Semester Dropdown:**
- ✅ Select from predefined options:
  - 1st Semester
  - 2nd Semester
  - Summer

### **Date Pickers:**
- ✅ Calendar widget for easy date selection
- ✅ No manual typing required

---

## 🔄 Complete Test Cycle

### **Cycle 1: Initial Test**
1. Run `FINAL_Setup_All_Records_Current_Semester.sql`
2. Test creating new semester
3. Test historical viewing
4. Test data isolation

### **Cycle 2: Rollback & Repeat**
1. Run `ROLLBACK_Delete_Test_Semester.sql`
2. Verify system restored
3. Repeat tests if needed

### **Cycle 3: Production Readiness**
1. Create REAL new semester
2. Keep it (don't rollback)
3. Start using system normally

---

## ✅ QA Checklist

**Database:**
- [ ] Initial setup script ran successfully
- [ ] All records in current semester
- [ ] No orphaned records

**UI Components:**
- [ ] Semester dropdown appears in navbar
- [ ] "New Semester" button visible
- [ ] Modal opens on button click
- [ ] Modal has year input (not text)
- [ ] Modal has semester dropdown
- [ ] Date pickers work

**Create Semester:**
- [ ] Can enter start year (e.g., 2025)
- [ ] End year auto-fills (e.g., 2026)
- [ ] Can select semester from dropdown
- [ ] Can pick dates
- [ ] "Set as current" checkbox works
- [ ] Create button works
- [ ] New semester appears in dropdown
- [ ] Page reloads correctly

**Historical Viewing:**
- [ ] Can select old semester
- [ ] Historical badge appears
- [ ] Old data shows
- [ ] Can select current semester again
- [ ] Badge disappears

**Data Isolation:**
- [ ] New records go to current semester
- [ ] Old records stay in old semester
- [ ] No data mixing

**Rollback:**
- [ ] Can delete test semester
- [ ] Records move back to original
- [ ] Current semester restored
- [ ] System works normally after rollback

---

## 🐛 Known Issues / Notes

**Issue 1: Session Timeout**
- If you're inactive for 2 hours, session expires
- Selected semester resets to current
- Solution: Login again

**Issue 2: Browser Cache**
- If dropdown doesn't update after creating semester
- Solution: Hard refresh (Ctrl + F5)

**Issue 3: Multiple Admins**
- Each admin has their own selected semester
- Session is isolated per user
- No conflicts

---

## 📝 Test Scenarios

### **Scenario 1: End of Semester**
1. Current: A.Y. 2025-2026 - 1st Semester
2. Create: A.Y. 2025-2026 - 2nd Semester
3. Set as current
4. Result: 1st Semester archived, 2nd Semester active

### **Scenario 2: New Academic Year**
1. Current: A.Y. 2025-2026 - 2nd Semester
2. Create: A.Y. 2026-2027 - 1st Semester
3. Set as current
4. Result: Old year archived, new year active

### **Scenario 3: Summer Term**
1. Current: A.Y. 2025-2026 - 2nd Semester
2. Create: A.Y. 2025-2026 - Summer
3. Set as current
4. Result: 2nd Semester archived, Summer active

---

## 🎯 Success Criteria

**System is ready for production when:**
- ✅ All test cases pass
- ✅ Can create semesters via modal
- ✅ Can view historical data
- ✅ Data isolation works
- ✅ Rollback script works
- ✅ No errors in console
- ✅ Performance is acceptable

---

## 📞 Support

**If you encounter issues:**
1. Check browser console (F12)
2. Check application logs
3. Verify database connection
4. Try hard refresh (Ctrl + F5)
5. Rollback and retry

---

*End of QA Testing Guide*
