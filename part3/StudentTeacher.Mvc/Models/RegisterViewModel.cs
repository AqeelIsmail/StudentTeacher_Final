using System.ComponentModel.DataAnnotations;

namespace StudentTeacher.Mvc.Models
{
    public class RegisterViewModel
    {
        [Required] //mvc must contain a value
        [EmailAddress] //validates the format of an email address.
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";

        [Required]
        [MaxLength(100)] //limits the length of a value.
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "Student Number")]
        public string? StudentNumber { get; set; } //means the property can be null.
                                                   //This is important because a Student needs a student number,
                                                   //whereas a Teacher needs an employee number.

        [MaxLength(30)]
        [Display(Name = "Employee Number")]

        public string? EmployeeNumber { get; set; }
    }
}