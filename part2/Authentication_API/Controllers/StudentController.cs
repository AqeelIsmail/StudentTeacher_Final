using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : Controller
    {
        [Authorize(Roles = "Student")]
        [HttpGet("student-only")]
        public IActionResult StudentOnly()
        {
            return Ok("Hello Student! You are authorized.");
        }

    }
}
