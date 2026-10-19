namespace StudentTeacher.Mvc.Models;

// Represents GET /api/auth/profile, including the nested profile object.
public class StudentProfileViewModel
{
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public StudentProfileDetailsViewModel Profile { get; set; } = new();
}

public class StudentProfileDetailsViewModel
{
    public string StudentNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
}
