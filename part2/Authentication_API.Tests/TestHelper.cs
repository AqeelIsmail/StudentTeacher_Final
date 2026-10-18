using Authentication_API;
using Authentication_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Authentication_API.Tests;

public static class TestHelper
{
    public static AppDbContext Db() => 
        new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public static void SignIn(ControllerBase controller, int userId, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "TestCookie");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    public static async Task Seed(AppDbContext db)
    {
        db.Users.AddRange(
            new User { 
               UserId = 1, 
                Email = "teacher@test.com", 
                PasswordHash = "test", 
                Role = "Teacher" 
            },
            
            new User 
            { 
                UserId = 2, 
                Email = "student@test.com", 
                PasswordHash = "test", 
                Role = "Student" 
            });
        db.TeacherProfiles.Add(
            new TeacherProfile 
            { TeacherProfileId = 1, UserId = 1, EmployeeNumber = "T001", FirstName = "Test", LastName = "Teacher" });
        db.StudentProfiles.Add(
            new StudentProfile 
            { StudentProfileId = 1, UserId = 2, StudentNumber = "S001", FirstName = "Test", LastName = "Student" });
        db.CourseCategories.Add(
            new CourseCategory { CourseCategoryId = 1, Name = "Programming" });
        await db.SaveChangesAsync();
    }
}
