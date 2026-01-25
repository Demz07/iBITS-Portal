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

    public string? ArchiveStatus { get; set; }

    public DateOnly? ArchiveDate { get; set; }

    [MaxLength(20)]
    public string? SchoolYearEnrolled { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Fee> Fees { get; set; } = new List<Fee>();

    // NEW: Direct collection of fines
    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();

    public virtual Officer? Officer { get; set; }

    [NotMapped]
    public decimal Balance { get; set; }

    [NotMapped]
    public string FullName
    {
        get
        {
            return string.IsNullOrWhiteSpace(StudentMn)
                ? $"{StudentFn} {StudentLn}"
                : $"{StudentFn} {StudentMn} {StudentLn}";
        }
    }
}