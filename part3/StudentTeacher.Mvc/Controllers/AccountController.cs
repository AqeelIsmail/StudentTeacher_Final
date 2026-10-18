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
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        //Create a client instance 
        var client = _clientFactory.CreateClient();
        client.BaseAddress = new Uri("https://localhost:7261/");//localhost of YOUR API

        var json = JsonConvert.SerializeObject(model);//serialize the user input to send to API
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //API endpoint to which this register request needs to be sent
        var response = await client.PostAsync("api/auth/register", content);

        //If register succeeds redirect user to now Login
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Login");
        }

        ModelState.AddModelError("", "Registration failed");
        return View(model);
    }

    [HttpGet]//show the login view
    public IActionResult Login() => View();


    [HttpPost] //understand this through slide 5: in ppt called cookies
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        /* MVC server sends a request to the API server */
        var client = _clientFactory.CreateClient();
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
             /* Reaches into the API's response and pulls out that cookie string
             as plain text, so the MVC server can store it in the drawer (server memory).*/
            var cookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();//Set-Cookie was the keyword set by the API when cookies created

            /* it generates the random Session ID label (8f3a2b91-xyz)
             * creates the drawer
             * puts cookie's value into that drawer, labeled "ApiAuthCookie"*/
            HttpContext.Session.SetString("ApiAuthCookie", cookie);

            return RedirectToAction("Index", "Home"); //browser gets the drawer label
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