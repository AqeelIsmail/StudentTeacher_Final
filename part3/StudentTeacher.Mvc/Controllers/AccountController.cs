using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StudentTeacher.Mvc.Models;
using System.Text;
using System.Text.Json.Serialization;
namespace StudentTeacher.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public AccountController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet]//allows you to see the view 
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]//protects the form against cross-site request forgery.
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        //Check that the form passes MVC validation.
        if (!ModelState.IsValid) //checks the MVC form's validation rules before contacting the API.
        {
            return View(model);
        }

        //Create a client instance 
        var client = _clientFactory.CreateClient();
        client.BaseAddress = new Uri("https://localhost:7261/");//localhost of YOUR API

        //Convert the registration model into JSON.
        var json = JsonConvert.SerializeObject(model);//serialize the user input to send to API
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //API endpoint to which this register request needs to be sent
        var response = await client.PostAsync("api/auth/register", content);

        //If register succeeds redirect user to now Login
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", $"Registration failed. API returned {(int)response.StatusCode}.");

        return View(model);
    }

    [HttpGet]//show the login view
    public IActionResult Login() => View();


    [HttpPost] //understand this through slide 5: in ppt called cookies
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        /* MVC server sends a request to the API server */
        var client = _clientFactory.CreateClient("StudentTeacherApi");
        client.BaseAddress = new Uri("https://localhost:7261/");

        var json = JsonConvert.SerializeObject(model);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        /* (refer to the API Auth Controller comment called "Step 1"
        At this point the API runs its Login endpoint
        Sends back the 'Set-Cookie' response and cookie information */
        var response = await client.PostAsync("api/auth/login", content);

        //MVC server reads that cookie issued by the API server 
        if (response.IsSuccessStatusCode)
        {
            // Read the Set-Cookie header returned by the API.
            if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            {
                ModelState.AddModelError("", "No authentication cookie was returned.");
                return View(model);
            }

            // Find the API authentication cookie.
            var setCookie = cookieHeaders.FirstOrDefault(
                c => c.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));

            if (setCookie == null)
            {
                ModelState.AddModelError("", "Authentication cookie was not found.");
                return View(model);
            }

            // Keep only the cookie name and value.
            var authCookie = setCookie.Split(';', 2)[0];

            // Store it in this user's MVC session.
            HttpContext.Session.SetString("ApiAuthCookie", authCookie);

            return RedirectToAction("Index", "Home");
        }


        ModelState.AddModelError("", "Invalid login");
        return View(model);
    }


    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Remove("ApiAuthCookie");
        return RedirectToAction("Login");
    }
}