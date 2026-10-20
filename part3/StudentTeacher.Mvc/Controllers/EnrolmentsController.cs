using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Net;
using System.Net.Http.Json;

namespace StudentTeacher.Mvc.Controllers;

public class EnrolmentsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;
    public EnrolmentsController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // Display only enrolments returned for the authenticated Student.
    [HttpGet]
    public async Task<IActionResult> My()
    {
        var cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/enrolments/my");
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Remove("ApiAuthCookie");
            HttpContext.Session.Remove("ApiRole");
            return RedirectToAction("Login", "Account");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only Students can view their enrolments.");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load enrolments.");

        var enrolments = await response.Content
            .ReadFromJsonAsync<List<StudentEnrolmentViewModel>>() ?? new();
        return View(enrolments);
    }

    // MVC posts to the Part 2 API; it never writes to SQL Server itself.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int courseId)
    {
        if (courseId <= 0) return BadRequest("Invalid course ID.");
        var cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/courses/{courseId}/enrol");
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Remove("ApiAuthCookie");
            HttpContext.Session.Remove("ApiRole");
            return RedirectToAction("Login", "Account");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only Students can enrol in courses.");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "You enrolled successfully. Status: Pending.";
        else if (response.StatusCode == HttpStatusCode.Conflict)
            TempData["Error"] = "You are already enrolled in this course.";
        else if (response.StatusCode == HttpStatusCode.NotFound)
            TempData["Error"] = "The selected course was not found.";
        else
            TempData["Error"] = "Enrolment failed. Please try again.";

        return RedirectToAction("Details", "Courses", new { id = courseId });
    }


    // Teacher-only course enrolment list. The API verifies both role and ownership.
    [HttpGet]
    public async Task<IActionResult> Course(int id)
    {
        if (id <= 0) return BadRequest("Invalid course ID.");

        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/courses/{id}/enrolments");
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Remove("ApiAuthCookie");
            HttpContext.Session.Remove("ApiRole");
            return RedirectToAction("Login", "Account");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Teacher access required and the course must belong to you.");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return NotFound("Course not found.");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load course enrolments.");

        var enrolments = await response.Content
            .ReadFromJsonAsync<List<CourseEnrolmentViewModel>>() ?? new();

        var model = new TeacherCourseEnrolmentsViewModel
        {
            CourseId = id,
            Enrolments = enrolments
        };

        // Course details are read from the API, never from SQL Server in MVC.
        using var courseResponse = await client.GetAsync($"api/Courses/{id}");
        if (courseResponse.IsSuccessStatusCode)
        {
            var course = await courseResponse.Content
                .ReadFromJsonAsync<CourseViewModel>();
            if (course != null)
            {
                model.CourseCode = course.Code;
                model.CourseTitle = course.Title;
            }
        }

        return View(model);
    }

    // MVC form submits POST; MVC forwards PUT to the existing Part 2 API.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int courseId, int enrolmentId,
        string status)
    {
        if (courseId <= 0 || enrolmentId <= 0)
            return BadRequest("Invalid course or enrolment ID.");

        if (status != "Pending" && status != "Confirmed")
            return BadRequest("Status must be Pending or Confirmed.");

        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"api/enrolments/{enrolmentId}/status")
        {
            Content = JsonContent.Create(new { Status = status })
        };
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            HttpContext.Session.Remove("ApiAuthCookie");
            HttpContext.Session.Remove("ApiRole");
            return RedirectToAction("Login", "Account");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the owning Teacher can update this enrolment.");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = $"Enrolment status changed to {status}.";
        else if (response.StatusCode == HttpStatusCode.NotFound)
            TempData["Error"] = "Enrolment not found.";
        else if (response.StatusCode == HttpStatusCode.BadRequest)
            TempData["Error"] = "The API rejected that status change.";
        else
            TempData["Error"] = "The enrolment status could not be updated.";

        return RedirectToAction(nameof(Course), new { id = courseId });
    }

}
