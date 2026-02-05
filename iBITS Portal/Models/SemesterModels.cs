using System;
using System.Collections.Generic;

namespace iBITS_Portal.Models;

public partial class AcademicYear
{
    public int AcademicYearId { get; set; }
    public string YearName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}

public partial class Semester
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = null!; // "1st Semester", "2nd Semester", "Summer"
    public int AcademicYearId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public virtual AcademicYear AcademicYear { get; set; } = null!;
    public virtual ICollection<StudentSemester> StudentSemesters { get; set; } = new List<StudentSemester>();
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

public partial class StudentSemester
{
    public int StudentSemesterId { get; set; }
    public string StudentNum { get; set; } = null!;
    public int SemesterId { get; set; }
    public bool IsActive { get; set; }
    public string? Section { get; set; }
    public int? YearLevel { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public string EnrollmentStatus { get; set; } = "Active"; // "Active", "Inactive", "Withdrawn"

    public virtual Semester Semester { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}

public partial class QRAuditLog
{
    // Keyless entity - AuditId removed since it's now keyless
    public string StudentNum { get; set; } = null!;
    public string QRCodeData { get; set; } = null!;
    public DateTime ScanTime { get; set; }
    public string ScanType { get; set; } = null!; // "TimeIn", "TimeOut", "EventCheckIn"
    public string ProcessingResult { get; set; } = null!; // "Success", "Duplicate", "Invalid", "OutOfWindow"
    public int? AttendanceId { get; set; }
    public int? EventId { get; set; }
    public int? SemesterId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IPAddress { get; set; }
    public string? Location { get; set; }
    public string? ProcessedBy { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual Student Student { get; set; } = null!;
    public virtual Attendance? Attendance { get; set; }
    public virtual Event? Event { get; set; }
    public virtual Semester? Semester { get; set; }
}