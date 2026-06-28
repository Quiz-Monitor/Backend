using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        // [HttpPost("exam-attempts/start")]
        // [Authorize(Roles = "student")]
        // public async Task<IActionResult> StartExam([FromBody] StartExamDto dto)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //         if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
        //         {
        //             return Unauthorized(new { message = "Invalid user token" });
        //         }

        //         var result = await _examAttemptService.StartExamAsync(studentId, dto);
        //         return Ok(result);
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         _logger.LogWarning(ex, "Failed to start exam: {Message}", ex.Message);
        //         return BadRequest(new { message = ex.Message });
        //     }
        //     catch (UnauthorizedAccessException ex)
        //     {
        //         _logger.LogWarning(ex, "Unauthorized start exam attempt: {Message}", ex.Message);
        //         return Forbid();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error starting exam");
        //         return StatusCode(500, new { message = "An error occurred while starting the exam" });
        //     }
        // }

        /// <summary>
        /// Get question by order number
        /// </summary>
        [HttpGet("exam-attempts/{attemptId}/questions/{orderNumber}")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> GetQuestion(int attemptId, int orderNumber)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _examAttemptService.GetQuestionByOrderAsync(attemptId, studentId, orderNumber);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to get question: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to question: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question");
                return StatusCode(500, new { message = "An error occurred while retrieving the question" });
            }
        }

        /// <summary>
        /// Save answer (auto-save)
        /// </summary>
        // [HttpPost("exam-attempts/{attemptId}/answers")]
        // [Authorize(Roles = "student")]
        // public async Task<IActionResult> SaveAnswer(int attemptId, [FromBody] SaveAnswerDto dto)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //         if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
        //         {
        //             return Unauthorized(new { message = "Invalid user token" });
        //         }

        //         var result = await _examAttemptService.SaveAnswerAsync(attemptId, studentId, dto);
        //         return Ok(result);
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         _logger.LogWarning(ex, "Failed to save answer: {Message}", ex.Message);
        //         return BadRequest(new { message = ex.Message });
        //     }
        //     catch (UnauthorizedAccessException ex)
        //     {
        //         _logger.LogWarning(ex, "Unauthorized save answer: {Message}", ex.Message);
        //         return Forbid();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error saving answer");
        //         return StatusCode(500, new { message = "An error occurred while saving the answer" });
        //     }
        // }

        /// <summary>
        /// Log violation event
        /// </summary>
        [HttpPost("exam-attempts/{attemptId}/violations")]
        [Authorize(Roles = "student")]
        public async Task<IActionResult> LogViolation(int attemptId, [FromBody] LogViolationDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _examAttemptService.LogViolationAsync(attemptId, studentId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to log violation: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized log violation: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging violation");
                return StatusCode(500, new { message = "An error occurred while logging the violation" });
            }
        }

        /// <summary>
        /// Submit exam
        /// </summary>
        // [HttpPost("exam-attempts/{attemptId}/submit")]
        // [Authorize(Roles = "student")]
        // public async Task<IActionResult> SubmitExam(int attemptId)
        // {
        //     try
        //     {
        //         var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //         if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
        //         {
        //             return Unauthorized(new { message = "Invalid user token" });
        //         }

        //         var result = await _examAttemptService.SubmitExamAsync(attemptId, studentId);
        //         return Ok(result);
        //     }
        //     catch (InvalidOperationException ex)
        //     {
        //         _logger.LogWarning(ex, "Failed to submit exam: {Message}", ex.Message);
        //         return BadRequest(new { message = ex.Message });
        //     }
        //     catch (UnauthorizedAccessException ex)
        //     {
        //         _logger.LogWarning(ex, "Unauthorized submit exam: {Message}", ex.Message);
        //         return Forbid();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error submitting exam");
        //         return StatusCode(500, new { message = "An error occurred while submitting the exam" });
        //     }
        // }

        /// <summary>
        /// Start exam + get ALL questions in one call.
        /// If attempt is "waiting" the exam is started (waiting → in_progress) before questions are returned.
        /// If attempt is already "in_progress" questions are returned as-is (handles reconnection).
        /// Choices never expose isCorrect to the student.
        /// </summary>
        [HttpGet("exam-attempts/{attemptId}/questions")]
        [Authorize(Roles = "student")]
        [ProducesResponseType(typeof(ExamQuestionsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllQuestions(int attemptId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                    return Unauthorized(new { message = "Invalid user token" });

                var result = await _examAttemptService.GetAllQuestionsAsync(attemptId, studentId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to get all questions: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to all questions: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all questions");
                return StatusCode(500, new { message = "An error occurred while retrieving questions" });
            }
        }

        /// <summary>
        /// Save ALL answers + submit the exam in one call.
        /// Answers are upserted in a single DB transaction, then the exam is submitted automatically.
        /// Response includes per-answer scores AND the full submit result
        /// (gradingStatus: "auto_graded" | "pending_manual_grading").
        /// </summary>
        [HttpPost("exam-attempts/{attemptId}/answers/bulk")]
        [Authorize(Roles = "student")]
        [ProducesResponseType(typeof(BulkSaveAnswersResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BulkSaveAnswers(int attemptId, [FromBody] BulkSaveAnswersDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int studentId))
                    return Unauthorized(new { message = "Invalid user token" });

                var result = await _examAttemptService.BulkSaveAnswersAsync(attemptId, studentId, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to bulk save answers: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized bulk save answers: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk saving answers");
                return StatusCode(500, new { message = "An error occurred while saving answers" });
            }
        }

        /// <summary>
        /// Get detailed report of a student's exam attempt (instructor only)
        /// </summary>
        [HttpGet("exam-attempts/{attemptId}/details")]
        [Authorize(Roles = "instructor")]
        [ProducesResponseType(typeof(ExamAttemptDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetExamAttemptDetails(int attemptId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _examAttemptService.GetExamAttemptDetailsAsync(attemptId, instructorId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to get exam attempt details: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to exam attempt details: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam attempt details");
                return StatusCode(500, new { message = "An error occurred while retrieving exam attempt details" });
            }
        }

    }
}
