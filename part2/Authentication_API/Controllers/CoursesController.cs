using Authentication_API.Services; //to use azure BlobStorageService
using Authentication_API.DTOs;
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
        private readonly BlobStorageService _blobStorageService;
        private readonly IConfiguration _configuration;

        public CoursesController(
            AppDbContext context,
            BlobStorageService blobStorageService,
            IConfiguration configuration)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _configuration = configuration;
        }

        /// <summary>
        /// Retrieves all available courses.
        /// </summary>
        /// <remarks>
        /// Public endpoint. Visitors, Students and Teachers can
        /// browse available courses without requiring authentication.
        /// </remarks>
        /// <returns>
        /// A list of available courses.
        /// </returns>
        /// <response code="200">
        /// Courses retrieved successfully.
        /// </response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        /// <summary>
        /// Lists courses created by the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Teacher role required; courses are filtered by the current Teacher profile.
        /// </remarks>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Teacher course list returned.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Teacher role required.</response>
        /// <response code="404">Teacher profile not found.</response>
        [HttpGet("my")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <param name="id">
        /// The unique ID of the course to retrieve.
        /// </param>
        /// <returns>
        /// The requested course and its details, if found.
        /// </returns>
        /// <response code="200">
        /// The course was retrieved successfully.
        /// </response>
        /// <response code="404">
        /// No course was found with the specified ID.
        /// </response>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        /// <summary>
        /// Creates a course for the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Teacher role required. The supplied category must exist and the course code must be unique.
        /// </remarks>
        /// <param name="dto">Course code, title, description and category identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="201">Course created.</response>
        /// <response code="400">Invalid course data or category.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Teacher role required.</response>
        /// <response code="404">Teacher profile not found.</response>
        /// <response code="409">Course code already exists.</response>
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
        /// <summary>
        /// Updates a course owned by the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Rejects changes by a different Teacher, missing categories, and duplicate course codes.
        /// </remarks>
        /// <param name="id">Course identifier.</param>
        /// <param name="dto">Updated course fields.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="200">Updated course returned.</response>
        /// <response code="400">Invalid course data or category.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Teacher or wrong role.</response>
        /// <response code="404">Course not found.</response>
        /// <response code="409">Course code already exists.</response>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
        /// <summary>
        /// Deletes a course owned by the signed-in Teacher.
        /// </summary>
        /// <remarks>
        /// Deletion is blocked when the course already has enrolments.
        /// </remarks>
        /// <param name="id">Course identifier.</param>
        /// <returns>An HTTP response indicating the outcome of the operation.</returns>
        /// <response code="204">Course deleted.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Not the owning Teacher or wrong role.</response>
        /// <response code="404">Course not found.</response>
        /// <response code="409">Course has enrolments.</response>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        //POST /api/Courses/{id}/image

        /// <summary>
        /// Uploads or replaces a course image.
        /// </summary>
        /// <remarks>
        /// Only the Teacher who owns the course can upload its image.
        /// The file is stored in Azure Blob Storage.
        /// The image URL is saved in SQL Server.
        /// </remarks>
        /// <param name="id">The ID of the course.</param>
        /// <param name="image">The JPG or PNG image to upload.</param>
        /// <returns>The URL of the uploaded image.</returns>
        /// <response code="200">Image uploaded successfully.</response>
        /// <response code="400">Invalid image file.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Teacher does not own the course.</response>
        /// <response code="404">Course not found.</response>
        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Teacher")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadCourseImage(
            int id,
            IFormFile image)
        {
            // 1. Find the selected course.
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
                return NotFound("Course not found.");

            // 2. Get the logged-in Teacher's profile ID.
            int? teacherId = await GetCurrentTeacherProfileId();

            // Only the Teacher who created the course may upload.
            if (teacherId == null ||
                course.TeacherProfileId != teacherId.Value)
            {
                return Forbid();
            }

            // 3. Check that a file was provided.
            if (image == null || image.Length == 0)
                return BadRequest("Please select an image.");

            // 4. Limit image size to 5 MB.
            if (image.Length > 5 * 1024 * 1024)
                return BadRequest("Image must be smaller than 5 MB.");

            // 5. Allow only JPG and PNG images.
            string extension = Path.GetExtension(image.FileName)
                .ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                return BadRequest("Only JPG and PNG images are allowed.");
            }

            // Check the declared content type matches the extension.
            bool validType =
                ((extension == ".jpg" || extension == ".jpeg")
                    && image.ContentType == "image/jpeg")
                || (extension == ".png"
                    && image.ContentType == "image/png");

            if (!validType)
                return BadRequest("Invalid image content type.");


            // 6. Read the container name from configuration.
            string containerName =
                _configuration["AzureStorage:CourseImagesContainer"]!;

            // 7. Remember the existing image URL (if any).
            string? oldImageUrl = course.CourseImageUrl;

            // 8. Upload the new image to Azurite.
            string newImageUrl =
                await _blobStorageService.UploadImageAsync(
                    image, containerName);

            // 9. Update the course's image URL.
            course.CourseImageUrl = newImageUrl;

            // 10. Save the new URL to SQL Server.
            await _context.SaveChangesAsync();

            // 11. Delete the previous image if one existed.
            if (!string.IsNullOrWhiteSpace(oldImageUrl))
            {
                await _blobStorageService.DeleteImageAsync(
                    oldImageUrl, containerName);
            }

            // 12. Return the new image URL.
            return Ok(new
            {
                imageUrl = newImageUrl,
                course = new
                {
                    course.CourseId,
                    course.Code,
                    course.Title,
                    course.CourseImageUrl
                }
            });
        }


        /// <summary>
        /// Removes an image from a course owned by the signed-in Teacher.
        /// </summary>
        /// <param name="id">The course ID.</param>
        /// <response code="204">Course image removed.</response>
        /// <response code="401">Authentication required.</response>
        /// <response code="403">Teacher does not own the course.</response>
        /// <response code="404">Course not found.</response>
        [HttpDelete("{id:int}/image")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCourseImage(int id)
        {
            // Find the course.
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
                return NotFound("Course not found.");

            // Check course ownership.
            int? teacherId = await GetCurrentTeacherProfileId();

            if (teacherId == null ||
                course.TeacherProfileId != teacherId.Value)
            {
                return Forbid();
            }

            // Nothing to remove.
            if (string.IsNullOrWhiteSpace(course.CourseImageUrl))
                return NoContent();

            // Remember the image URL.
            string oldImageUrl = course.CourseImageUrl;

            // Remove the URL from the database.
            course.CourseImageUrl = null;

            await _context.SaveChangesAsync();

            // Delete the actual image from Blob Storage.

            string containerName =
                _configuration["AzureStorage:CourseImagesContainer"]!;

            await _blobStorageService.DeleteImageAsync(
                oldImageUrl, containerName);


            return NoContent();
        }


    }

    //public class CourseDto
    //{
    //    [Range(1, int.MaxValue)]
    //    public int CourseCategoryId { get; set; }

    //    [Required]
    //    [MaxLength(30)]
    //    public string Code { get; set; } = null!;

    //    [Required]
    //    [MaxLength(150)]
    //    public string Title { get; set; } = null!;

    //    [MaxLength(1000)]
    //    public string? Description { get; set; }
    //}
}
