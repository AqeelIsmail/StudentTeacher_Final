

using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class CourseCategory
    {
        public int CourseCategoryId { get; set; }//PK

        //Category information
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; }

        //Navigation property
        public ICollection<Course> Courses { get; set; } = new List<Course>(); //one-to-many relationship between course categories and courses.
    }
}
