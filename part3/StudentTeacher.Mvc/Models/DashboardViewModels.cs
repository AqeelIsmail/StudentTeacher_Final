namespace StudentTeacher.Mvc.Models;

// All dashboard information is obtained from the Part 2 API.
public class TeacherDashboardViewModel
{
    public List<TeacherCourseSummaryViewModel> Courses { get; set; } = new();
    public int TotalEnrolments => Courses.Sum(c => c.EnrolmentCount);
}

public class TeacherCourseSummaryViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
}

public class CourseEnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int CourseId { get; set; }
    public int StudentProfileId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TeacherCourseEnrolmentsViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public List<CourseEnrolmentViewModel> Enrolments { get; set; } = new();
}

public class StudentEnrolmentViewModel
{
    public int EnrolmentId { get; set; }
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class StudentDashboardViewModel
{
    public List<StudentEnrolmentViewModel> Enrolments { get; set; } = new();
}
