using System;
using System.Collections.Generic;

namespace iBITS_Portal.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public string? AttendanceStatus { get; set; }

    public string StudentNum { get; set; } = null!;

    public int? EventId { get; set; }

    public virtual Event? Event { get; set; }

    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();

    public virtual Student? StudentNumNavigation { get; set; }
}
