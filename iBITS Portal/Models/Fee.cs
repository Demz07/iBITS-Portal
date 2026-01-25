// C:\Users\Dave\OneDrive\Desktop\this where the updated code must be located\Models\Fee.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models;

public partial class Fee
{
    public int FeeId { get; set; }

    public string? FeeName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    public DateOnly? FeesStartDate { get; set; }

    public DateOnly? FeesDueDate { get; set; }

    public string? FeeStatus { get; set; }

    // Academic Year field
    public string? AcadYear { get; set; }

    // NEW: BatchId for grouping manually created fees
    public string? BatchId { get; set; }

    // NEW: DateCreated for tracking when the fee was created
    public DateTime? DateCreated { get; set; }

    public string StudentNum { get; set; } = null!;

    // Navigation property - using StudentNumNavigation to match existing codebase
    [ForeignKey("StudentNum")]
    public virtual Student? StudentNumNavigation { get; set; }
}
