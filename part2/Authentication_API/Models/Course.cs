
using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }//PK
        //FKs
        [Required]
        public int TeacherProfileId { get; set; }
        [Required]  
        public int CourseCategoryId { get; set; }

        //Course info
        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = null!;
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!;
        [MaxLength(1000)]
        public string? Description { get; set; }
        [MaxLength(2048)]
        public string? CourseImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        //Navigation properties
        public TeacherProfile TeacherProfile { get; set; } = null!; //belongs to one teacher profile.
        public CourseCategory CourseCategory { get; set; } = null!;//belongs to one course category.
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
        //course can have many enrolments, one-to-many relationship between courses and enrolments.
    }
}
