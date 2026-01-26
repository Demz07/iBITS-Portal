// ============================================================
// FILE: Models/RemittanceItem.cs
// PURPOSE: Individual payment records within a remittance batch
// ============================================================
// This model represents a single student payment that is part of
// a remittance batch. Links to either a Fee or Fine record.
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    /// <summary>
    /// Represents an individual payment item within a remittance batch.
    /// Each item corresponds to one student's payment for a specific fee/fine.
    /// </summary>
    public class RemittanceItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RemittanceItemId { get; set; }

        /// <summary>
        /// Foreign key to the parent Remittance batch
        /// </summary>
        [Required]
        public int RemittanceId { get; set; }

        /// <summary>
        /// Foreign key to Fees table (if this is a fee payment)
        /// </summary>
        public int? FeeId { get; set; }

        /// <summary>
        /// Foreign key to Fines table (if this is a fine payment)
        /// </summary>
        public int? FineId { get; set; }

        /// <summary>
        /// Student who made the payment
        /// </summary>
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = string.Empty;

        /// <summary>
        /// Cached student name for reporting purposes
        /// </summary>
        [StringLength(300)]
        public string? StudentName { get; set; }

        /// <summary>
        /// Amount paid by the student
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Date when the Class Treasurer collected/marked this payment
        /// </summary>
        [Required]
        public DateTime CollectionDate { get; set; }

        /// <summary>
        /// Payment method: Cash, GCash, Bank Transfer, etc.
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Transaction reference number (for electronic payments)
        /// </summary>
        [StringLength(100)]
        public string? TransactionRef { get; set; }

        /// <summary>
        /// Additional notes about this payment
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        // ============================================================
        // Navigation Properties
        // ============================================================

        /// <summary>
        /// The parent remittance batch
        /// </summary>
        [ForeignKey("RemittanceId")]
        public virtual Remittance? Remittance { get; set; }

        /// <summary>
        /// The associated fee record (if fee payment)
        /// </summary>
        [ForeignKey("FeeId")]
        public virtual Fee? Fee { get; set; }

        /// <summary>
        /// The associated fine record (if fine payment)
        /// </summary>
        [ForeignKey("FineId")]
        public virtual Fine? Fine { get; set; }

        /// <summary>
        /// The student who made the payment
        /// </summary>
        [ForeignKey("StudentNum")]
        public virtual Student? Student { get; set; }

        // ============================================================
        // Computed Properties
        // ============================================================

        /// <summary>
        /// Indicates if this is a fee payment
        /// </summary>
        [NotMapped]
        public bool IsFeePayment => FeeId.HasValue;

        /// <summary>
        /// Indicates if this is a fine payment
        /// </summary>
        [NotMapped]
        public bool IsFinePayment => FineId.HasValue;

        /// <summary>
        /// Gets the payment type as a string
        /// </summary>
        [NotMapped]
        public string PaymentType => IsFeePayment ? "Fee" : "Fine";
    }
}
