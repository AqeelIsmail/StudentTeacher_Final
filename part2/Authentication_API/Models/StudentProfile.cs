using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class StudentProfile
    {
        [Key]
        public int StudentProfileId { get; set; } //PK
   
        public int UserId { get; set; } //FK
        [Required]
        [MaxLength(30)]
        public string StudentNumber { get; set; } = null!;
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
