using System.ComponentModel.DataAnnotations;
using Authentication_API.Models;
namespace Authentication_API
{
    public class User
    {
        [Key]
        public int UserId { get; set; } //PK
        //account information
        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = null!; 
        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; } = null!;
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = null!;// "Student" or "teacher"

        //navigation properties
        //--> allow EF Core to navigate from a User object to its related profile.
        //The ? indicates that the relationship may be absent.
        //These are not additional columns in the Users table.
        public StudentProfile? StudentProfile { get; set; }
        public TeacherProfile? TeacherProfile { get; set; }
    }
}
