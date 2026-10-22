using Authentication_API;
using Authentication_API.Models;
using Authentication_API.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;//using EF core for database operations

[ApiController]
[Route("api/[controller]")]
public class AuthController : Controller
{  
    //https://andrewlock.net/exploring-the-asp-net-core-identity-passwordhasher/

    private readonly AppDbContext _context;

    //PasswordHashed requires a class object to be passed to it on which the hashing will be done
    private readonly PasswordHasher<User> _hasher = new(); //in built salt and hasher 

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // REGISTER: Creates a user and their Student or Teacher profile
    /// <summary>
    /// Registers a Student or Teacher account and creates the linked profile.
    /// </summary>
    /// <remarks>
    /// Public registration. Passwords are hashed; duplicate email or role-specific numbers are rejected.
    /// </remarks>
    /// <param name="dto">Registration details, including the role and corresponding student or employee number.</param>
    /// <returns>An HTTP response indicating the outcome of the operation.</returns>
    /// <response code="201">Account and profile created.</response>
    /// <response code="400">Invalid registration fields, role or missing role-specific identifier.</response>
    /// <response code="409">Email or role-specific identifier already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // STEP 1: Check that the role is valid
        if (dto.Role != "Student" && dto.Role != "Teacher")
        {
            return BadRequest("Role must be Student or Teacher.");
        }

