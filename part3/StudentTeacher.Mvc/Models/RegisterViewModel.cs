namespace StudentTeacher.Mvc.Models
{
    public class RegisterViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Student" or "Teacher"
    }
}
