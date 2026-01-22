using System;

namespace iBITS_Portal.Models;

public partial class ActivityLog
{
    public int Id { get; set; }

    public string Action { get; set; } = null!; // e.g., "Role Change", "Login", "Import"

    public string Description { get; set; } = null!; // Details of what happened

    public string PerformedBy { get; set; } = null!; // The Admin/User who did it

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }
}