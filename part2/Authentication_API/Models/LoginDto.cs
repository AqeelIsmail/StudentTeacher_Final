using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Models
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}