using Authentication_API;
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
        [HttpPost("/api/courses/{id:int}/enrol")]
        [Authorize(Roles = "Student")]
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
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
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
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Student")]
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
        [HttpGet("/api/courses/{id:int}/enrolments")]
        [Authorize(Roles = "Teacher")]
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
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Teacher")]
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
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Student")]
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

    public class EnrolmentStatusDto
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}
