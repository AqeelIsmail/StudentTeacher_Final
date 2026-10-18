
using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentId { get; set; }//pks

        //fks
        public int StudentProfileId { get; set; }
        public int CourseId { get; set; }

        //Enrolment info
        public DateTime EnrolledAt { get; set; }
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        //Navigation properties
        public StudentProfile StudentProfile { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public CourseResult? CourseResult { get; set; }
    }
}
