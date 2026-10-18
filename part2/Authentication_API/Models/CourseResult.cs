
using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class CourseResult
    {
        public int CourseResultId { get; set; } //PK

        //FK
        public int EnrolmentId { get; set; }

        //result info
        public decimal FinalMark { get; set; }
        [Required]
        [MaxLength(10)]
        public string Outcome { get; set; } = null!;
        public DateTime RecordedAt { get; set; }

        //navigation property
        public Enrolment Enrolment { get; set; } = null!;
    }
}
