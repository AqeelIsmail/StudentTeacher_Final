
using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class RegisterDto
    {
        //This class defines the information the registration process expects.
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(30)]
        public string? StudentNumber { get; set; }

        [MaxLength(30)]
        public string? EmployeeNumber { get; set; }
    }
}
