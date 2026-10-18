using Microsoft.AspNetCore.Mvc;
using StudentTeacher.Mvc.Models;
using System.Diagnostics;

namespace StudentTeacher.Mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public HomeController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        //Understand this through the slide: Go to slide 6 in the ppt called 'cookies'
        public async Task<IActionResult> TeacherArea()
        {
            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            //Finds the session that was named at login 'ApiAuthCooki' (finds the drawer with the label)
            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (authCookie != null)
            {
                //If it is found, keyword 'Cookie' is attached to the request sent to the API (API looks for this keyword in request header)
                client.DefaultRequestHeaders.Add("Cookie", authCookie);
            }
            //API receives the request with the Cookie attached, allowing API to know which role is logged in 
            var response = await client.GetAsync("api/Teacher/teacher-only");

            string modelMessage;

            if (response.IsSuccessStatusCode)
            {
                modelMessage = await response.Content.ReadAsStringAsync();
            }
            else
            {
                modelMessage = "Access Denied: " + response.StatusCode;
            }


            return View(model: modelMessage );
        }

        public async Task<IActionResult> StudentArea()
        {
            var client = _clientFactory.CreateClient("StudentTeacherApi");
            client.BaseAddress = new Uri("https://localhost:7261/");

            var authCookie = HttpContext.Session.GetString("ApiAuthCookie");
            if (authCookie != null)
            {
                client.DefaultRequestHeaders.Add("Cookie", authCookie);
            }

            var response = await client.GetAsync("api/Student/student-only");
            string modelMessage;

            if (response.IsSuccessStatusCode)
            {
                modelMessage = await response.Content.ReadAsStringAsync();
            }
            else
            {
                modelMessage = "Access Denied: " + response.StatusCode;
            }


            return View(model: modelMessage);
        }
    }
}
