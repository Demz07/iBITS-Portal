# iBITS Portal - Semester Management System
## Complete User Guide & Workflow Documentation

---

## 📚 Table of Contents
1. [Overview](#overview)
2. [Semester Creation (Admin Only)](#semester-creation)
3. [Setting Current Semester](#setting-current-semester)
4. [Viewing Historical Data](#viewing-historical-data)
5. [Data Isolation & Automatic Assignment](#data-isolation)
6. [User Workflows](#user-workflows)
7. [FAQ](#faq)

---

## 🎯 Overview

The iBITS Portal uses a **Semester-based system** to organize all academic records. Every semester is independent, allowing you to:
- Track students, events, fees, and fines per semester
- View historical data from past semesters
- Create new semesters for each academic period
- Ensure data isolation between semesters

### Key Concepts:
- **Current Semester**: The active semester where new records are created
- **Historical Semester**: Past semesters that are read-only
- **Semester Context**: The system automatically filters data based on selected semester

---

## 🔧 Semester Creation (Admin Only)

### Who Can Create Semesters?
**Only Administrators** have permission to create new semesters.

### How to Create a New Semester:

1. **Navigate to Semester Management**
   - Go to Admin Dashboard → **Semesters**
   - Or use direct URL: \/Admin/Semesters\

2. **Click "Create New Semester"**
   - Fill in the required information:
     - **Academic Year**: e.g., "A.Y. 2025-2026"
     - **Semester Name**: e.g., "1st Semester", "2nd Semester", "Summer"
     - **Start Date**: First day of the semester
     - **End Date**: Last day of the semester
     - **Set as Current**: Check this to make it the active semester

3. **Submit**
   - The new semester is created
   - If marked as "Current", all new records will be assigned to this semester
   - Previous semester becomes historical (read-only)

### Important Notes:
- ✅ **Only ONE semester can be "Current" at a time**
- ✅ **Creating a new semester does NOT delete old data**
- ✅ **All previous records remain accessible in historical mode**
- ✅ **New semester starts empty and ready for new enrollment**

---

## ⭐ Setting Current Semester

### How to Change the Current Semester:

1. **Go to Semester Management**
   - Navigate to \/Admin/Semesters\

2. **Find the Semester**
   - Locate the semester you want to activate

3. **Click "Set Current"**
   - Confirm the action
   - System automatically:
     - Removes "Current" flag from all other semesters
     - Sets selected semester as "Current"
     - Updates the semester context system-wide

### What Happens When You Set a New Current Semester?
- ✅ All new students are enrolled in the new semester
- ✅ All new events are assigned to the new semester
- ✅ All new fees/fines are linked to the new semester
- ✅ Dashboard shows data for the current semester
- ✅ Previous semester data becomes historical (read-only)

---

## 📖 Viewing Historical Data

### How to View Past Semester Data:

1. **Use the Semester Selector**
   - Located in the top navigation bar (Admin & Officer views)
   - Dropdown shows all available semesters

2. **Select a Past Semester**
   - Click on the semester you want to view
   - Page automatically refreshes with historical data

3. **Historical Mode Indicators**
   - ⚠️ **Warning Banner**: Yellow banner appears showing "Historical Mode: Viewing past semester data (read-only)"
   - 🔒 **Read-Only Badges**: Action buttons are replaced with "Read Only" or "View Only" badges
   - 📊 **Data Filtering**: All tables, charts, and reports show only data from selected semester

### What You CAN Do in Historical Mode:
- ✅ View student records from that semester
- ✅ View events from that semester
- ✅ View fees and fines from that semester
- ✅ Export reports and data
- ✅ View attendance records
- ✅ Review payment history

### What You CANNOT Do in Historical Mode:
- ❌ Create new students
- ❌ Create new events
- ❌ Create new fees or fines
- ❌ Edit existing records
- ❌ Delete or archive records
- ❌ Mark fees/fines as paid
- ❌ Reset passwords
- ❌ Manage student roles

### Switching Back to Current Semester:
- Simply select the "Current" semester from the dropdown
- Or refresh the page without a semester filter

---

## 🔐 Data Isolation & Automatic Assignment

### How Data Isolation Works:

Every record in the system has a **SemesterId** foreign key that links it to a specific semester:

\\\
Student Enrollment → SemesterId
Events → SemesterId
Fees → SemesterId
Fines → SemesterId
Attendance → Event → SemesterId
\\\

### Automatic Semester Assignment:

When you create new records, the system **automatically assigns** the current semester:

#### **Students**:
- When admin creates a student, they're automatically enrolled in the current semester
- A \StudentSemester\ record is created linking the student to the semester
- Student appears in current semester's student list

#### **Events**:
- New events are automatically assigned to current semester
- Event attendances are linked to the semester through the event
- Fines from event absences inherit the semester from the event

#### **Fees**:
- Manual fees created by officers are assigned to current semester
- Organization-wide fees are assigned to current semester
- Class fees are assigned to current semester

#### **Fines**:
- Event-based fines inherit semester from the event
- Manual fines are assigned to current semester

### Example Workflow:

**Scenario**: Transitioning from 1st Semester to 2nd Semester

1. **Admin creates new semester**: "A.Y. 2025-2026 - 2nd Semester"
2. **Admin sets it as Current**
3. **System Response**:
   - All previous "1st Semester" data becomes read-only
   - Dashboard switches to show "2nd Semester" (empty, ready for new data)
   - New students enrolled → Linked to "2nd Semester"
   - New events created → Linked to "2nd Semester"
   - New fees/fines → Linked to "2nd Semester"

4. **Viewing Historical Data**:
   - Admin selects "1st Semester" from dropdown
   - All 1st Semester data appears (students, events, fees)
   - Warning banner shows: "Viewing historical data"
   - Cannot modify any records

---

## 👥 User Workflows

### Admin Workflow:

#### **Start of New Semester**:
1. Create new academic year (if needed) in Semester Management
2. Create new semester with start/end dates
3. Mark as "Current"
4. System is ready for new enrollment

#### **During Semester**:
1. Enroll students → Auto-assigned to current semester
2. Create events → Auto-assigned to current semester
3. Manage fees, fines, attendance → All linked to current semester
4. Switch to past semesters to review historical data

#### **End of Semester**:
1. Create next semester
2. Set next semester as "Current"
3. Previous semester becomes historical
4. All data preserved and accessible

### Officer Workflow:

#### **Org Treasurer**:
1. View fees for current semester
2. Create organization-wide fees → Auto-assigned to current semester
3. Mark fees as paid
4. View historical fees by selecting past semester from dropdown

#### **Class Treasurer**:
1. View fees for class in current semester
2. Create class-specific fees → Auto-assigned to current semester
3. Collect payments and submit remittances
4. Cannot modify historical data

#### **Org/Class Secretary**:
1. View attendance for current semester events
2. Cannot create events (Admin only)
3. Can view historical attendance by selecting past semester

---

## ❓ FAQ

### Q: What happens to student records when I create a new semester?
**A**: Student records are preserved. Students are enrolled in the new semester through \StudentSemester\ records. Their profile information remains unchanged, but their semester enrollment history is tracked.

### Q: Can I delete old semesters?
**A**: No. Semesters should not be deleted as they contain historical data. You can mark them as "Inactive" to hide them from filters.

### Q: What if I accidentally set the wrong semester as current?
**A**: Simply go back to Semester Management and set the correct semester as current. The system will update immediately.

### Q: Can multiple semesters be "Current" at the same time?
**A**: No. The system automatically removes the "Current" flag from all other semesters when you set a new one as current.

### Q: How do I view data from all semesters combined?
**A**: Currently, data is isolated by semester. To view all data, you would need to export reports from each semester individually.

### Q: Can officers create new semesters?
**A**: No. Only Administrators can create and manage semesters.

### Q: What happens if I try to edit historical data?
**A**: The UI prevents this by hiding all edit/delete buttons when viewing historical semesters. You'll see "Read Only" badges instead.

### Q: Can I re-activate a past semester as current?
**A**: Yes. If you need to add data to a past semester, you can set it as "Current" again. However, this is not recommended as it may cause confusion.

### Q: Are fees and fines carried over to new semesters?
**A**: No. Each semester starts fresh. Unpaid fees from previous semesters remain in that semester's historical records.

### Q: How do I know which semester I'm viewing?
**A**: 
- Check the semester selector in the top navigation
- Look for the warning banner (appears in historical mode)
- Dashboard title shows the selected semester name

### Q: Can students see historical semester data?
**A**: Students can view their own historical data (past fees, fines, attendance) but cannot modify anything.

---

## 🎓 Best Practices

1. **Create semesters in advance**: Set up the next semester before the current one ends
2. **Use consistent naming**: "A.Y. 2025-2026 - 1st Semester", "A.Y. 2025-2026 - 2nd Semester"
3. **Set accurate dates**: Start and end dates help with reporting and historical queries
4. **Don't modify historical data**: Keep past semesters untouched for audit trails
5. **Export reports regularly**: Generate and save reports before transitioning semesters
6. **Communicate transitions**: Notify officers when switching to a new semester

---

## 🔗 Quick Links

- **Semester Management**: \/Admin/Semesters\
- **Admin Dashboard**: \/Admin/Index\
- **Student Records**: \/Admin/StudentRecords\
- **Events Management**: \/Admin/Events\
- **Fees Management (Org)**: \/Officer/OrgFees\
- **Fines Management (Org)**: \/Officer/OrgFines\

---

## 📞 Support

For issues or questions about semester management:
1. Check this documentation first
2. Contact your system administrator
3. Review the implementation summary in \SEMESTER_CONTEXT_IMPLEMENTATION.md\

---

**Document Version**: 1.0  
**Last Updated**: February 08, 2026  
**Author**: iBITS Portal Development Team
