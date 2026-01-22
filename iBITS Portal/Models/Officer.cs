using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // <-- Add this
using System.ComponentModel.DataAnnotations.Schema; // <-- Add this

namespace iBITS_Portal.Models;

public partial class Officer
{
    [Key] // <-- ADD THIS ANNOTATION
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <-- ADD THIS ANNOTATION
    public int OfficerId { get; set; }

    public string? Classification { get; set; }

    public string? Position { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}