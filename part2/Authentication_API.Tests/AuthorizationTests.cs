using Authentication_API.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Authentication_API.Tests;

public class AuthorizationTests
{
    [Theory]
    [InlineData(typeof(CoursesController), "CreateCourse", "Teacher")]
    [InlineData(typeof(EnrolmentsController), "Enrol", "Student")]
    public void RoleRestrictedActions_DeclareExpectedAuthorizeRole(
        Type controllerType, string methodName, string role)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes<AuthorizeAttribute>(),
            attribute => attribute.Roles == role);
    }
}
