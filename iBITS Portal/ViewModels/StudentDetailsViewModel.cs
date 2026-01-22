// ViewModels/StudentDetailsViewModel.cs

using System;
using System.Collections.Generic;
using iBITS_Portal.Models;

namespace iBITS_Portal.ViewModels
{
    public class StudentDetailsViewModel
    {
        // Core Info
        public Student Student { get; set; } = null!;
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Member";

        // Stats
        public int TotalEvents { get; set; }
        public int EventsAttended { get; set; }
        public double AttendanceRate { get; set; }

        public decimal TotalFees { get; set; }
        public decimal TotalFines { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }

        // Lists
        public List<AttendanceRecord> AttendanceHistory { get; set; } = new List<AttendanceRecord>();
        public List<Fee> Fees { get; set; } = new List<Fee>();
        public List<FineDisplay> Fines { get; set; } = new List<FineDisplay>();
    }

    public class AttendanceRecord
    {
        public string EventName { get; set; } = "";
        public DateOnly? Date { get; set; }
        public string Status { get; set; } = ""; // Present, Absent
        public bool HasFine { get; set; }
    }

    public class FineDisplay
    {
        public string EventName { get; set; } = "";
        public string Reason { get; set; } = "Absence";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public DateOnly? DueDate { get; set; }
    }
}