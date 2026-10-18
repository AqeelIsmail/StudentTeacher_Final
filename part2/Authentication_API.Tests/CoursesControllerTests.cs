using Authentication_API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication_API.Tests;

public class CoursesControllerTests
{
    [Fact]
    public async Task Teacher_CreateCourse_Returns201_AndSavesCourse()
    {
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);
        var controller = new CoursesController(db);
        TestHelper.SignIn(controller, 1, "Teacher");

        var response = await controller.CreateCourse(new CourseDto
        {
            CourseCategoryId = 1,
            Code = "TEST101",
            Title = "Test Course"
        });

        Assert.Equal(201, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.True(await db.Courses.AnyAsync(c => c.Code == "TEST101" && c.TeacherProfileId == 1));
    }

    [Fact]
    public async Task Teacher_CreateCourse_InvalidCategory_Returns400()
    {
        //Arrange
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);
        var controller = new CoursesController(db);
        TestHelper.SignIn(controller, 1, "Teacher");
        //Act
        var response = await controller.CreateCourse(new CourseDto
        {
            CourseCategoryId = 999,
            Code = "TEST102",
            Title = "Invalid Category"
        });
        //Assert
        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Empty(await db.Courses.ToListAsync());
    }
}
