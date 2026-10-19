using System.ComponentModel.DataAnnotations;

namespace StudentTeacher.Mvc.Models;

// The API returns results; MVC never queries SQL Server directly.
public class StudentResultViewModel
{
    public int CourseResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public decimal FinalMark { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class TeacherResultViewModel
{
    public int CourseResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int StudentProfileId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public decimal FinalMark { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}

public class TeacherCourseResultsViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public List<CourseEnrolmentViewModel> Enrolments { get; set; } = new();
    public List<TeacherResultViewModel> Results { get; set; } = new();
}
