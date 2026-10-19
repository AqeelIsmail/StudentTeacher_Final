
using Authentication_API.Controllers;
using Authentication_API.DTOs;
using Authentication_API.Models;
using Authentication_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Authentication_API.Tests;

public class CoursesControllerTests
{
    // Create a CoursesController with its required dependencies.
    private static CoursesController CreateController(AppDbContext db)
    {
        // Provide the Azure settings needed by BlobStorageService.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                ["AzureStorage:CourseImagesContainer"] = "course-images"
            })
            .Build();

        var blobStorageService = new BlobStorageService(configuration);

        return new CoursesController(
            db,
            blobStorageService,
            configuration);
    }

    [Fact]
    public async Task Teacher_CreateCourse_Returns201_AndSavesCourse()
    {
        // Arrange
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);

        var controller = CreateController(db);
        TestHelper.SignIn(controller, 1, "Teacher");

        // Act
        var response = await controller.CreateCourse(new CourseDto
        {
            CourseCategoryId = 1,
            Code = "TEST101",
            Title = "Test Course"
        });

        // Assert
        Assert.Equal(
            201,
            Assert.IsType<ObjectResult>(response).StatusCode);

        Assert.True(
            await db.Courses.AnyAsync(
                c => c.Code == "TEST101" &&
                     c.TeacherProfileId == 1));
    }

    [Fact]
    public async Task Teacher_CreateCourse_InvalidCategory_Returns400()
    {
        // Arrange
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);

        var controller = CreateController(db);
        TestHelper.SignIn(controller, 1, "Teacher");

        // Act
        var response = await controller.CreateCourse(new CourseDto
        {
            CourseCategoryId = 999,
            Code = "TEST102",
            Title = "Invalid Category"
        });

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);

        Assert.Empty(await db.Courses.ToListAsync());
    }
}
