// ============================================================
// FILE: Models/Remittance.cs
// PURPOSE: Tracks batch remittances from Class Treasurer to Org Treasurer
// ============================================================
// This model represents a remittance batch submitted by a Class Treasurer
// containing payments collected from their section classmates.
// ============================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    /// <summary>
    /// Represents a remittance batch from Class Treasurer to Org Treasurer.
    /// Contains multiple RemittanceItems (individual student payments).
    /// </summary>
    public class Remittance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RemittanceId { get; set; }

        /// <summary>
        /// Unique batch code (e.g., "RMT-2026-0001")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string BatchCode { get; set; } = string.Empty;

        /// <summary>
        /// Fee category name (for fee remittances)
        /// </summary>
        [StringLength(200)]
        public string? FeeName { get; set; }

        /// <summary>
        /// Fine category (for fine remittances)
        /// </summary>
        [StringLength(200)]
        public string? FineCategory { get; set; }

        /// <summary>
        /// Type of remittance: "Fee" or "Fine"
        /// </summary>
        [Required]
        [StringLength(20)]
        public string RemittanceType { get; set; } = "Fee";

        /// <summary>
        /// YearLevelSection of the class (e.g., "BSIT 3-1")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Total amount of all payments in this remittance
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Count of students who paid
        /// </summary>
        [Required]
        public int TotalStudents { get; set; }

        /// <summary>
        /// Class Treasurer who submitted this remittance (StudentNum)
        /// </summary>
        [Required]
        [StringLength(450)]
        public string SubmittedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date when the remittance was submitted
        /// </summary>
        [Required]
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Current status: Pending, Validated, or Rejected
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = RemittanceStatus.Pending;

        /// <summary>
        /// Org Treasurer who validated/rejected (StudentNum)
        /// </summary>
        [StringLength(450)]
        public string? ValidatedBy { get; set; }

        /// <summary>
        /// THE OFFICIAL DATE - set when Org Treasurer validates
        /// This date syncs to all student payment records
        /// </summary>
        public DateTime? ValidationDate { get; set; }

        /// <summary>
        /// Notes from Org Treasurer during validation
        /// </summary>
        [StringLength(1000)]
        public string? ValidationNotes { get; set; }

        /// <summary>
        /// Reason if the remittance was rejected
        /// </summary>
        [StringLength(1000)]
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Academic year (e.g., "2025-2026")
        /// </summary>
        [StringLength(20)]
        public string? AcademicYear { get; set; }

        /// <summary>
        /// Semester ID for historical record tracking
        /// </summary>
        public int? SemesterId { get; set; }

        /// <summary>
        /// Record creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // ============================================================
        // Navigation Properties
        // ============================================================

        /// <summary>
        /// Individual payment items in this remittance
        /// </summary>
        public virtual ICollection<RemittanceItem> RemittanceItems { get; set; } = new List<RemittanceItem>();

        /// <summary>
        /// The Class Treasurer who submitted this remittance
        /// </summary>
        [ForeignKey("SubmittedBy")]
        public virtual Student? SubmittedByNavigation { get; set; }

        /// <summary>
        /// The Org Treasurer who validated this remittance
        /// </summary>
        [ForeignKey("ValidatedBy")]
        public virtual Student? ValidatedByNavigation { get; set; }

        /// <summary>
        /// Navigation property to Semester
        /// </summary>
        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        // ============================================================
        // Computed Properties
        // ============================================================

        /// <summary>
        /// Display name for the category (Fee or Fine)
        /// </summary>
        [NotMapped]
        public string CategoryName => RemittanceType == "Fee" ? FeeName ?? "Unknown Fee" : FineCategory ?? "Unknown Fine";

        /// <summary>
        /// Checks if this remittance is still pending
        /// </summary>
        [NotMapped]
        public bool IsPending => Status == RemittanceStatus.Pending;

        /// <summary>
        /// Checks if this remittance has been validated
        /// </summary>
        [NotMapped]
        public bool IsValidated => Status == RemittanceStatus.Validated;

        /// <summary>
        /// Checks if this remittance was rejected
        /// </summary>
        [NotMapped]
        public bool IsRejected => Status == RemittanceStatus.Rejected;

        /// <summary>
        /// Days since submission (for pending remittances)
        /// </summary>
        [NotMapped]
        public int DaysPending => (DateTime.Now - SubmittedDate).Days;
    }

    /// <summary>
    /// Constants for remittance status values
    /// </summary>
    public static class RemittanceStatus
    {
        public const string Pending = "Pending";
        public const string Validated = "Validated";
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// Constants for remittance type values
    /// </summary>
    public static class RemittanceType
    {
        public const string Fee = "Fee";
        public const string Fine = "Fine";
    }

    /// <summary>
    /// Constants for fee/fine remittance status on individual records
    /// </summary>
    public static class FeeRemittanceStatus
    {
        public const string NotRemitted = "NotRemitted";
        public const string PendingRemittance = "PendingRemittance";
        public const string Remitted = "Remitted";
    }
}
