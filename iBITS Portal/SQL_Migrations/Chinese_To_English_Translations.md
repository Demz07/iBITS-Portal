# iBITS Portal - Chinese to English Column Name Translations

## 📋 Translation Reference Guide

This document provides the complete mapping from Chinese column names to English equivalents for the iBITS Portal database schema.

## 🔤 Column Name Translations

### Core Academic Fields
| Chinese | English | Description |
|---------|---------|-------------|
| `学期名称` | `SemesterName` | Name of the semester (First Semester, Second Semester, Summer) |
| `学号` | `StudentNum` | Student number/ID |
| `专业` | `Major` | Student's major/course |
| `年级` | `GradeLevel` | Student's grade level (1, 2, 3, 4) |
| `年级班` | `GradeSection` | Student's section/class within grade level |
| `状态` | `Status` | Current status (Active, Inactive, Graduated) |

### Attendance Tracking Fields
| Chinese | English | Description |
|---------|---------|-------------|
| `累计缺勤次数` | `TotalAbsences` | Total count of absences |
| `累计缺勤百分比` | `AttendancePercentage` | Attendance rate percentage |
| `迟到次数` | `TotalTardies` | Total count of tardiness/late arrivals |
| `早退次数` | `TotalEarlyExits` | Total count of early exits |
| `请假次数` | `TotalLeaves` | Total count of approved leaves |
| `补课次数` | `TotalMakeups` | Total count of makeup classes |

### Time Tracking Fields
| Chinese | English | Description |
|---------|---------|-------------|
| `到校时间` | `TimeIn` | Check-in time when student arrives |
| `离校时间` | `TimeOut` | Check-out time when student leaves |
| `时长` | `DurationMinutes` | Duration in minutes between TimeIn and TimeOut |
| `扫描时间` | `ScanTime` | Timestamp when QR code was scanned |
| `扫描设备类型` | `ScanDeviceType` | Type of device used (Mobile, Desktop, Scanner) |
| `位置` | `Location` | Physical location of attendance |
| `MAC地址` | `MacAddress` | Hardware MAC address for device verification |
| `备注` | `Remarks` | Additional notes or dean's remarks |

### Academic Year Structure
| Chinese | English | Description |
|---------|---------|-------------|
| `学年` | `AcademicYear` | Academic year period (2024-2025, 2025-2026) |
| `第一学期` | `First Semester` | First semester of academic year |
| `第二学期` | `Second Semester` | Second semester of academic year |
| `夏季学期` | `Summer` | Summer semester |

## 🗂️ Database Schema Changes

### New Tables Created (English Names)
1. **AcademicYears** - Academic year definitions
2. **Semesters** - Semester definitions within academic years  
3. **StudentSemesters** - Student enrollment tracking by semester

### Columns Added to Existing Tables
- **Attendance**: `TimeIn`, `TimeOut`, `DurationMinutes`, `ScanDeviceType`, `Location`, `MacAddress`, `Remarks`, `SemesterId`
- **Student**: `Location`, `LastScanTime`, `LastScanDevice`, `SemesterId`
- **Event**: `SemesterId`

## 🎯 Benefits of English-Only Schema

### For Filipino Users
- **Consistency**: All column names follow English naming conventions
- **Maintainability**: Easier for developers to understand and maintain
- **International Standards**: Follows database naming best practices
- **Code Readability**: Entity Framework models map cleanly to database columns

### For System Administration
- **SQL Clarity**: Queries are more readable and maintainable
- **Documentation**: Standard English terms are well-documented
- **Training**: Easier to train new developers and DBAs
- **Integration**: Better compatibility with third-party tools and APIs

## 🔧 Implementation Details

### Constraints and Relationships
- **NO ACTION constraints** used to prevent cascade path conflicts
- **UNIQUE constraints** ensure data integrity
- **FOREIGN KEY relationships** maintain referential integrity
- **INDEXES** optimized for performance

### Default Values and Logic
- **Status defaults to 'Active'** for new student enrollments
- **AttendancePercentage defaults to 100.00%** for new records
- **IsCurrent flag** identifies active semester
- **GETDATE() defaults** for timestamp fields

## 📊 Data Migration Considerations

### Existing Data Compatibility
- All existing data remains compatible
- New columns are NULLABLE where appropriate
- Default values ensure backward compatibility
- Gradual migration path supported

### Performance Optimizations
- Indexes created on frequently queried columns
- Composite indexes for semester-based filtering
- Optimized for time-based attendance queries

## 🚀 Ready for Production

The English-only schema is production-ready with:
- ✅ No Chinese characters in database objects
- ✅ Complete semester system implementation
- ✅ Time-in/time-out attendance tracking
- ✅ Performance optimizations
- ✅ Cascade conflict prevention
- ✅ Comprehensive documentation

## 📞 Support

For any questions about the translation or implementation:
1. Refer to this translation guide
2. Check the verification script output
3. Review the main implementation script comments
4. Test with the provided SQL scripts

---
**Document Version**: 1.0  
**Last Updated**: 2025-02-05  
**Target Users**: Filipino students and administrators  
**Language**: English-only database schema