using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Net;
using System.Net.Http.Json;

namespace StudentTeacher.Mvc.Controllers;

// The MVC frontend consumes only the Part 2 API for result operations.
// The API enforces role and course ownership for every protected request.
public class ResultsController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public ResultsController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // Teacher views the enrolled Students and recorded marks for their own course.
    [HttpGet]
    public async Task<IActionResult> Course(int id)
    {
        if (id <= 0) return BadRequest("Invalid course ID.");
        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie)) return RedirectToAction("Login", "Account");

        var client = CreateApiClient();
        using var resultsResponse = await SendWithCookie(client, HttpMethod.Get,
            $"api/courses/{id}/results", cookie);
        if (resultsResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (resultsResponse.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the Teacher who owns this course can view its results.");
        if (resultsResponse.StatusCode == HttpStatusCode.NotFound)
            return NotFound("Course not found.");
        if (!resultsResponse.IsSuccessStatusCode)
            return StatusCode((int)resultsResponse.StatusCode, "Unable to load course results.");

        var results = await resultsResponse.Content
            .ReadFromJsonAsync<List<TeacherResultViewModel>>() ?? new();

        using var enrolmentsResponse = await SendWithCookie(client, HttpMethod.Get,
            $"api/courses/{id}/enrolments", cookie);
        if (enrolmentsResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (enrolmentsResponse.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "You do not own this course.");
        if (!enrolmentsResponse.IsSuccessStatusCode)
            return StatusCode((int)enrolmentsResponse.StatusCode, "Unable to load course enrolments.");

        var model = new TeacherCourseResultsViewModel
        {
            CourseId = id,
            Results = results,
            Enrolments = await enrolmentsResponse.Content
                .ReadFromJsonAsync<List<CourseEnrolmentViewModel>>() ?? new()
        };

        // The course's display details are public, but results remain protected.
        using var courseResponse = await client.GetAsync($"api/Courses/{id}");
        if (courseResponse.IsSuccessStatusCode)
        {
            var course = await courseResponse.Content.ReadFromJsonAsync<CourseViewModel>();
            if (course != null)
            {
                model.CourseCode = course.Code;
                model.CourseTitle = course.Title;
            }
        }

        return View(model);
    }

    // POST is used for an MVC form; the API operation is POST /api/enrolments/{id}/result.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int courseId, int enrolmentId, decimal? finalMark)
    {
        if (courseId <= 0 || enrolmentId <= 0)
            return BadRequest("Invalid course or enrolment ID.");
        if (finalMark is null || finalMark < 0 || finalMark > 100)
        {
            TempData["Error"] = "Enter a final mark from 0 to 100.";
            return RedirectToAction(nameof(Course), new { id = courseId });
        }

        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie)) return RedirectToAction("Login", "Account");
        var client = CreateApiClient();

        // Confirm that the enrolment belongs to the chosen course (API checks ownership).
        using var enrolmentsResponse = await SendWithCookie(client, HttpMethod.Get,
            $"api/courses/{courseId}/enrolments", cookie);
        if (enrolmentsResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (enrolmentsResponse.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the owning Teacher can record results.");
        if (!enrolmentsResponse.IsSuccessStatusCode)
            return StatusCode((int)enrolmentsResponse.StatusCode, "Unable to verify enrolment.");
        var enrolments = await enrolmentsResponse.Content
            .ReadFromJsonAsync<List<CourseEnrolmentViewModel>>() ?? new();
        if (!enrolments.Any(e => e.EnrolmentId == enrolmentId && e.CourseId == courseId))
            return BadRequest("This enrolment does not belong to the selected course.");

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"api/enrolments/{enrolmentId}/result")
        {
            Content = JsonContent.Create(new { FinalMark = finalMark.Value })
        };
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the owning Teacher can record results.");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Result recorded. The Student can now view their result.";
        else if (response.StatusCode == HttpStatusCode.Conflict)
            TempData["Error"] = "A result already exists for this enrolment. Use Update Mark instead.";
        else if (response.StatusCode == HttpStatusCode.BadRequest)
            TempData["Error"] = "The API rejected the mark. The enrolment must be Confirmed and the mark between 0 and 100.";
        else if (response.StatusCode == HttpStatusCode.NotFound)
            TempData["Error"] = "Enrolment not found.";
        else
            TempData["Error"] = "Unable to record the result.";

        return RedirectToAction(nameof(Course), new { id = courseId });
    }

    // Teacher corrects an existing mark. API recalculates Pass/Fail.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int courseId, int resultId, decimal? finalMark)
    {
        if (courseId <= 0 || resultId <= 0)
            return BadRequest("Invalid course or result ID.");
        if (finalMark is null || finalMark < 0 || finalMark > 100)
        {
            TempData["Error"] = "Enter a final mark from 0 to 100.";
            return RedirectToAction(nameof(Course), new { id = courseId });
        }
        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie)) return RedirectToAction("Login", "Account");
        var client = CreateApiClient();

        // Check the result belongs to the specified course, not just a course this Teacher owns.
        using var resultsResponse = await SendWithCookie(client, HttpMethod.Get,
            $"api/courses/{courseId}/results", cookie);
        if (resultsResponse.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (resultsResponse.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the owning Teacher can update results.");
        if (!resultsResponse.IsSuccessStatusCode)
            return StatusCode((int)resultsResponse.StatusCode, "Unable to verify course result.");
        var results = await resultsResponse.Content
            .ReadFromJsonAsync<List<TeacherResultViewModel>>() ?? new();
        if (!results.Any(r => r.CourseResultId == resultId))
            return BadRequest("This result does not belong to the selected course.");

        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"api/results/{resultId}")
        {
            Content = JsonContent.Create(new { FinalMark = finalMark.Value })
        };
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only the owning Teacher can update results.");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Result updated successfully.";
        else if (response.StatusCode == HttpStatusCode.BadRequest)
            TempData["Error"] = "Final mark must be between 0 and 100.";
        else if (response.StatusCode == HttpStatusCode.NotFound)
            TempData["Error"] = "Result not found.";
        else
            TempData["Error"] = "Unable to update the result.";

        return RedirectToAction(nameof(Course), new { id = courseId });
    }

    // Student views only their results, filtered by the API's authenticated identity.
    [HttpGet]
    public async Task<IActionResult> My()
    {
        string? cookie = HttpContext.Session.GetString("ApiAuthCookie");
        if (string.IsNullOrWhiteSpace(cookie)) return RedirectToAction("Login", "Account");
        var client = CreateApiClient();
        using var response = await SendWithCookie(client, HttpMethod.Get,
            "api/results/my", cookie);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only Students can view their own results.");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load your results.");

        var results = await response.Content
            .ReadFromJsonAsync<List<StudentResultViewModel>>() ?? new();
        return View(results);
    }

    private HttpClient CreateApiClient()
    {
        var client = _clientFactory.CreateClient("StudentTeacherApi");
        client.BaseAddress = new Uri("https://localhost:7261/");
        return client;
    }

    private static async Task<HttpResponseMessage> SendWithCookie(HttpClient client,
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
