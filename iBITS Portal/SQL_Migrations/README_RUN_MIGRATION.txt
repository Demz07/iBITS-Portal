========================================
SQL MIGRATION INSTRUCTIONS
========================================

STEP 1: Clean up the failed migration
--------------------------------------
Execute this script first:
>> 000_ROLLBACK_Clean_Failed_Migration.sql

This will remove the partially created UserAnnouncementDismissals table.
(The ExpiryDate column that was successfully added will remain)


STEP 2: Run the corrected migration
--------------------------------------
Execute this script:
>> 001_Add_Announcement_Expiry_And_Dismissal_FIXED.sql

This will:
- Verify ExpiryDate column exists (already added)
- Create UserAnnouncementDismissals table with correct foreign keys
- Update existing payment reminders with 30-day expiry
- Show verification results


WHAT WAS FIXED:
--------------------------------------
Changed: REFERENCES Students(StudentNum)
To:      REFERENCES Student(StudentNum)

The table name is "Student" (singular), not "Students" (plural)


VERIFICATION:
--------------------------------------
After running both scripts, you should see:
✓ ExpiryDate column exists in Announcements table
✓ UserAnnouncementDismissals table created
✓ Foreign keys to Student and Announcements
✓ Indexes created
✓ Unique constraint on (StudentNum, AnnouncementId)


THEN:
--------------------------------------
Start your application and test the features!

========================================
