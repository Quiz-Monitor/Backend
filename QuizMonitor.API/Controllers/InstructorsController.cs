using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;

namespace QuizMonitor.API.Controllers
{
    [ApiController]
    [Route("api/instructors")]
    [Authorize(Roles = "instructor")]
    [Produces("application/json")]
    public class InstructorsController : ControllerBase
    {
        private readonly IInstructorService _instructorService;
        private readonly ILogger<InstructorsController> _logger;

        public InstructorsController(IInstructorService instructorService, ILogger<InstructorsController> logger)
        {
            _instructorService = instructorService;
            _logger = logger;
        }

        /// <summary>
        /// Get authenticated instructor's performance statistics and insights
        /// </summary>
        [HttpGet("me/statistics")]
        [ProducesResponseType(typeof(InstructorStatisticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInstructorStatistics()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var statistics = await _instructorService.GetInstructorStatisticsAsync(instructorId);
                return Ok(statistics);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to instructor statistics: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instructor statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving instructor statistics" });
            }
        }
    }
}
