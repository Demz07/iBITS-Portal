// C:\Users\Dave\OneDrive\Desktop\this where the updated code must be located\Models\Fine.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models;

public partial class Fine
{
    public int FineId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    public DateOnly? FinesStartDate { get; set; }

    public DateOnly? FinesDueDate { get; set; }

    public string? FinesStatus { get; set; }

    // Link to Attendance (for Event-based fines)
    public int? AttendanceId { get; set; }

    public virtual Attendance? Attendance { get; set; }

    // Properties for Manual Fines
    public string? Description { get; set; }

    public string? StudentNum { get; set; }

    // Navigation property - using StudentNumNavigation to match existing codebase
    [ForeignKey("StudentNum")]
    public virtual Student? StudentNumNavigation { get; set; }

    // NEW: Property to group manually created fines together
    public string? BatchId { get; set; }
}
