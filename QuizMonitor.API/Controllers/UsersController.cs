using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.DAL.Data;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;
using System.Security.Claims;

namespace QuizMonitor.API.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUnitOfWork unitOfWork, ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    /// <returns>Current user profile information</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfoDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDTO>> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            if (user == null || user.DeletedAt != null)
            {
                return NotFound(new { message = "User not found" });
            }

            var response = new UserInfoDTO
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { message = "An error occurred while retrieving the profile" });
        }
    }
}
