
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

            // Check course ownership through the API; never trust a posted teacher ID.
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (!string.IsNullOrEmpty(authCookie))
            {
                using var ownerRequest = new HttpRequestMessage(
                    HttpMethod.Get, "api/Courses/my");
                ownerRequest.Headers.TryAddWithoutValidation("Cookie", authCookie);
                using var ownerResponse = await client.SendAsync(ownerRequest);
                if (ownerResponse.IsSuccessStatusCode)
                {
                    var myCourses = await ownerResponse.Content
                        .ReadFromJsonAsync<List<CourseViewModel>>();
                    course.CanManageCourse =
                        myCourses?.Any(c => c.CourseId == id) == true;
                }
                else if (ownerResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    HttpContext.Session.Remove("ApiAuthCookie");
                }
            }

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
            {
                // POST /api/Courses returns the created CourseId.
                var createdCourse = await response.Content
                    .ReadFromJsonAsync<CourseViewModel>();
                if (createdCourse == null || createdCourse.CourseId <= 0)
                    return StatusCode(502, "The API created a course but did not return its ID.");

                if (model.Image is { Length: > 0 })
                {
                    var uploaded = await SendCourseImage(client, authCookie,
                        createdCourse.CourseId, model.Image);
                    if (!uploaded)
                        TempData["Error"] =
                            "Course created, but the image upload failed. You can retry from Edit Course.";
                }

                return RedirectToAction(nameof(Details), new { id = createdCourse.CourseId });
            }

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

        // Teachers can edit their own course details and optionally replace its banner.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (string.IsNullOrEmpty(authCookie))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            using var ownershipRequest = new HttpRequestMessage(
                HttpMethod.Get, "api/Courses/my");
            ownershipRequest.Headers.TryAddWithoutValidation("Cookie", authCookie);
            using var ownerResponse = await client.SendAsync(ownershipRequest);
            if (ownerResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }
            if (ownerResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
                return Forbid();
            if (!ownerResponse.IsSuccessStatusCode)
                return StatusCode((int)ownerResponse.StatusCode);

            var myCourses = await ownerResponse.Content
                .ReadFromJsonAsync<List<CourseViewModel>>();
            if (myCourses?.Any(c => c.CourseId == id) != true)
                return Forbid();

            using var response = await client.GetAsync($"api/Courses/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return NotFound();
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var course = await response.Content.ReadFromJsonAsync<CourseViewModel>();
            if (course == null) return NotFound();

            var model = new CreateCourseViewModel
            {
                CourseCategoryId = course.CourseCategoryId,
                Code = course.Code,
                Title = course.Title,
                Description = course.Description
            };
            await LoadCategories(client, model);
            ViewBag.CourseId = id;
            ViewBag.CurrentImageUrl = course.CourseImageUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateCourseViewModel model)
        {
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (string.IsNullOrEmpty(authCookie))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            if (!ModelState.IsValid)
            {
                await LoadCategories(client, model);
                ViewBag.CourseId = id;
                return View(model);
            }

            var courseData = new
            {
                model.CourseCategoryId, model.Code, model.Title, model.Description
            };
            using var request = new HttpRequestMessage(
                HttpMethod.Put, $"api/Courses/{id}");
            request.Headers.TryAddWithoutValidation("Cookie", authCookie);
            request.Content = JsonContent.Create(courseData);
            using var response = await client.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Forbidden => "You cannot edit this course.",
                    System.Net.HttpStatusCode.Conflict => "Another course already uses this code.",
                    System.Net.HttpStatusCode.NotFound => "Course not found.",
                    _ => "The course could not be updated."
                });
                await LoadCategories(client, model);
                ViewBag.CourseId = id;
                return View(model);
            }

            if (model.Image is { Length: > 0 })
            {
                if (!await SendCourseImage(client, authCookie, id, model.Image))
                    TempData["Error"] =
                        "Course details updated, but the replacement image upload failed.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Course deletion is a required Teacher management operation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (string.IsNullOrEmpty(authCookie))
                return RedirectToAction("Login", "Account");

            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");
            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"api/Courses/{id}");
            request.Headers.TryAddWithoutValidation("Cookie", authCookie);
            using var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HttpContext.Session.Remove("ApiAuthCookie");
                return RedirectToAction("Login", "Account");
            }

            TempData["Error"] = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Conflict =>
                    "Cannot delete a course that has enrolments.",
                System.Net.HttpStatusCode.Forbidden =>
                    "You cannot delete this course.",
                _ => "The course could not be deleted."
            };
            return RedirectToAction(nameof(Details), new { id });
        }

        // MVC forwards the file to the API, never directly to Azure Storage.
        private static async Task<bool> SendCourseImage(
            HttpClient client, string authCookie, int courseId, IFormFile image)
        {
            if (image.Length == 0 || image.Length > 5 * 1024 * 1024)
                return false;

            using var form = new MultipartFormDataContent();
            using var file = new StreamContent(image.OpenReadStream());
            file.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
            form.Add(file, "image", Path.GetFileName(image.FileName));

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"api/Courses/{courseId}/image");
            request.Headers.TryAddWithoutValidation("Cookie", authCookie);
            request.Content = form;
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
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
