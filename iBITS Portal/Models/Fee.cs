// C:\Users\Dave\OneDrive\Desktop\needs to be update\Heres the code you will need to update\Fee.cs
// Models/Fee.cs

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

    // NEW: Academic Year field
    public string? AcadYear { get; set; }

    public string StudentNum { get; set; } = null!;

    public virtual Student? StudentNumNavigation { get; set; }
}
