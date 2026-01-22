// ============================================================
// FILE PATH: Models/PendingRoleChange.cs
// ============================================================
// NEW FILE: Model for tracking pending role changes that require
// student confirmation before being applied.
// ============================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models
{
    /// <summary>
    /// Represents a pending role change that awaits student confirmation.
    /// When an admin assigns a new role to a student, the change is stored here
    /// and only applied after the student confirms it upon their next login.
    /// </summary>
    public class PendingRoleChange
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The student number of the student whose role is being changed.
        /// </summary>
        [Required]
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// The student's previous/current role before the change.
        /// </summary>
        [Required]
        public string OldRole { get; set; } = string.Empty;

        /// <summary>
        /// The new role to be assigned after confirmation.
        /// </summary>
        [Required]
        public string NewRole { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the admin who initiated this role change.
        /// </summary>
        [Required]
        public string AssignedByAdminId { get; set; } = string.Empty;

        /// <summary>
        /// The username of the admin who initiated this role change (for display purposes).
        /// </summary>
        public string? AssignedByAdminName { get; set; }

        /// <summary>
        /// The date and time when the role change was assigned by the admin.
        /// </summary>
        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Indicates whether the student has confirmed/accepted the role change.
        /// </summary>
        public bool IsConfirmed { get; set; } = false;

        /// <summary>
        /// The date and time when the student confirmed the role change.
        /// Null if not yet confirmed.
        /// </summary>
        public DateTime? ConfirmedDate { get; set; }

        /// <summary>
        /// Indicates whether the student declined the role change.
        /// </summary>
        public bool IsDeclined { get; set; } = false;

        /// <summary>
        /// The date and time when the student declined the role change.
        /// Null if not declined.
        /// </summary>
        public DateTime? DeclinedDate { get; set; }
    }
}
