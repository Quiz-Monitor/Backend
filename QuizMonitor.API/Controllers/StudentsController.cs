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
    [Route("api/students")]
    [Authorize(Roles = "student")]
    [Produces("application/json")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        /// <summary>
        /// Get authenticated student's exam results
        /// </summary>
        [HttpGet("me/results")]
        [ProducesResponseType(typeof(List<StudentExamResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyExamResults()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var results = await _studentService.GetMyExamResultsAsync(studentId);
                return Ok(results);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to exam results: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student exam results");
                return StatusCode(500, new { message = "An error occurred while retrieving exam results" });
            }
        }

        /// <summary>
        /// Get authenticated student's available exams
        /// </summary>
        [HttpGet("me/exams")]
        [ProducesResponseType(typeof(List<StudentExamResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyAvailableExams()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var exams = await _studentService.GetAvailableExamsForStudentAsync(studentId);
                return Ok(exams);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to student exams: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available exams for student");
                return StatusCode(500, new { message = "An error occurred while retrieving available exams" });
            }
        }
    }
}
