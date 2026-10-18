using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : Controller
    {
        /// <summary>
        /// Checks access to a Student-only API endpoint.
        /// </summary>
        /// <remarks>
        /// Diagnostic endpoint restricted to Student role.
        /// </remarks>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Student role authorized.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Student role required.</response>
        [Authorize(Roles = "Student")]
        [HttpGet("student-only")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult StudentOnly()
        {
            return Ok("Hello Student! You are authorized.");
        }

    }
}
