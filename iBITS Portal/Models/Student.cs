// ============================================================
// FILE PATH: Models/Student.cs
// ============================================================
// UPDATED: Added SchoolYearEnrolled property for tracking
// the academic year when the student was enrolled.
// ============================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models;

public partial class Student
{
    [Key]
    public string StudentNum { get; set; } = null!;

    public string? StudentImage { get; set; }

    public string? Qrcode { get; set; }

    public string StudentFn { get; set; } = null!;

    public string? StudentMn { get; set; }

    public string StudentLn { get; set; } = null!;

    public DateOnly? Birthday { get; set; }

    public string? StudentEmail { get; set; }

    public string? Course { get; set; }

    public string? YearLevelSection { get; set; }

    public string? StudentType { get; set; }

    public string? Classification { get; set; }

    public int? OfficerId { get; set; }

    public bool? IsArchived { get; set; }

    public string? ArchiveStatus { get; set; } // "Graduated", "Continuing"

    public DateOnly? ArchiveDate { get; set; }

    // NEW: School Year Enrolled - tracks when the student enrolled (e.g., "A.Y. 2025-2026")
    [MaxLength(20)]
    public string? SchoolYearEnrolled { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Fee> Fees { get; set; } = new List<Fee>();

    public virtual Officer? Officer { get; set; }

    // This property is for UI calculation and is not mapped to the database
    [NotMapped]
    public decimal Balance { get; set; }

    // NEW HELPER PROPERTY: FullName (Not Mapped to DB)
    [NotMapped]
    public string FullName
    {
        get
        {
            // Handles cases where middle name might be null or empty
            return string.IsNullOrWhiteSpace(StudentMn)
                ? $"{StudentFn} {StudentLn}"
                : $"{StudentFn} {StudentMn} {StudentLn}";
        }
    }
}