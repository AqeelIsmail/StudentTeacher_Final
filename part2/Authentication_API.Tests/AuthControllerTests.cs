using Authentication_API.Controllers;
using Authentication_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Text;

namespace Authentication_API.Tests
{
    public class AuthControllerTests
    {
        // Creates a separate in-memory database for each test
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        // Sets up HttpContext so that Login() can use cookie authentication
        //private void SetupHttpContext(AuthController controller)
        //{
        //    var services = new ServiceCollection();

        //    services
        //        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        //        .AddCookie();

        //    var serviceProvider = services.BuildServiceProvider();

        //    var httpContext = new DefaultHttpContext
        //    {
        //        RequestServices = serviceProvider
        //    };

        //    controller.ControllerContext = new ControllerContext
        //    {
        //        HttpContext = httpContext
        //    };
        //}

        private AuthenticationService SetupHttpContext(AuthController controller)
        {
            var AuthService = new AuthenticationService();

            var services = new ServiceCollection();

            services.AddSingleton<IAuthenticationService>(
                AuthService);

            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return AuthService;
        }


        [Fact] //tells Xunit that this method is a test case
        public async Task Register_ShouldCreateUser() //allows the task to run asynchronously
                                                      //and return a Task, which is required for async test methods
        {
            // Arrange: Create a separate test database (in-memory)
            using var context = CreateDbContext(); 
             
            var controller = new AuthController(context); //creates an instance of the AuthController,
                                                          //passing in the test database context

            // Registration now requires Student profile information
            var dto = new RegisterDto //creates a new instance of RegisterDto and populates it with test data
            {
                Email = "student@test.com",
                Password = "Password123!",
                Role = "Student",
                FirstName = "John",
                LastName = "Smith",
                StudentNumber = "STU001"
            };

            // Act: Call the registration endpoint
            var result = await controller.Register(dto); //calls the register method of the AuthController,
                                                         //passing in the RegisterDto object
                                                         //awaits the result of the registration method

            // Assert: Registration should return HTTP 201 Created
            var createdResult = Assert.IsType<ObjectResult>(result);//checks the type of the result
                                                                    //returned by the Register method
            Assert.Equal(201, createdResult.StatusCode);//compares expected, with actual
                                                        //checks that the status code of the result is 201 (Created)

            // Check that the User was saved
            var user = context.Users.FirstOrDefault();

            Assert.NotNull(user);
            Assert.Equal("student@test.com", user.Email);
            Assert.Equal("Student", user.Role);

            // Check that the password was hashed
            Assert.NotEqual("Password123!", user.PasswordHash);

            // Check that the Student profile was created
            var studentProfile = context.StudentProfiles.FirstOrDefault();

            Assert.NotNull(studentProfile);
            Assert.Equal("STU001", studentProfile.StudentNumber);
            Assert.Equal("John", studentProfile.FirstName);
            Assert.Equal("Smith", studentProfile.LastName);

            // Check the relationship between User and StudentProfile
            Assert.Equal(user.UserId, studentProfile.UserId);//expect, actual
        }


        [Fact]
        public async Task Register_Teacher_ShouldCreateTeacherProfile()
        {
            // Arrange
            using var context = CreateDbContext();

            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "teacher@test.com",
                Password = "Password123!",
                Role = "Teacher",
                FirstName = "Sarah",
                LastName = "Williams",
                EmployeeNumber = "EMP001"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert: HTTP 201 Created
            var createdResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, createdResult.StatusCode);

            // Verify the User record
            var user = Assert.Single(context.Users);
            Assert.Equal("teacher@test.com", user.Email);
            Assert.Equal("Teacher", user.Role);
            Assert.NotEqual("Password123!", user.PasswordHash);

            // Verify the Teacher profile
            var teacherProfile = Assert.Single(context.TeacherProfiles);
            Assert.Equal("EMP001", teacherProfile.EmployeeNumber);
            Assert.Equal("Sarah", teacherProfile.FirstName);
            Assert.Equal("Williams", teacherProfile.LastName);

            // Verify the relationship
            Assert.Equal(user.UserId, teacherProfile.UserId);

            // A Teacher must not receive a Student profile
            Assert.Empty(context.StudentProfiles);
        }

