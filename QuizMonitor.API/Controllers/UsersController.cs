using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly QuizMonitorDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(QuizMonitorDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => u.DeletedAt == null) // Only get non-deleted users
                .Select(u => new UserResponseDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    PhoneNumber = u.PhoneNumber,
                    ProfilePicture = u.ProfilePicture,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            // Validate role
            var validRoles = new[] { "instructor", "student", "admin" };
            if (!validRoles.Contains(createUserDto.Role.ToLower()))
            {
                return BadRequest(new { message = "Invalid role. Must be 'instructor', 'student', or 'admin'" });
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == createUserDto.Email && u.DeletedAt == null);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Create new user
            var user = new User
            {
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password), // Hash the password
                FullName = createUserDto.FullName,
                Role = createUserDto.Role.ToLower(),
                PhoneNumber = createUserDto.PhoneNumber,
                ProfilePicture = createUserDto.ProfilePicture,
                CreatedAt = DateTime.Now, // Use DateTime.Now for PostgreSQL timestamp without timezone
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUserById(int id)
    {
        try
        {
            var user = await _context.Users
                .Where(u => u.UserId == id && u.DeletedAt == null)
                .Select(u => new UserResponseDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    PhoneNumber = u.PhoneNumber,
                    ProfilePicture = u.ProfilePicture,
                    LastLogin = u.LastLogin,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user");
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }
}
