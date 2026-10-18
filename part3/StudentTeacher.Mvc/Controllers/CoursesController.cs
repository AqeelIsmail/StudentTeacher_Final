
using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Net;
using System.Net.Http.Json;

namespace StudentTeacher.Mvc.Controllers
{
    public class CoursesController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public CoursesController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Get the API authentication cookie from MVC session.
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");

            if (string.IsNullOrEmpty(authCookie))
            {
                return RedirectToAction("Login", "Account");
            }

            // Create the named HttpClient configured in Program.cs.
            var client = _clientFactory.CreateClient("StudentTeacherApi");

            client.BaseAddress = new Uri("https://localhost:7261/");

            // Prepare the request to the Part 2 API.
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/Courses");

            // Forward the logged-in user's API cookie.
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authCookie);

            // Send the request.
            using var response = await client.SendAsync(request);

            // Handle expired or invalid authentication.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode,
                    "Unable to retrieve courses from the API.");
            }

            // Convert the API's JSON response into C# objects.
            var courses = await response.Content
                .ReadFromJsonAsync<List<CourseViewModel>>();

            // Pass the courses to the Razor view.
            return View(courses ?? new List<CourseViewModel>());
        }
    }
}
