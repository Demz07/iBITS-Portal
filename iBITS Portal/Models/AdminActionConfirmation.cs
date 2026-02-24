using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models
{
    public class AdminActionConfirmation
    {
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirmation password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = null!;

        public string Action { get; set; } = null!;
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
    }

    public class PasswordChangeModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "New password and confirmation do not match")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }

        [EmailAddress]
        [Display(Name = "Admin Email")]
        public string? Email { get; set; }
    }
}