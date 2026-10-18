using System.ComponentModel.DataAnnotations;

namespace Authentication_API.DTOs
{
    public class EnrolmentStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}
