using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;

namespace QuizMonitor.API.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    [Produces("application/json")]
    
    public class ExamAttemptsController : ControllerBase
    {
        private readonly IExamAttemptService _examAttemptService;
        private readonly ILogger<ExamAttemptsController> _logger;

        public ExamAttemptsController(IExamAttemptService examAttemptService, ILogger<ExamAttemptsController> logger)
        {
            _examAttemptService = examAttemptService;
            _logger = logger;
        }


        /// <summary>
        /// Join an exam by code (creates WAITING attempt)
        /// </summary>
        [HttpPost("exams/join")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> JoinExam([FromBody] JoinExamDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _examAttemptService.JoinExamAsync(studentId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to join exam: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining exam");
                return StatusCode(500, new { message = "An error occurred while joining the exam" });
            }
        }

        /// <summary>
        /// Start exam attempt (transitions WAITING → ACTIVE)
        /// </summary>
        [HttpPost("exam-attempts/start")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> StartExam([FromBody] StartExamDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _examAttemptService.StartExamAsync(studentId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to start exam: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized start exam attempt: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting exam");
                return StatusCode(500, new { message = "An error occurred while starting the exam" });
            }
        }


    }
}