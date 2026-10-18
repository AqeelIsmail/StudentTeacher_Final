using Authentication_API.DTOs;
using Authentication_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Authentication_API.Controllers
{
    [Route("api/enrolments")]
    [ApiController]
    [Authorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EnrolmentsController(AppDbContext context) => _context = context;

        // POST /api/courses/{id}/enrol - Student enrols in a course.
        /// <summary>
        /// Enrols the signed-in Student in a course.
        /// </summary>
        /// <remarks>
        /// Creates a Pending enrolment. Duplicate enrolments are rejected.
        /// </remarks>
        /// <param name="id">Course identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="201">Enrolment created.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Student role required.</response>
        /// <response code="404">Student profile or course not found.</response>
        /// <response code="409">Student already enrolled in this course.</response>
        [HttpPost("/api/courses/{id:int}/enrol")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int id)
        {
            int? studentId = await CurrentStudentProfileId();
            if (studentId == null) return NotFound("Student profile not found.");
            if (!await _context.Courses.AnyAsync(c => c.CourseId == id))
                return NotFound("Course not found.");
            if (await _context.Enrolments.AnyAsync(e => e.CourseId == id && e.StudentProfileId == studentId.Value))
                return Conflict("You are already enrolled in this course.");

            var enrolment = new Enrolment
            {
                CourseId = id,
                StudentProfileId = studentId.Value,
                EnrolledAt = DateTime.Now,
                Status = "Pending"
            };
            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();
            return StatusCode(201, new
            {
                enrolment.EnrolmentId, enrolment.StudentProfileId,
                enrolment.CourseId, enrolment.EnrolledAt, enrolment.Status
            });
        }

        // GET /api/enrolments/my - Student's own enrolments.
        /// <summary>
        /// Lists enrolments belonging to the signed-in Student.
        /// </summary>
        /// <remarks>
        /// Student role required.
        /// </remarks>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Student enrolments returned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Student role required.</response>
        /// <response code="404">Student profile not found.</response>
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            int? studentId = await CurrentStudentProfileId();
            if (studentId == null) return NotFound("Student profile not found.");
            var enrolments = await _context.Enrolments.AsNoTracking()
                .Where(e => e.StudentProfileId == studentId.Value)
                .Select(e => new
                {
                    e.EnrolmentId, e.CourseId, CourseCode = e.Course.Code,
                    CourseTitle = e.Course.Title, e.EnrolledAt, e.Status
                }).ToListAsync();
            return Ok(enrolments);
        }

        // GET /api/enrolments/{id} - Student may view only their own enrolment.
        /// <summary>
        /// Retrieves an enrolment owned by the signed-in Student.
        /// </summary>
        /// <remarks>
        /// Students may not read another Student's enrolment.
        /// </remarks>
        /// <param name="id">Enrolment identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Enrolment returned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Student or wrong role.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.AsNoTracking()
                .Where(e => e.EnrolmentId == id)
                .Select(e => new
                {
                    e.EnrolmentId, e.StudentProfileId, e.CourseId,
                    CourseCode = e.Course.Code, CourseTitle = e.Course.Title,
                    e.EnrolledAt, e.Status
                }).FirstOrDefaultAsync();
            if (enrolment == null) return NotFound("Enrolment not found.");
            int? studentId = await CurrentStudentProfileId();
            if (studentId == null || enrolment.StudentProfileId != studentId.Value) return Forbid();
            return Ok(enrolment);
        }

        // GET /api/courses/{id}/enrolments - Teacher sees their course's students.
        /// <summary>
        /// Lists students enrolled in a course owned by the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Teacher role required and course ownership is checked.
        /// </remarks>
        /// <param name="id">Course identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Course enrolments returned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Teacher or wrong role.</response>
        /// <response code="404">Course not found.</response>
        [HttpGet("/api/courses/{id:int}/enrolments")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourseEnrolments(int id)
        {
            var course = await _context.Courses.AsNoTracking()
                .Where(c => c.CourseId == id)
                .Select(c => new { c.TeacherProfileId }).FirstOrDefaultAsync();
            if (course == null) return NotFound("Course not found.");
            int? teacherId = await CurrentTeacherProfileId();
            if (teacherId == null || course.TeacherProfileId != teacherId.Value) return Forbid();

            var enrolments = await _context.Enrolments.AsNoTracking()
                .Where(e => e.CourseId == id)
                .Select(e => new
                {
                    e.EnrolmentId, e.CourseId, e.StudentProfileId,
                    e.StudentProfile.StudentNumber, e.StudentProfile.FirstName,
                    e.StudentProfile.LastName, e.EnrolledAt, e.Status
                }).ToListAsync();
            return Ok(enrolments);
        }

        // PUT /api/enrolments/{id}/status - owning Teacher manages status.
        /// <summary>
        /// Changes an enrolment status for a course owned by the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Accepted statuses are Pending, Confirmed and Cancelled. Cannot cancel after results are recorded.
        /// </remarks>
        /// <param name="id">Enrolment identifier.</param>
        /// <param name="dto">Requested enrolment status.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Status updated.</response>
        /// <response code="400">Invalid status or cancellation blocked after results.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Teacher or wrong role.</response>
        /// <response code="404">Enrolment not found.</response>
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] EnrolmentStatusDto dto)
        {
            var enrolment = await _context.Enrolments
                .Include(e => e.Course).FirstOrDefaultAsync(e => e.EnrolmentId == id);
            if (enrolment == null) return NotFound("Enrolment not found.");
            int? teacherId = await CurrentTeacherProfileId();
            if (teacherId == null || enrolment.Course.TeacherProfileId != teacherId.Value) return Forbid();
            if (dto.Status != "Pending" && dto.Status != "Confirmed" && dto.Status != "Cancelled")
                return BadRequest("Status must be Pending, Confirmed or Cancelled.");
            // Do not cancel an enrolment after a result has been recorded.
            if (dto.Status == "Cancelled" && await _context.CourseResults.AnyAsync(r => r.EnrolmentId == id))
                return BadRequest("Cannot cancel an enrolment with a recorded result.");
            enrolment.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(new { enrolment.EnrolmentId, enrolment.CourseId, enrolment.StudentProfileId, enrolment.Status });
        }

        // DELETE /api/enrolments/{id} - Student withdraws their own enrolment.
        /// <summary>
        /// Withdraws the signed-in Student from their own course enrolment.
        /// </summary>
        /// <remarks>
        /// An enrolment with a recorded result cannot be withdrawn.
        /// </remarks>
        /// <param name="id">Enrolment identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="204">Enrolment removed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Student or wrong role.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">Result already recorded.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Withdraw(int id)
        {
            var enrolment = await _context.Enrolments.FindAsync(id);
            if (enrolment == null) return NotFound("Enrolment not found.");
            int? studentId = await CurrentStudentProfileId();
            if (studentId == null || enrolment.StudentProfileId != studentId.Value) return Forbid();
            if (await _context.CourseResults.AnyAsync(r => r.EnrolmentId == id))
                return Conflict("Cannot withdraw after a result has been recorded.");
            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;

        private async Task<int?> CurrentStudentProfileId()
        {
            int? userId = CurrentUserId();
            if (userId == null) return null;
            return await _context.StudentProfiles.Where(s => s.UserId == userId.Value)
                .Select(s => (int?)s.StudentProfileId).FirstOrDefaultAsync();
        }

        private async Task<int?> CurrentTeacherProfileId()
        {
            int? userId = CurrentUserId();
            if (userId == null) return null;
            return await _context.TeacherProfiles.Where(t => t.UserId == userId.Value)
                .Select(t => (int?)t.TeacherProfileId).FirstOrDefaultAsync();
        }
    }

    //public class EnrolmentStatusDto
    //{
    //    [Required]
    //    public string Status { get; set; } = null!;
    //}
}
