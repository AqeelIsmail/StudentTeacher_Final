using Authentication_API;
using Authentication_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Authentication_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all available courses.
        /// </summary>
        /// <remarks>
        /// This endpoint can be accessed by authenticated
        /// Students and Teachers.
        /// </remarks>
        /// <returns>
        /// A list of available courses.
        /// </returns>
        /// <response code="200">
        /// Courses retrieved successfully.
        /// </response>
        /// <response code="401">
        /// The user is not authenticated.
        /// </response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCourses()
        {
            var courses = await _context.Courses.AsNoTracking()
                .Select(c => new
                {
                    c.CourseId,
                    c.Code,
                    c.Title,
                    c.Description,
                    c.CourseCategoryId,
                    CategoryName = c.CourseCategory.Name,
                    c.TeacherProfileId,
                    TeacherFirstName = c.TeacherProfile.FirstName,
                    TeacherLastName = c.TeacherProfile.LastName,
                    c.CourseImageUrl,
                    c.CreatedAt
                }).ToListAsync();
            return Ok(courses);
        }

        // GET: api/courses/my - courses belonging to the signed-in Teacher
        [HttpGet("my")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetMyCourses()
        {
            int? teacherId = await GetCurrentTeacherProfileId();
            if (teacherId == null) return NotFound("Teacher profile not found.");

            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.TeacherProfileId == teacherId.Value)
                .Select(c => new
                {
                    c.CourseId,
                    c.Code,
                    c.Title,
                    c.Description,
                    c.CourseCategoryId,
                    CategoryName = c.CourseCategory.Name,
                    c.TeacherProfileId,
                    c.CourseImageUrl,
                    c.CreatedAt
                }).ToListAsync();
            return Ok(courses);
        }

        // GET: api/courses/5 - course details including category and Teacher
        /// <summary>
        /// Retrieves a specific course by its ID.
        /// </summary>
        /// <remarks>
        /// This endpoint can be accessed by authenticated
        /// Students and Teachers.
        /// </remarks>
        /// <param name="id">
        /// The unique ID of the course to retrieve.
        /// </param>
        /// <returns>
        /// The requested course and its details, if found.
        /// </returns>
        /// <response code="200">
        /// The course was retrieved successfully.
        /// </response>
        /// <response code="401">
        /// The user is not authenticated.
        /// </response>
        /// <response code="404">
        /// No course was found with the specified ID.
        /// </response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _context.Courses.AsNoTracking()
                .Where(c => c.CourseId == id)
                .Select(c => new
                {
                    c.CourseId,
                    c.Code,
                    c.Title,
                    c.Description,
                    c.CourseCategoryId,
                    CategoryName = c.CourseCategory.Name,
                    c.TeacherProfileId,
                    TeacherFirstName = c.TeacherProfile.FirstName,
                    TeacherLastName = c.TeacherProfile.LastName,
                    c.CourseImageUrl,
                    c.CreatedAt
                }).FirstOrDefaultAsync();

            if (course == null) return NotFound("Course not found.");
            return Ok(course);
        }

        // POST: api/courses - Teacher creates a course assigned to own profile
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateCourse(CourseDto dto)
        {
            int? teacherId = await GetCurrentTeacherProfileId();
            if (teacherId == null) return NotFound("Teacher profile not found.");

            bool categoryExists = await _context.CourseCategories
                .AnyAsync(c => c.CourseCategoryId == dto.CourseCategoryId);
            if (!categoryExists) return BadRequest("Invalid course category.");

            bool codeExists = await _context.Courses.AnyAsync(c => c.Code == dto.Code);
            if (codeExists) return Conflict("Course code already exists.");

            var course = new Course
            {
                TeacherProfileId = teacherId.Value,
                CourseCategoryId = dto.CourseCategoryId,
                Code = dto.Code,
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.Now
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                course.CourseId,
                course.TeacherProfileId,
                course.CourseCategoryId,
                course.Code,
                course.Title,
                course.Description,
                course.CourseImageUrl,
                course.CreatedAt
            });
        }

        // PUT: api/courses/5 - only the Teacher who owns the course
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateCourse(int id, CourseDto dto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound("Course not found.");

            int? teacherId = await GetCurrentTeacherProfileId();
            if (teacherId == null || course.TeacherProfileId != teacherId.Value)
                return Forbid();

            bool categoryExists = await _context.CourseCategories
                .AnyAsync(c => c.CourseCategoryId == dto.CourseCategoryId);
            if (!categoryExists) return BadRequest("Invalid course category.");

            bool duplicateCode = await _context.Courses
                .AnyAsync(c => c.Code == dto.Code && c.CourseId != id);
            if (duplicateCode) return Conflict("Course code already exists.");

            course.CourseCategoryId = dto.CourseCategoryId;
            course.Code = dto.Code;
            course.Title = dto.Title;
            course.Description = dto.Description;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                course.CourseId,
                course.TeacherProfileId,
                course.CourseCategoryId,
                course.Code,
                course.Title,
                course.Description,
                course.CourseImageUrl,
                course.CreatedAt
            });
        }

        // DELETE: api/courses/5 - only owner, only without enrolments
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound("Course not found.");

            int? teacherId = await GetCurrentTeacherProfileId();
            if (teacherId == null || course.TeacherProfileId != teacherId.Value)
                return Forbid();

            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CourseId == id);
            if (hasEnrolments)
                return Conflict("Cannot delete a course with enrolments.");

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<int?> GetCurrentTeacherProfileId()
        {
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdText, out int userId)) return null;

            return await _context.TeacherProfiles
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.TeacherProfileId)
                .FirstOrDefaultAsync();
        }
    }

    public class CourseDto
    {
        [Range(1, int.MaxValue)]
        public int CourseCategoryId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
