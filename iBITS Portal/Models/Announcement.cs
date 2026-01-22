using System.ComponentModel.DataAnnotations;

namespace iBITS_Portal.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty; // Initialize to non-null

        [Required]
        public string Content { get; set; } = string.Empty; // Initialize to non-null

        [Required]
        public string PostedBy { get; set; } = string.Empty; // Initialize to non-null

        public DateTime Timestamp { get; set; }
    }
}