using System.ComponentModel.DataAnnotations;

namespace Authentication_API.DTOs
{
    public class CourseDto
    {
        [Range(1, int.MaxValue)]
        public int CourseCategoryId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
