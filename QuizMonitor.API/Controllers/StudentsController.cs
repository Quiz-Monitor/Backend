using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Get authenticated student's submitted exams with conditional score visibility
        /// </summary>
        [HttpGet("me/submitted-exams")]
        [ProducesResponseType(typeof(StudentSubmittedExamsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSubmittedExams()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _studentService.GetSubmittedExamsAsync(studentId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to submitted exams: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student submitted exams");
                return StatusCode(500, new { message = "An error occurred while retrieving submitted exams" });
            }
        }

        /// <summary>
        /// Get authenticated student's performance statistics and insights
        /// </summary>
        [HttpGet("me/statistics")]
        [ProducesResponseType(typeof(StudentStatisticsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStudentStatistics()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var statistics = await _studentService.GetStudentStatisticsAsync(studentId);
                return Ok(statistics);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to student statistics: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving student statistics" });
            }
        }

        /// <summary>
        /// Review a graded exam — see your answers alongside the correct answers.
        /// Only available once the attempt status is "graded".
        /// </summary>
        [HttpGet("me/exams/{examId}/review")]
        [ProducesResponseType(typeof(StudentExamReviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReviewExam(int examId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var review = await _studentService.GetExamReviewAsync(examId, studentId);
                return Ok(review);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to exam review: {Message}", ex.Message);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid exam review request: {Message}", ex.Message);
                return ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("not joined", StringComparison.OrdinalIgnoreCase)
                    ? NotFound(new { message = ex.Message })
                    : BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam review");
                return StatusCode(500, new { message = "An error occurred while retrieving the exam review" });
            }
        }
    }
}
