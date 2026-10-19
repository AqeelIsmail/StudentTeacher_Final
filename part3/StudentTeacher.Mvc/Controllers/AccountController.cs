using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StudentTeacher.Mvc.Models;
using System.Text;
using System.Text.Json;
namespace StudentTeacher.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _clientFactory;

    public AccountController(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Login");
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
    [ValidateAntiForgeryToken]
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

            // Verify the role with the API, not with a value supplied by the browser.
            using var profileRequest = new HttpRequestMessage(
                HttpMethod.Get, "api/auth/profile");
            profileRequest.Headers.TryAddWithoutValidation("Cookie", authCookie);
            using var profileResponse = await client.SendAsync(profileRequest);
            if (!profileResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Login succeeded, but the user profile could not be verified.");
                return View(model);
            }

            using var profileDocument = JsonDocument.Parse(
                await profileResponse.Content.ReadAsStringAsync());
            string? role = profileDocument.RootElement
                .GetProperty("role").GetString();

            if (role != "Teacher" && role != "Student")
            {
                ModelState.AddModelError("", "The API returned an unsupported account role.");
                return View(model);
            }

            HttpContext.Session.SetString("ApiAuthCookie", authCookie);
            HttpContext.Session.SetString("ApiRole", role);

            return RedirectToAction(
                role == "Teacher" ? "TeacherArea" : "StudentArea", "Home");
        }


        ModelState.AddModelError("", "Invalid login");
        return View(model);
    }



    [HttpPost] //means logout must be submitted using an HTTP POST request.
    [ValidateAntiForgeryToken]//helps protect against unwanted form submissions.
    public async Task<IActionResult> Logout()
    {
        // Retrieve the API authentication cookie from MVC session.
        var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
        //Session.GetString() retrieves the cookie saved when the user logged in.
        if (!string.IsNullOrEmpty(authCookie))
        {
            // Use the named HttpClient configured in Program.cs.
            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            // Send the current user's authentication cookie to the API.
            using var request = new HttpRequestMessage( 
                //HttpRequestMessage allows us to construct the API request and attach its cookie.
                HttpMethod.Post,
                "api/auth/logout");

            request.Headers.TryAddWithoutValidation("Cookie", authCookie);

            try
            {
                // Call the API logout endpoint.
                using var response = await client.SendAsync(request);
                //SendAsync() sends the logout request asynchronously.

                if (!response.IsSuccessStatusCode)
                {
                    // The API did not complete the logout successfully.
                    // MVC will still clear its own session below.
                }
            }
            catch (HttpRequestException)
            {
                // The API may be temporarily unavailable.
                // MVC still needs to clear its own session.
            }
        }

        // Remove the authentication state stored by MVC.
        HttpContext.Session.Remove("ApiAuthCookie");
        HttpContext.Session.Remove("ApiRole");
        //Session.Remove() ensures MVC stops using the authentication cookie,
        //even if the API cannot be reached.

        // Return to the login page.
        return RedirectToAction("Login");
    }

}