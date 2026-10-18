using System.ComponentModel.DataAnnotations;

namespace Authentication_API.DTOs
{
    public class CourseResultDto
    {
        [Range(typeof(decimal), "0", "100")]
        public decimal FinalMark { get; set; }
    }
}
