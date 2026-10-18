using Authentication_API.Controllers;
using Authentication_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication_API.Tests;

public class EnrolmentsControllerTests
{
    [Fact]
    public async Task Student_Enrol_Returns201_AndStoresPending()
    {
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);
        db.Courses.Add(new Course
        {
            CourseId = 1,
            TeacherProfileId = 1,
            CourseCategoryId = 1,
            Code = "TEST101",
            Title = "Test Course"
        });
        await db.SaveChangesAsync();

        var controller = new EnrolmentsController(db);
        TestHelper.SignIn(controller, 2, "Student");
        var response = await controller.Enrol(1);

        Assert.Equal(201, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.True(await db.Enrolments.AnyAsync(e => e.CourseId == 1 &&
            e.StudentProfileId == 1 && e.Status == "Pending"));
    }

    [Fact]
    public async Task Student_Enrol_Twice_Returns409()
    {
        using var db = TestHelper.Db();
        await TestHelper.Seed(db);
        db.Courses.Add(new Course
        {
            CourseId = 1,
            TeacherProfileId = 1,
            CourseCategoryId = 1,
            Code = "TEST101",
            Title = "Test Course"
        });
        await db.SaveChangesAsync();

        var controller = new EnrolmentsController(db);
        TestHelper.SignIn(controller, 2, "Student");
        await controller.Enrol(1);
        var response = await controller.Enrol(1);

        Assert.IsType<ConflictObjectResult>(response);
        Assert.Equal(1, await db.Enrolments.CountAsync());
    }
}
