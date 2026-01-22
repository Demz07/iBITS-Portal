// ============================================================
// FILE PATH: Models/SystemSetting.cs
// ============================================================
// NEW: System settings model for storing application-wide
// configurations like Current Academic Year.
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models;

public class SystemSetting
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string SettingKey { get; set; } = null!;

    [MaxLength(500)]
    public string? SettingValue { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public DateTime? LastUpdated { get; set; }

    [MaxLength(256)]
    public string? UpdatedBy { get; set; }
}
