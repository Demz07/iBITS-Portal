using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models;

public partial class AcademicYear
{
    [Key]
    public int AcademicYearId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string YearName { get; set; } = null!; // "2025-2026", "2026-2027"
    
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
}

public partial class Semester
{
    [Key]
    public int SemesterId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string SemesterName { get; set; } = null!; // "1st Semester", "2nd Semester", "Summer"
    
    public int AcademicYearId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; } // Only ONE semester can be current at a time
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public virtual AcademicYear AcademicYear { get; set; } = null!;
    public virtual ICollection<StudentSemester> StudentSemesters { get; set; } = new List<StudentSemester>();
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

public partial class StudentSemester
{
    [Key]
    public int StudentSemesterId { get; set; }
    
    [Required]
    [StringLength(450)]
    public string StudentNum { get; set; } = null!;
    
    public int SemesterId { get; set; }
    public bool IsActive { get; set; }
    
    [StringLength(50)]
    public string? Section { get; set; }
    
    public int? YearLevel { get; set; }
    public DateTime EnrollmentDate { get; set; }
    
    [Required]
    [StringLength(20)]
    public string EnrollmentStatus { get; set; } = "Active"; // "Active", "Inactive", "Withdrawn"

    public virtual Semester Semester { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
