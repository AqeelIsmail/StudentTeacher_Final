using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class TeacherProfile
    {
        [Key]
        public int TeacherProfileId { get; set; }

        public int UserId { get; set; }
        [Required]
        [MaxLength(30)]
        public string EmployeeNumber { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;
        [MaxLength(2048)]
        public string? ProfileImageUrl { get; set; }

        //navigation property
        public User User { get; set; } = null!;
    }
}
