
using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
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
            // Create the named HttpClient configured in Program.cs.
            var client = _clientFactory.CreateClient("StudentTeacherApi");

            client.BaseAddress = new Uri("https://localhost:7261/");

            // Send a GET request to the public API endpoint.
            using var response = await client.GetAsync("api/Courses");

            // Check whether the API request succeeded.
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)response.StatusCode,
                    "Unable to retrieve courses from the API.");
            }

            // Convert the JSON response into a list of C# objects.
            var courses = await response.Content
                .ReadFromJsonAsync<List<CourseViewModel>>();

            // Pass the list to the Razor view.
            return View(courses ?? new List<CourseViewModel>());
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Create the named HttpClient.
            var client = _clientFactory.CreateClient("StudentTeacherApi");

            client.BaseAddress = new Uri("https://localhost:7261/");

            // Retrieve a particular course using its ID.
            using var response = await client.GetAsync($"api/Courses/{id}");

            // The course does not exist.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Course not found.");
            }

            // Handle other unsuccessful API responses.
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(
                    (int)response.StatusCode,
                    "Unable to retrieve the course.");
            }

            // Convert the JSON response to a CourseViewModel.
            var course = await response.Content
                .ReadFromJsonAsync<CourseViewModel>();

            if (course == null)
            {
                return NotFound("Course not found.");
            }

            // Send the course to the Details view.
            return View(course);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");

            if (string.IsNullOrEmpty(authCookie))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            // Verify that the logged-in user is a Teacher.
            using var check = new HttpRequestMessage(
                HttpMethod.Get, "api/Courses/my");

            check.Headers.TryAddWithoutValidation("Cookie", authCookie);

            using var checkResponse = await client.SendAsync(check);

            if (checkResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }

            if (checkResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                return StatusCode(403, "Only Teachers can create courses.");

            if (!checkResponse.IsSuccessStatusCode)
                return StatusCode((int)checkResponse.StatusCode);

            var model = new CreateCourseViewModel();

            await LoadCategories(client, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");

            if (string.IsNullOrEmpty(authCookie))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            if (!ModelState.IsValid)
            {
                await LoadCategories(client, model);
                return View(model);
            }

            // Only the fields required by the API are sent.
            var courseData = new
            {
                model.CourseCategoryId,
                model.Code,
                model.Title,
                model.Description
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post, "api/Courses");

            request.Headers.TryAddWithoutValidation("Cookie", authCookie);
            request.Content = JsonContent.Create(courseData);

            using var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                ModelState.AddModelError("", "Only Teachers can create courses.");
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                ModelState.AddModelError("", "A course with this code already exists.");
            else
                ModelState.AddModelError("", "Unable to create the course.");

            await LoadCategories(client, model);
            return View(model);
        }

        private static async Task LoadCategories(
            HttpClient client, CreateCourseViewModel model)
        {
            // Categories come from the API, not directly from SQL Server.
            model.Categories =
                await client.GetFromJsonAsync<List<CourseCategoryOption>>(
                    "api/Categories") ?? new List<CourseCategoryOption>();
        }
    }
}
