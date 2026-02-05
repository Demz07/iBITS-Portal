using System;
using System.Collections.Generic;

namespace iBITS_Portal.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public string? AttendanceStatus { get; set; }

    public string StudentNum { get; set; } = null!;

    public int? EventId { get; set; }

    // NEW: Time-In/Time-Out Fields
    public DateTime? TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
    public int? DurationMinutes { get; set; } // Computed in database
    public string? ScanDevice { get; set; }
    public string? Location { get; set; }

    // NEW: Semester Field
    public int? SemesterId { get; set; }

    // Existing Navigation Properties
    public virtual Event? Event { get; set; }
    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();
    public virtual Student? StudentNumNavigation { get; set; }
    
    // NEW: Semester Navigation Property
    public virtual Semester? Semester { get; set; }
    
    // QRAuditLog Navigation removed (keyless entity doesn't support navigation)
    // Use separate audit lookups if needed
}
