using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Net;
using System.Net.Http.Json;

namespace StudentTeacher.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public HomeController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public IActionResult Index()
    {
        // The role is set from GET /api/auth/profile after successful login.
        var role = HttpContext.Session.GetString("ApiRole");
        var cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (!string.IsNullOrWhiteSpace(cookie) && role == "Teacher")
            return RedirectToAction(nameof(TeacherArea));
        if (!string.IsNullOrWhiteSpace(cookie) && role == "Student")
            return RedirectToAction(nameof(StudentArea));
        return RedirectToAction("Index", "Courses");
    }

    public async Task<IActionResult> TeacherArea()
    {
        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");

        // Confirm Teacher role through a protected API endpoint.
        using var roleResponse = await SendWithCookie(client, HttpMethod.Get,
            "api/Teacher/teacher-only", cookie);
        if (roleResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (!roleResponse.IsSuccessStatusCode)
            return StatusCode((int)roleResponse.StatusCode, "Teacher access required.");

        using var coursesResponse = await SendWithCookie(client, HttpMethod.Get,
            "api/Courses/my", cookie);
        if (coursesResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (!coursesResponse.IsSuccessStatusCode)
            return StatusCode((int)coursesResponse.StatusCode, "Unable to load your courses.");

        var courses = await coursesResponse.Content
            .ReadFromJsonAsync<List<CourseViewModel>>() ?? new();
        var model = new TeacherDashboardViewModel();

        foreach (var course in courses)
        {
            // This endpoint checks course ownership inside the API.
            using var enrolmentsResponse = await SendWithCookie(client,
                HttpMethod.Get, $"api/courses/{course.CourseId}/enrolments", cookie);
            if (enrolmentsResponse.StatusCode == HttpStatusCode.Unauthorized)
                return ClearSessionAndLogin();
            if (!enrolmentsResponse.IsSuccessStatusCode)
                return StatusCode((int)enrolmentsResponse.StatusCode,
                    "Unable to load enrolment counts for a course.");

            var enrolments = await enrolmentsResponse.Content
                .ReadFromJsonAsync<List<CourseEnrolmentViewModel>>() ?? new();
            model.Courses.Add(new TeacherCourseSummaryViewModel
            {
                CourseId = course.CourseId,
                Code = course.Code,
                Title = course.Title,
                EnrolmentCount = enrolments.Count,
                ConfirmedCount = enrolments.Count(e => e.Status == "Confirmed")
            });
        }

        return View(model);
    }

    public async Task<IActionResult> StudentArea()
    {
        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");

        using var roleResponse = await SendWithCookie(client, HttpMethod.Get,
            "api/Student/student-only", cookie);
        if (roleResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (!roleResponse.IsSuccessStatusCode)
            return StatusCode((int)roleResponse.StatusCode, "Student access required.");

        using var response = await SendWithCookie(client, HttpMethod.Get,
            "api/enrolments/my", cookie);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load your enrolments.");

        var model = new StudentDashboardViewModel
        {
            Enrolments = await response.Content
                .ReadFromJsonAsync<List<StudentEnrolmentViewModel>>() ?? new()
        };
        return View(model);
    }

    private async Task<HttpResponseMessage> SendWithCookie(HttpClient client,
        HttpMethod method, string path, string cookie)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return await client.SendAsync(request);
    }

    private IActionResult ClearSessionAndLogin()
    {
        HttpContext.Session.Remove("ApiAuthCookie");
        HttpContext.Session.Remove("ApiRole");
        return RedirectToAction("Login", "Account");
    }
}
