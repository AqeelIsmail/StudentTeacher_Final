using Authentication_API;
using Authentication_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Authentication_API.Controllers
{
    [Route("api/results")]
    [ApiController]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public ResultsController(AppDbContext context) => _context = context;

        // POST /api/enrolments/{id}/result - owning Teacher records a confirmed enrolment's mark.
        [HttpPost("/api/enrolments/{id:int}/result")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RecordResult(int id, [FromBody] CourseResultDto dto)
        {
            var enrolment = await _context.Enrolments.AsNoTracking()
                .Where(e => e.EnrolmentId == id)
                .Select(e => new { e.Course.TeacherProfileId, e.Status })
                .FirstOrDefaultAsync();
            if (enrolment == null) return NotFound("Enrolment not found.");
            int? teacherId = await CurrentTeacherProfileId();
            if (teacherId == null || enrolment.TeacherProfileId != teacherId.Value) return Forbid();
            if (dto.FinalMark < 0 || dto.FinalMark > 100)
                return BadRequest("Final mark must be between 0 and 100.");
            if (enrolment.Status != "Confirmed")
                return BadRequest("Results can only be recorded for confirmed enrolments.");
            if (await _context.CourseResults.AnyAsync(r => r.EnrolmentId == id))
                return Conflict("A result has already been recorded for this enrolment.");

            var result = new CourseResult
            {
                EnrolmentId = id,
                FinalMark = dto.FinalMark,
                Outcome = dto.FinalMark >= 50 ? "Pass" : "Fail",
                RecordedAt = DateTime.Now
            };
            _context.CourseResults.Add(result);
            await _context.SaveChangesAsync();
            return StatusCode(201, new
            {
                result.CourseResultId, result.EnrolmentId, result.FinalMark,
                result.Outcome, result.RecordedAt
            });
        }

        // GET /api/results/my - Student's own results.
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyResults()
        {
            int? studentId = await CurrentStudentProfileId();
            if (studentId == null) return NotFound("Student profile not found.");
            var results = await _context.CourseResults.AsNoTracking()
                .Where(r => r.Enrolment.StudentProfileId == studentId.Value)
                .Select(r => new
                {
                    r.CourseResultId, r.EnrolmentId,
                    r.Enrolment.CourseId,
                    CourseCode = r.Enrolment.Course.Code,
                    CourseTitle = r.Enrolment.Course.Title,
                    r.FinalMark, r.Outcome, r.RecordedAt
                }).ToListAsync();
            return Ok(results);
        }

        // GET /api/courses/{id}/results - owning Teacher views course results.
        [HttpGet("/api/courses/{id:int}/results")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetCourseResults(int id)
        {
            var course = await _context.Courses.AsNoTracking()
                .Where(c => c.CourseId == id)
                .Select(c => new { c.TeacherProfileId }).FirstOrDefaultAsync();
            if (course == null) return NotFound("Course not found.");
            int? teacherId = await CurrentTeacherProfileId();
            if (teacherId == null || course.TeacherProfileId != teacherId.Value) return Forbid();

            var results = await _context.CourseResults.AsNoTracking()
                .Where(r => r.Enrolment.CourseId == id)
                .Select(r => new
                {
                    r.CourseResultId, r.EnrolmentId,
                    r.Enrolment.StudentProfileId,
                    r.Enrolment.StudentProfile.StudentNumber,
                    r.Enrolment.StudentProfile.FirstName,
                    r.Enrolment.StudentProfile.LastName,
                    r.FinalMark, r.Outcome, r.RecordedAt
                }).ToListAsync();
            return Ok(results);
        }

        // PUT /api/results/{id} - owning Teacher corrects mark and outcome.
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] CourseResultDto dto)
        {
            var result = await _context.CourseResults
                .Include(r => r.Enrolment).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(r => r.CourseResultId == id);
            if (result == null) return NotFound("Result not found.");
            int? teacherId = await CurrentTeacherProfileId();
            if (teacherId == null || result.Enrolment.Course.TeacherProfileId != teacherId.Value) return Forbid();
            if (dto.FinalMark < 0 || dto.FinalMark > 100)
                return BadRequest("Final mark must be between 0 and 100.");

            result.FinalMark = dto.FinalMark;
            result.Outcome = dto.FinalMark >= 50 ? "Pass" : "Fail";
            await _context.SaveChangesAsync();
            return Ok(new
            {
                result.CourseResultId, result.EnrolmentId, result.FinalMark,
                result.Outcome, result.RecordedAt
            });
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

    public class CourseResultDto
    {
        [Range(typeof(decimal), "0", "100")]
        public decimal FinalMark { get; set; }
    }
}