        // STEP 2: Check whether the email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return Conflict("Email is already registered.");
        }

        // STEP 3: Validate the Student or Teacher number
        if (dto.Role == "Student")
        {
            if (string.IsNullOrWhiteSpace(dto.StudentNumber))
            {
                return BadRequest("Student number is required.");
            }

            if (await _context.StudentProfiles
                .AnyAsync(s => s.StudentNumber == dto.StudentNumber))
            {
                return Conflict("Student number already exists.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.EmployeeNumber))
            {
                return BadRequest("Employee number is required.");
            }

            if (await _context.TeacherProfiles
                .AnyAsync(t => t.EmployeeNumber == dto.EmployeeNumber))
            {
                return Conflict("Employee number already exists.");
            }
        }

        // STEP 4: Create the User object
        var newUser = new User
        {
            Email = dto.Email,
            Role = dto.Role
        };

        // Hash the password before storing it
        newUser.PasswordHash =
            _hasher.HashPassword(newUser, dto.Password);

        // STEP 5: Create the correct linked profile
        if (dto.Role == "Student")
        {
            newUser.StudentProfile = new StudentProfile
            {
                StudentNumber = dto.StudentNumber!,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };
        }
        else
        {
            newUser.TeacherProfile = new TeacherProfile
            {
                EmployeeNumber = dto.EmployeeNumber!,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };
        }

        // STEP 6: Save the User and linked profile together
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // STEP 7: Return HTTP 201 Created
        return StatusCode(201, new
        {
            message = "Account and profile created successfully.",
            userId = newUser.UserId,
            role = newUser.Role
        });
    }

    /* Step 1: 
     * Runs when the client sends a request to this endpoint 
     * This endpoint checks if the password sent is correct and creates a cookie on line 68
     *  */
    /// <summary>
    /// Authenticates an existing Student or Teacher using a password.
    /// </summary>
    /// <remarks>
    /// On success, the API issues its cookie-authentication ticket through SignInAsync.
    /// </remarks>
    /// <param name="dto">Email address and password.</param>
    /// <returns>An HTTP response indicating the outcome of the operation.</returns>
    /// <response code="200">Credentials accepted and sign-in requested.</response>
    /// <response code="400">Invalid request body or missing fields.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        //looks up the user by email. If nobody has the email return 401 status code
        var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
        if (user == null)
        { 
            return Unauthorized();
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result == PasswordVerificationResult.Failed) 
        { 
            return Unauthorized(); 
        }

        /* Step 2: Claim created
         Claim is a single labeled fact about the user - Key value pair
         Here we record 3 facts: IDName/email and the persons role 
         This is a bag of facts about this one login */
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        /* Step 3: Bundles the claims of the user in an object, holding info about who this person is
         * Later on when a logged in persons role needs to be checked, this will be referred to */
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        /* Step 4: This makes the API server attach a "Set-Cookie" header to its HTTP response
         * and create a cookie with the encrypted details of the user */
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok("Logged in");
    }
    /// <summary>
    /// Logs out the current user by clearing the API authentication cookie.
    /// </summary>
    /// <remarks>
    /// Signs the user out of cookie authentication.
    /// The response instructs the client to remove its authentication cookie.
    /// </remarks>
    /// <returns>
    /// HTTP 200 OK when the sign-out operation is completed.
    /// </returns>
    /// <response code="200">
    /// The user has been signed out successfully.
    /// </response>
    [HttpPost("logout")] //clears the users cookie
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok("Logged out");
    }

    // GET /api/auth/profile - signed-in Student or Teacher views their own profile.
    /// <summary>
    /// Retrieves the signed-in user and their Student or Teacher profile.
    /// </summary>
    /// <remarks>
    /// Requires API cookie authentication and uses the authenticated user identifier.
    /// </remarks>
    /// <returns>An HTTP response indicating the outcome of the operation.</returns>
    /// <response code="200">User and matching profile returned.</response>
    /// <response code="401">Authentication or user record missing.</response>
    /// <response code="403">User role is unsupported.</response>
    /// <response code="404">Matching Student or Teacher profile not found.</response>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Unauthorized();

        if (user.Role == "Student")
        {
            var profile = await _context.StudentProfiles.AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => new {
                    s.StudentProfileId,
                    s.StudentNumber,
                    s.FirstName,
                    s.LastName,
                    s.ProfileImageUrl
                })
                .FirstOrDefaultAsync();
            if (profile == null) return NotFound("Student profile not found.");
            return Ok(new { user.UserId, user.Email, user.Role, Profile = profile });
        }

        if (user.Role == "Teacher")
        {
            var profile = await _context.TeacherProfiles.AsNoTracking()
                .Where(t => t.UserId == userId)
                .Select(t => new {
                    t.TeacherProfileId,
                    t.EmployeeNumber,
                    t.FirstName,
                    t.LastName,
                    t.ProfileImageUrl
                })
                .FirstOrDefaultAsync();
            if (profile == null) return NotFound("Teacher profile not found.");
            return Ok(new { user.UserId, user.Email, user.Role, Profile = profile });
        }
        return Forbid();
    }

    // PUT /api/auth/profile - update only first and last name on own profile.
    /// <summary>
    /// Updates the signed-in Student or Teacher profile name.
    /// </summary>
    /// <remarks>
    /// Only the authenticated user's own first and last names are updated.
    /// </remarks>
    /// <param name="dto">New first and last names.</param>
    /// <returns>An HTTP response indicating the outcome of the operation.</returns>
    /// <response code="200">Updated profile returned.</response>
    /// <response code="400">Invalid profile data.</response>
    /// <response code="401">Authentication or user record missing.</response>
    /// <response code="403">User role is unsupported.</response>
    /// <response code="404">Matching Student or Teacher profile not found.</response>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return Unauthorized();

        if (user.Role == "Student")
        {
            var profile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);
            if (profile == null) return NotFound("Student profile not found.");
            profile.FirstName = dto.FirstName;
            profile.LastName = dto.LastName;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                user.UserId,
                user.Email,
                user.Role,
                Profile = new
                {
                    profile.StudentProfileId,
                    profile.StudentNumber,
                    profile.FirstName,
                    profile.LastName,
                    profile.ProfileImageUrl
                }
            });
        }

        if (user.Role == "Teacher")
        {
            var profile = await _context.TeacherProfiles
                .FirstOrDefaultAsync(t => t.UserId == userId);
            if (profile == null) return NotFound("Teacher profile not found.");
            profile.FirstName = dto.FirstName;
            profile.LastName = dto.LastName;
            await _context.SaveChangesAsync();
            return Ok(new
            {
                user.UserId,
                user.Email,
                user.Role,
                Profile = new
                {
                    profile.TeacherProfileId,
                    profile.EmployeeNumber,
                    profile.FirstName,
                    profile.LastName,
                    profile.ProfileImageUrl
                }
            });
        }
        return Forbid();
    }

    // POST /api/auth/profile/image - uploads or replaces the signed-in user's picture.
    /// <summary>
    /// Uploads or replaces the logged-in Student or Teacher profile picture.
    /// </summary>
    /// <remarks>
    /// The image is stored in Blob Storage and its URL is saved in SQL Server.
    /// </remarks>
    /// <param name="image">A JPG or PNG image, up to 5 MB.</param>
    /// <returns>The URL of the uploaded profile picture.</returns>
    /// <response code="200">Image uploaded successfully.</response>
    /// <response code="400">Invalid image file.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Profile not found.</response>
    [HttpPost("profile/image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile image,
        [FromServices] Authentication_API.Services.BlobStorageService blobStorage,
        [FromServices] IConfiguration configuration)
    {
        // Identify the user from the API authentication cookie.
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized();

        // Validate the image 
        if (image == null || image.Length == 0)
            return BadRequest("Please select an image.");
        if (image.Length > 5 * 1024 * 1024)
            return BadRequest("Image must be smaller than 5 MB.");

        string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        bool validImage =
            ((extension == ".jpg" || extension == ".jpeg") && image.ContentType == "image/jpeg")
            || (extension == ".png" && image.ContentType == "image/png");
        if (!validImage)
            return BadRequest("Only JPG and PNG images are allowed.");

        // Both roles have a profile image field in the existing database.
        string? oldImageUrl;
        Authentication_API.Models.StudentProfile? student = null;
        Authentication_API.Models.TeacherProfile? teacher = null;
        string? role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Student")
        {
            student = await _context.StudentProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (student == null) return NotFound("Student profile not found.");
            oldImageUrl = student.ProfileImageUrl;
        }
        else if (role == "Teacher")
        {
            teacher = await _context.TeacherProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (teacher == null) return NotFound("Teacher profile not found.");
            oldImageUrl = teacher.ProfileImageUrl;
        }
        else
        {
            return Forbid();
        }

        string containerName = configuration["AzureStorage:ProfileImagesContainer"]
            ?? throw new InvalidOperationException("Profile image container is not configured.");

        // Upload first so that the previous image remains if the upload fails.
        string imageUrl = await blobStorage.UploadImageAsync(image, containerName);
        if (student != null)
            student.ProfileImageUrl = imageUrl;
        else
            teacher!.ProfileImageUrl = imageUrl;

        await _context.SaveChangesAsync();

        // Clean up the old blob when the database now points to the new one.
        if (!string.IsNullOrWhiteSpace(oldImageUrl))
            await blobStorage.DeleteImageAsync(oldImageUrl, containerName);

        return Ok(new { imageUrl });
    }
}
//// Request body for PUT /api/auth/profile. Matches the approved endpoint plan.
//public class UpdateProfileDto
//{
//    [Required]
//    [MaxLength(100)]
//    public string FirstName { get; set; } = null!;

//    [Required]
//    [MaxLength(100)]
//    public string LastName { get; set; } = null!;
//}