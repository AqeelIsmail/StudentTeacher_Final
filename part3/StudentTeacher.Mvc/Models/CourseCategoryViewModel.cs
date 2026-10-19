using System.ComponentModel.DataAnnotations;

namespace StudentTeacher.Mvc.Models;

// Matches the values accepted and returned by the Categories API.
public class CourseCategoryViewModel
{
    public int CourseCategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
