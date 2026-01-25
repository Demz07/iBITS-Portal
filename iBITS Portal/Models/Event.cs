using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models;

public partial class Event
{
    [Key]
    public int EventId { get; set; }

    public string? EventName { get; set; }

    public string? EventLocation { get; set; }

    public DateOnly? EventDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public string? EventDuration { get; set; }

    // Helper property to calculate actual end time for backward compatibility
    [NotMapped]
    public DateTime? CalculatedEndTime 
    {
        get 
        {
            if (EndTime.HasValue && EndDate.HasValue)
                return EndDate.Value.ToDateTime(EndTime.Value);
                
            if (StartTime.HasValue && EventDate.HasValue)
            {
                // For time-based events, use EndDate/EndTime
                if (EndDate.HasValue)
                    return EndDate.Value.ToDateTime(EndTime ?? new TimeOnly(17, 0, 0));
                
                // For duration-based events, calculate from duration
                if (!string.IsNullOrEmpty(EventDuration))
                {
                    return CalculateDurationEndTime();
                }
            }
            
            return null;
        }
    }

    // Helper method to parse duration and calculate end time
    private DateTime? CalculateDurationEndTime()
    {
        if (!EventDate.HasValue || !StartTime.HasValue || string.IsNullOrEmpty(EventDuration))
            return null;
            
        try
        {
            // Handle "X hours" format
            if (EventDuration.ToLower().Contains("hour"))
            {
                if (int.TryParse(EventDuration.Split(' ')[0], out int hours))
                {
                    return EventDate.Value.ToDateTime(StartTime.Value).AddHours(hours);
                }
            }
            
            // Handle "9am-5pm" format
            if (EventDuration.ToLower().Contains("am") || EventDuration.ToLower().Contains("pm"))
            {
                var parts = EventDuration.Split('-');
                if (parts.Length == 2)
                {
                    var startTimeStr = parts[0].Trim();
                    var endTimeStr = parts[1].Trim();
                    
                    // Parse end time
                    if (int.TryParse(endTimeStr.Replace("am", "").Replace("pm", ""), out int endHour))
                    {
                        var isPM = endTimeStr.Contains("pm");
                        var actualEndHour = isPM ? (endHour % 12 + 12) : endHour;
                        return EventDate.Value.ToDateTime(new TimeOnly(actualEndHour, 0, 0));
                    }
                }
            }
        }
        catch
        {
            return null;
        }
        return null;
    }

    public string? AcadYear { get; set; }

    public string? EventDesc { get; set; }

    // =================================================================
    // NEW PROPERTIES BASED ON YOUR REQUIREMENTS
    // =================================================================

    [Display(Name = "Event Type")]
    public string? EventType { get; set; } // e.g., "iBITS Event" or "Non-iBITS Event"

    [Display(Name = "Fine for Members")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FineForMember { get; set; }

    [Display(Name = "Fine for Class Officers")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FineForClassOfficer { get; set; }

    [Display(Name = "Fine for Org Officers")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FineForOrgOfficer { get; set; }

    // Properties for Non-iBITS events (different fine amounts)
    [Display(Name = "Non-iBITS Fine for Members")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NonIbitsFineForMember { get; set; }

    [Display(Name = "Non-iBITS Fine for Class Officers")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NonIbitsFineForClassOfficer { get; set; }

    [Display(Name = "Non-iBITS Fine for Org Officers")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NonIbitsFineForOrgOfficer { get; set; }

    public bool IsClosed { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [NotMapped]
    public bool IsInUse => Attendances.Any();
}