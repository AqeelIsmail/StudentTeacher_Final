using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Net;
using System.Net.Http.Json;

namespace StudentTeacher.Mvc.Controllers;

public class CategoriesController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public CategoriesController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    // All category changes go through the Part 2 API.
    // The API verifies the Teacher role for POST, PUT and DELETE.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        using var response = await client.GetAsync("api/Categories");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load categories.");

        var categories = await response.Content
            .ReadFromJsonAsync<List<CourseCategoryViewModel>>() ?? new();
        return View(categories);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        return View(new CourseCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseCategoryViewModel model)
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        model.Name = model.Name?.Trim() ?? string.Empty;
        if (!ModelState.IsValid) return View(model);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Categories")
        {
            Content = JsonContent.Create(new { model.Name, model.Description })
        };
        request.Headers.TryAddWithoutValidation("Cookie", GetApiCookie());
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Teacher access required.");
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Category created.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response.StatusCode == HttpStatusCode.Conflict
            ? "A category with this name already exists."
            : "Unable to create the category.");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        // The API exposes GET /api/Categories (not GET /api/Categories/{id}).
        using var response = await client.GetAsync("api/Categories");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to load categories.");

        var categories = await response.Content
            .ReadFromJsonAsync<List<CourseCategoryViewModel>>() ?? new();
        var category = categories.FirstOrDefault(c => c.CourseCategoryId == id);
        if (category == null) return NotFound("Category not found.");

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseCategoryViewModel model)
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        if (id <= 0 || id != model.CourseCategoryId)
            return BadRequest("Invalid category ID.");

        model.Name = model.Name?.Trim() ?? string.Empty;
        if (!ModelState.IsValid) return View(model);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/Categories/{id}")
        {
            Content = JsonContent.Create(new { model.Name, model.Description })
        };
        request.Headers.TryAddWithoutValidation("Cookie", GetApiCookie());
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Teacher access required.");
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", response.StatusCode switch
        {
            HttpStatusCode.Conflict => "Another category already uses this name.",
            HttpStatusCode.NotFound => "Category not found.",
            _ => "Unable to update the category."
        });
        return View(model);
    }

    // MVC forms submit POST; the API operation itself is DELETE.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = CreateApiClient();
        var accessResult = await CheckTeacherAccess(client);
        if (accessResult != null) return accessResult;

        if (id <= 0) return BadRequest("Invalid category ID.");

        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"api/Categories/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", GetApiCookie());
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Teacher access required.");

        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.StatusCode switch
            {
                HttpStatusCode.NoContent => "Category deleted.",
                HttpStatusCode.Conflict => "Cannot delete a category used by a course.",
                HttpStatusCode.NotFound => "Category not found.",
                _ => "Unable to delete the category."
            };
        return RedirectToAction(nameof(Index));
    }

    private HttpClient CreateApiClient()
    {
        var client = _clientFactory.CreateClient("StudentTeacherApi");
        ////client.BaseAddress = new Uri("https://localhost:7261/");
        return client;
    }

    private string? GetApiCookie() =>
        HttpContext.Session.GetString("ApiAuthCookie");

    // Verify the role through the API instead of trusting a form value.
    private async Task<IActionResult?> CheckTeacherAccess(HttpClient client)
    {
        string? cookie = GetApiCookie();
        if (string.IsNullOrWhiteSpace(cookie))
            return RedirectToAction("Login", "Account");

        using var request = new HttpRequestMessage(HttpMethod.Get,
            "api/Teacher/teacher-only");
        request.Headers.TryAddWithoutValidation("Cookie", cookie);
        using var response = await client.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ClearSessionAndLogin();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return StatusCode(403, "Only Teachers can manage categories.");
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Unable to verify Teacher access.");

        return null;
    }

    private IActionResult ClearSessionAndLogin()
    {
        HttpContext.Session.Remove("ApiAuthCookie");
        HttpContext.Session.Remove("ApiRole");
        return RedirectToAction("Login", "Account");
    }
}
