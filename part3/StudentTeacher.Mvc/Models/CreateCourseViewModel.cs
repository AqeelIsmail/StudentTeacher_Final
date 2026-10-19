
using System.ComponentModel.DataAnnotations;

namespace StudentTeacher.Mvc.Models
{
    public class CreateCourseViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
        public int CourseCategoryId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Optional course banner uploaded via the Part 2 API.
        public IFormFile? Image { get; set; }

        // Used to populate the category dropdown.
        public List<CourseCategoryOption> Categories { get; set; } = new();
    }

    public class CourseCategoryOption
    {
        public int CourseCategoryId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
