
using Authentication_API;
using Authentication_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Authentication_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        // Database context provided through Dependency Injection
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/categories
        // Both Students and Teachers can view course categories
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.CourseCategories
                .AsNoTracking()
                .Select(c => new
                {
                    c.CourseCategoryId,
                    c.Name,
                    c.Description
                })
                .ToListAsync();

            return Ok(categories); // HTTP 200
        }

        // POST: api/categories
        // Only Teachers can create course categories
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateCategory(
            CourseCategoryDto dto)
        {
            // Check whether a category with the same name exists
            bool exists = await _context.CourseCategories
                .AnyAsync(c => c.Name == dto.Name);

            if (exists)
            {
                return Conflict("Category name already exists."); // HTTP 409
            }

            // Create a new CourseCategory entity
            var category = new CourseCategory
            {
                Name = dto.Name,
                Description = dto.Description
            };

            // Add the entity to the DbContext
            _context.CourseCategories.Add(category);

            // Save the new category to SQL Server
            await _context.SaveChangesAsync();

            // HTTP 201 Created
            return StatusCode(201, new
            {
                message = "Course category created successfully.",
                courseCategoryId = category.CourseCategoryId,
                name = category.Name,
                description = category.Description
            });
        }

        // PUT: api/categories/{id}
        // Only Teachers can update course categories
        [HttpPut("{id}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            CourseCategoryDto dto)
        {
            // Find the category using its primary key
            var category = await _context.CourseCategories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound("Category not found."); // HTTP 404
            }

            // Check for another category with the same name
            bool duplicateName = await _context.CourseCategories
                .AnyAsync(c =>
                    c.Name == dto.Name &&
                    c.CourseCategoryId != id);

            if (duplicateName)
            {
                return Conflict("Category name already exists."); // HTTP 409
            }

            // Update the category properties
            category.Name = dto.Name;
            category.Description = dto.Description;

            // Save changes to SQL Server
            await _context.SaveChangesAsync();

            return Ok(new
            {
                category.CourseCategoryId,
                category.Name,
                category.Description
            }); // HTTP 204
        }

        // DELETE: api/categories/{id}
        // Only Teachers can delete course categories
        [HttpDelete("{id}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // Find the category
            var category = await _context.CourseCategories
                .FindAsync(id);

            if (category == null)
            {
                return NotFound("Category not found."); // HTTP 404
            }

            // Prevent deleting categories linked to existing courses
            bool isUsed = await _context.Courses
                .AnyAsync(c => c.CourseCategoryId == id);

            if (isUsed)
            {
                return Conflict(
                    "Cannot delete a category that is used by courses.");
            }

            // Remove the category
            _context.CourseCategories.Remove(category);

            // Save changes to SQL Server
            await _context.SaveChangesAsync();

            return NoContent(); // HTTP 204
        }
    }

    // DTO used for creating and updating course categories
    public class CourseCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
