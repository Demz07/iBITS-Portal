// ============================================================
// FILE: Models/Fine.cs
// PURPOSE: Fine records for students with remittance tracking
// ============================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace iBITS_Portal.Models;

public partial class Fine
{
    public int FineId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    // Amount paid so far (for partial payments - DISABLED per business rule)
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; } = 0;

    // Calculated property for remaining balance
    [NotMapped]
    public decimal RemainingBalance => (Amount ?? 0) - AmountPaid;

    // Calculated property for payment status
    [NotMapped]
    public string PaymentStatus
    {
        get
        {
            var status = FinesStatus?.ToLower();
            if (status == "excused" || status == "waived") return "Excused";
            if (AmountPaid <= 0) return "Unpaid";
            if (AmountPaid >= (Amount ?? 0)) return "Paid";
            return "Partial";
        }
    }

    public DateOnly? FinesStartDate { get; set; }

    public DateOnly? FinesDueDate { get; set; }

    public string? FinesStatus { get; set; }

    // Link to Attendance (for Event-based fines)
    public int? AttendanceId { get; set; }

    public virtual Attendance? Attendance { get; set; }

    // Properties for Manual Fines
    public string? Description { get; set; }

    public string? StudentNum { get; set; }

    // Property to group manually created fines together
    public string? BatchId { get; set; }

    // ============================================================
    // REMITTANCE TRACKING FIELDS
    // ============================================================

    /// <summary>
    /// Remittance status: NotRemitted, PendingRemittance, or Remitted
    /// </summary>
    [StringLength(20)]
    public string RemittanceStatus { get; set; } = FeeRemittanceStatus.NotRemitted;

    /// <summary>
    /// Foreign key to Remittance (set when included in a remittance batch)
    /// </summary>
    public int? RemittanceId { get; set; }

    /// <summary>
    /// Class Treasurer who collected/marked this payment (StudentNum)
    /// </summary>
    [StringLength(450)]
    public string? CollectedBy { get; set; }

    /// <summary>
    /// Date when Class Treasurer marked this as paid
    /// </summary>
    public DateTime? CollectionDate { get; set; }

    /// <summary>
    /// THE OFFICIAL PAYMENT DATE - set when Org Treasurer validates the remittance
    /// This is the date that appears on official records
    /// </summary>
    public DateTime? OfficialPaymentDate { get; set; }

    // ============================================================
    // PAYMENT LOCK FIELDS (for Revoke/Lock Feature)
    // ============================================================

    /// <summary>
    /// Indicates if this payment has been locked by Org Treasurer (permanent)
    /// Once locked, payment cannot be revoked or edited
    /// </summary>
    public bool IsPaymentLocked { get; set; } = false;

    /// <summary>
    /// Date when payment was locked
    /// </summary>
    public DateTime? PaymentLockedDate { get; set; }

    /// <summary>
    /// Org Treasurer who locked the payment (StudentNum)
    /// </summary>
    [StringLength(450)]
    public string? LockedBy { get; set; }

    // ============================================================
    // Navigation Properties
    // ============================================================

    /// <summary>
    /// Navigation property to Student
    /// </summary>
    [ForeignKey("StudentNum")]
    public virtual Student? StudentNumNavigation { get; set; }

    /// <summary>
    /// Navigation property to Remittance batch
    /// </summary>
    [ForeignKey("RemittanceId")]
    public virtual Remittance? Remittance { get; set; }

    /// <summary>
    /// Navigation property to the Class Treasurer who collected
    /// </summary>
    [ForeignKey("CollectedBy")]
    public virtual Student? CollectedByNavigation { get; set; }

    /// <summary>
    /// Navigation property to the Org Treasurer who locked the payment
    /// </summary>
    [ForeignKey("LockedBy")]
    public virtual Student? LockedByNavigation { get; set; }

    // ============================================================
    // Computed Properties for Remittance Logic
    // ============================================================

    /// <summary>
    /// Checks if this fine can be edited by Class Treasurer
    /// (Only allowed if not yet remitted)
    /// </summary>
    [NotMapped]
    public bool CanClassTreasurerEdit => RemittanceStatus == FeeRemittanceStatus.NotRemitted;

    /// <summary>
    /// Checks if this fine is locked (manually locked OR remitted)
    /// </summary>
    [NotMapped]
    public bool IsLocked => IsPaymentLocked || RemittanceStatus != FeeRemittanceStatus.NotRemitted;

    /// <summary>
    /// Checks if this fine has been officially validated
    /// </summary>
    [NotMapped]
    public bool IsOfficiallyPaid => RemittanceStatus == FeeRemittanceStatus.Remitted && OfficialPaymentDate.HasValue;

    /// <summary>
    /// Checks if Org Treasurer can edit this fine
    /// (Only allowed for unpaid AND not remitted AND not locked)
    /// </summary>
    [NotMapped]
    public bool CanOrgTreasurerEdit => FinesStatus?.ToUpper() != "PAID" 
                                        && RemittanceStatus != FeeRemittanceStatus.Remitted
                                        && !IsPaymentLocked;

    /// <summary>
    /// Checks if Org Treasurer can revoke this payment
    /// Rules (Option B - Grace Period):
    /// - Payment must be Paid
    /// - Payment must NOT be locked
    /// - Payment must be Remitted (only Org Treasurer direct payments)
    /// - Payment must NOT be part of a remittance batch (RemittanceId = NULL)
    /// This allows Org Treasurer to revoke their OWN payments before locking (grace period for mistakes)
    /// </summary>
    [NotMapped]
    public bool CanOrgTreasurerRevoke => FinesStatus?.ToUpper() == "PAID" 
                                          && !IsPaymentLocked 
                                          && RemittanceStatus == FeeRemittanceStatus.Remitted
                                          && RemittanceId == null; // Only direct Org Treasurer payments

    /// <summary>
    /// Checks if Org Treasurer can lock this payment
    /// Rules (Option B - Grace Period):
    /// - Payment must be Paid
    /// - Payment must be Remitted (validated by Org Treasurer)
    /// - Payment must NOT already be locked
    /// - Payment must NOT be part of a remittance batch (RemittanceId = NULL)
    /// This allows Org Treasurer to manually lock their direct payments for final verification
    /// </summary>
    [NotMapped]
    public bool CanOrgTreasurerLock => FinesStatus?.ToUpper() == "PAID" 
                                        && !IsPaymentLocked 
                                        && RemittanceStatus == FeeRemittanceStatus.Remitted
                                        && RemittanceId == null; // Only direct Org Treasurer payments

    /// <summary>
    /// Checks if this fine is in a submitted remittance batch waiting for validation
    /// Shows "Remitted waiting for validation" status in Org Treasurer view
    /// </summary>
    [NotMapped]
    public bool IsAwaitingValidation => RemittanceId.HasValue 
                                         && Remittance != null 
                                         && Remittance.Status == Models.RemittanceStatus.Pending;

    /// <summary>
    /// Checks if this fine should be visible in Org Treasurer views
    /// Only show if:
    /// - Part of a submitted remittance batch (pending or validated)
    /// - OR marked as paid by Org Treasurer directly (RemittanceId = NULL and Remitted)
    /// Do NOT show Class Treasurer payments without a batch
    /// </summary>
    [NotMapped]
    public bool ShouldShowInOrgView => (RemittanceId.HasValue && Remittance != null && Remittance.Status != Models.RemittanceStatus.Rejected)
                                        || (RemittanceId == null && RemittanceStatus == FeeRemittanceStatus.Remitted);
}
