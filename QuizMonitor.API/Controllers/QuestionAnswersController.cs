using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using System.Security.Claims;

namespace QuizMonitor.API.Controllers
{
    [ApiController]
    [Route("api/answers")]
    [Produces("application/json")]
    [Authorize(Roles = "instructor")]
    public class QuestionAnswersController : ControllerBase
    {
        private readonly IQuestionAnswerService _questionAnswerService;
        private readonly ILogger<QuestionAnswersController> _logger;

        public QuestionAnswersController(IQuestionAnswerService questionAnswerService, ILogger<QuestionAnswersController> logger)
        {
            _questionAnswerService = questionAnswerService;
            _logger = logger;
        }

        /// <summary>
        /// Grade a student's answer for an open-ended question
        /// </summary>
        /// <param name="answerId">Answer ID</param>
        /// <param name="dto">Grading data (score and feedback)</param>
        /// <returns>Graded answer details and updated attempt scores</returns>
        [HttpPost("{answerId}/grade")]
        [ProducesResponseType(typeof(GradeAnswerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<GradeAnswerResponseDto>> GradeAnswer(int answerId, [FromBody] GradeAnswerDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var response = await _questionAnswerService.GradeAnswerAsync(answerId, instructorId, dto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized answer grading attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid answer grading operation");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grading answer");
                return StatusCode(500, new { message = "An error occurred while grading the answer" });
            }
        }

    }
}
