
namespace StudentTeacher.Mvc.Models
{
    public class CourseViewModel
    {
        public int CourseId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int CourseCategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int TeacherProfileId { get; set; }

        public string TeacherFirstName { get; set; } = string.Empty;

        public string TeacherLastName { get; set; } = string.Empty;

        public string? CourseImageUrl { get; set; }

        // Set by MVC after checking the API for course ownership.
        public bool CanManageCourse { get; set; }

        public bool IsStudent { get; set; }
        public StudentEnrolmentViewModel? StudentEnrolment { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
