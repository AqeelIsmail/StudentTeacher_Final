using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : Controller
    {
        /// <summary>
        /// Checks access to a Teacher-only API endpoint.
        /// </summary>
        /// <remarks>
        /// Diagnostic endpoint restricted to Teacher role.
        /// </remarks>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Teacher role authorized.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Teacher role required.</response>
        [Authorize(Roles = "Teacher")]
        [HttpGet("teacher-only")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult TeacherOnly()
        {
            return Ok("Hello Teacher! You are authorized.");
        }
    }
}
