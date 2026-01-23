// ============================================================
// FILE: Models/PaymentTransaction.cs
// PURPOSE: Payment transaction history and audit trail
// ============================================================
// This model tracks all payment confirmations made by treasurers.
// It provides a complete audit trail of who marked fees as paid,
// when, and through what payment method.
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models
{
    public partial class PaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }

        /// <summary>
        /// Foreign key to the Fee record
        /// </summary>
        [Required]
        public int FeeId { get; set; }

        /// <summary>
        /// Student who made the payment
        /// </summary>
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;

        /// <summary>
        /// Amount paid
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Date when payment was recorded
        /// </summary>
        [Required]
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Payment method used (Cash, GCash, Bank Transfer, etc.)
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Student number of the treasurer who confirmed the payment
        /// </summary>
        [Required]
        [StringLength(450)]
        public string ProcessedBy { get; set; } = null!;

        /// <summary>
        /// Reference number or transaction ID (optional)
        /// </summary>
        [StringLength(100)]
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Additional notes about the transaction
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Academic year when payment was made
        /// </summary>
        [StringLength(20)]
        public string? AcademicYear { get; set; }

        // Navigation properties
        [ForeignKey("FeeId")]
        public virtual Fee? Fee { get; set; }

        [ForeignKey("StudentNum")]
        public virtual Student? Student { get; set; }

        [ForeignKey("ProcessedBy")]
        public virtual Student? Treasurer { get; set; }
    }

    // Additional model for fine payment transactions
    public partial class FinePaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FineTransactionId { get; set; }

        /// <summary>
        /// Foreign key to the Fine record
        /// </summary>
        [Required]
        public int FineId { get; set; }

        /// <summary>
        /// Student who paid the fine
        /// </summary>
        [Required]
        [StringLength(450)]
        public string StudentNum { get; set; } = null!;

        /// <summary>
        /// Amount paid
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Date when fine payment was recorded
        /// </summary>
        [Required]
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Payment method used
        /// </summary>
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Officer who confirmed the payment
        /// </summary>
        [Required]
        [StringLength(450)]
        public string ProcessedBy { get; set; } = null!;

        /// <summary>
        /// Reference number
        /// </summary>
        [StringLength(100)]
        public string? TransactionReference { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("FineId")]
        public virtual Fine? Fine { get; set; }

        [ForeignKey("StudentNum")]
        public virtual Student? Student { get; set; }

        [ForeignKey("ProcessedBy")]
        public virtual Student? Treasurer { get; set; }
    }
}
