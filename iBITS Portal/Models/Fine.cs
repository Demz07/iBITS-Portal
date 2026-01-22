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

    public int? AttendanceId { get; set; }

    public virtual Attendance? Attendance { get; set; }
}
