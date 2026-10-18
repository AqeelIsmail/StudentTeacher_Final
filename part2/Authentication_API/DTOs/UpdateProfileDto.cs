using System.ComponentModel.DataAnnotations;

namespace Authentication_API.DTOs
{
    // Request body for PUT /api/auth/profile. 
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;
    }
}
