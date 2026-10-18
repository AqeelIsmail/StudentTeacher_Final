using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashBoardController : ControllerBase
    {
        /// <summary>
        /// Checks whether an API caller is authenticated.
        /// </summary>
        /// <remarks>
        /// Diagnostic endpoint available to either authenticated role.
        /// </remarks>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Authenticated.</response>
        /// <response code="401">Authentication required.</response>
        [Authorize] // any logged-in user, regardless of role
        [HttpGet("logged-in-only")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult LoggedInOnly()
        {
            return Ok("You are logged in, but this doesn't check your role.");
        }
    }
}