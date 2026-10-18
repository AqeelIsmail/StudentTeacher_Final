using Authentication_API;
using Authentication_API.Models;
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
    [HttpPost("register")]
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
    [HttpPost("login")]
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

    [HttpPost("logout")] //clears the users cookie
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok("Logged out");
    }

    // GET /api/auth/profile - signed-in Student or Teacher views their own profile.
    [HttpGet("profile")]
    [Authorize]
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
    [HttpPut("profile")]
    [Authorize]
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
}
// Request body for PUT /api/auth/profile. Matches the approved endpoint plan.
public class UpdateProfileDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;
}