        //same email registration test
        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnConflict()
        {
            // Arrange: Create a temporary in-memory database
            using var context = CreateDbContext();

            // Add an existing user
            var existingUser = new User
            {
                Email = "student@test.com",
                Role = "Student",
                PasswordHash = "ExistingHashedPassword"
            };

            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var controller = new AuthController(context);

            // Attempt to register using the same email
            var dto = new RegisterDto
            {
                Email = "student@test.com",
                Password = "Password123!",
                Role = "Student",
                FirstName = "John",
                LastName = "Smith",
                StudentNumber = "STU002"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert: HTTP 409 Conflict
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal("Email is already registered.", conflictResult.Value);

            // No additional user or profile should be created
            Assert.Single(context.Users);
            Assert.Empty(context.StudentProfiles);
        }


        // Login test for correct password
        [Fact]
        public async Task Login_WithCorrectPassword_ShouldReturnOk()
        {
            // Arrange
            using var context = CreateDbContext();

            var user = new User
            {
                Email = "student@test.com",
                Role = "Student"
            };

            var hasher = new PasswordHasher<User>();

            user.PasswordHash = hasher.HashPassword(
                user,
                "Password123!"
            );

            context.Users.Add(user);
            context.SaveChanges();

            var controller = new AuthController(context);

            //// Set up HttpContext for cookie authentication
            //SetupHttpContext(controller);

            var AuthService = SetupHttpContext(controller);

            var dto = new LoginDto
            {
                Email = "student@test.com",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            //Assert.Equal("Logged in", okResult.Value);
            Assert.Equal("Logged in", okResult.Value);

            // Verify that the controller attempted to create the login session
            Assert.True(AuthService.SignInCalled);
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            using var context = CreateDbContext();

            var user = new User
            {
                Email = "student@test.com",
                Role = "Student"
            };

            var hasher = new PasswordHasher<User>();

            user.PasswordHash = hasher.HashPassword(
                user,
                "CorrectPassword123!"
            );

            context.Users.Add(user);
            context.SaveChanges();

            var controller = new AuthController(context);

            SetupHttpContext(controller);

            var dto = new LoginDto
            {
                Email = "student@test.com",
                Password = "WrongPassword123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ShouldReturnUnauthorized()
        {
            // Arrange
            using var context = CreateDbContext();

            var controller = new AuthController(context);

            SetupHttpContext(controller);

            var dto = new LoginDto
            {
                Email = "doesnotexist@test.com",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task Logout_ShouldReturnOk()
        {
            // Arrange
            using var context = CreateDbContext();

            var controller = new AuthController(context);

            var authService = SetupHttpContext(controller);

            // Act
            var result = await controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);

            Assert.Equal("Logged out", okResult.Value);

            // Verify that the controller attempted to sign the user out
            Assert.True(authService.SignOutCalled);
        }


        [Fact]
        public async Task Register_WithInvalidRole_ShouldReturnBadRequest()
        {
            using var context = CreateDbContext();
            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "admin@test.com",
                Password = "Password123!",
                Role = "Admin",
                FirstName = "John",
                LastName = "Smith"
            };

            var result = await controller.Register(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, badRequest.StatusCode);
            Assert.Equal("Role must be Student or Teacher.", badRequest.Value);

            Assert.Empty(context.Users);
        }

        [Fact]
        public async Task Register_WithoutStudentNumber_ShouldReturnBadRequest()
        {
            using var context = CreateDbContext();
            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "student@test.com",
                Password = "Password123!",
                Role = "Student",
                FirstName = "John",
                LastName = "Smith",
                StudentNumber = null
            };

            var result = await controller.Register(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, badRequest.StatusCode);
            Assert.Equal("Student number is required.", badRequest.Value);

            Assert.Empty(context.Users);
            Assert.Empty(context.StudentProfiles);
        }

        [Fact]
        public async Task Register_WithoutEmployeeNumber_ShouldReturnBadRequest()
        {
            using var context = CreateDbContext();
            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "teacher@test.com",
                Password = "Password123!",
                Role = "Teacher",
                FirstName = "Sarah",
                LastName = "Williams",
                EmployeeNumber = null
            };

            var result = await controller.Register(dto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, badRequest.StatusCode);
            Assert.Equal("Employee number is required.", badRequest.Value);

            Assert.Empty(context.Users);
            Assert.Empty(context.TeacherProfiles);
        }

        [Fact]
        public async Task Register_WithDuplicateStudentNumber_ShouldReturnConflict()
        {
            using var context = CreateDbContext();

            var existingUser = new User
            {
                Email = "existingstudent@test.com",
                Role = "Student",
                PasswordHash = "ExistingHash"
            };

            existingUser.StudentProfile = new StudentProfile
            {
                StudentNumber = "STU001",
                FirstName = "Existing",
                LastName = "Student"
            };

            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "newstudent@test.com",
                Password = "Password123!",
                Role = "Student",
                FirstName = "John",
                LastName = "Smith",
                StudentNumber = "STU001"
            };

            var result = await controller.Register(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(409, conflict.StatusCode);
            Assert.Equal("Student number already exists.", conflict.Value);

            Assert.Single(context.Users);
            Assert.Single(context.StudentProfiles);
        }

        [Fact]
        public async Task Register_WithDuplicateEmployeeNumber_ShouldReturnConflict()
        {
            using var context = CreateDbContext();

            var existingUser = new User
            {
                Email = "existingteacher@test.com",
                Role = "Teacher",
                PasswordHash = "ExistingHash"
            };

            existingUser.TeacherProfile = new TeacherProfile
            {
                EmployeeNumber = "EMP001",
                FirstName = "Existing",
                LastName = "Teacher"
            };

            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var controller = new AuthController(context);

            var dto = new RegisterDto
            {
                Email = "newteacher@test.com",
                Password = "Password123!",
                Role = "Teacher",
                FirstName = "Sarah",
                LastName = "Williams",
                EmployeeNumber = "EMP001"
            };

            var result = await controller.Register(dto);

            var conflict = Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(409, conflict.StatusCode);
            Assert.Equal("Employee number already exists.", conflict.Value);

            Assert.Single(context.Users);
            Assert.Single(context.TeacherProfiles);
        }

    }
}
