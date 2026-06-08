using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using System.Security.Claims;

namespace QuizMonitor.API.Controllers;

[ApiController]
[Route("api/exams")]
[Produces("application/json")]
[Authorize(Roles = "instructor")]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IQuestionAnswerService _questionAnswerService;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(IExamService examService, IQuestionAnswerService questionAnswerService, ILogger<ExamsController> logger)
    {
        _examService = examService;
        _questionAnswerService = questionAnswerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new exam
    /// </summary>
    /// <param name="dto">Exam creation data</param>
    /// <returns>Created exam details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ExamResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExamResponseDto>> CreateExam([FromBody] CreateExamDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _examService.CreateExamAsync(instructorId, dto);
            return CreatedAtAction(nameof(CreateExam), new { id = response.ExamId }, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized exam creation attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam");
            return StatusCode(500, new { message = "An error occurred while creating the exam" });
        }
    }

    /// <summary>
    /// Publish an exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <returns>Published exam details</returns>
    [HttpPost("{examId}/publish")]
    [ProducesResponseType(typeof(ExamResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExamResponseDto>> PublishExam(int examId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _examService.PublishExamAsync(examId, instructorId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized exam publish attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid exam publish operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing exam");
            return StatusCode(500, new { message = "An error occurred while publishing the exam" });
        }
    }

    /// <summary>
    /// Add a question to an exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <param name="dto">Question data</param>
    /// <returns>Created question details</returns>
    [HttpPost("{examId}/questions")]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionResponseDto>> AddQuestion(int examId, [FromBody] CreateQuestionDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _examService.AddQuestionAsync(examId, instructorId, dto);
            return CreatedAtAction(nameof(AddQuestion), new { examId, questionId = response.QuestionId }, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized question addition attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid question addition operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding question: {Message}", ex.Message);
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            var fullError = ex.InnerException != null
                ? $"{ex.Message} | Inner: {ex.InnerException.Message}"
                : ex.Message;
            return StatusCode(500, new
            {
                message = "An error occurred while adding the question",
                error = fullError,
                innerException = innerMessage,
                stackTrace = ex.StackTrace
            });
        }
    }

    /// <summary>
    /// Update a question in an exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <param name="questionId">Question ID</param>
    /// <param name="dto">Updated question data</param>
    /// <returns>Updated question details</returns>
    [HttpPut("{examId}/questions/{questionId}")]
    [ProducesResponseType(typeof(QuestionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionResponseDto>> UpdateQuestion(int examId, int questionId, [FromBody] UpdateQuestionDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var response = await _examService.UpdateQuestionAsync(examId, questionId, instructorId, dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized question update attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid question update operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question");
            return StatusCode(500, new { message = "An error occurred while updating the question" });
        }
    }

    /// <summary>
    /// Remove a question from an exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <param name="questionId">Question ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{examId}/questions/{questionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveQuestion(int examId, int questionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            await _examService.RemoveQuestionAsync(examId, questionId, instructorId);
            return Ok(new
            {
                message = "Question deleted successfully",
                questionId,
                examId
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized question deletion attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid question deletion operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing question");
            return StatusCode(500, new { message = "An error occurred while removing the question" });
        }
    }

    /// <summary>
    /// Get exam results for all students
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <returns>List of student results</returns>
    [HttpGet("{examId}/results")]
    [ProducesResponseType(typeof(List<StudentExamResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StudentExamResultDto>>> GetExamResults(int examId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var results = await _examService.GetExamResultsAsync(examId, instructorId);
            return Ok(results);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized exam results access attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid exam results operation");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving exam results");
            return StatusCode(500, new { message = "An error occurred while retrieving exam results" });
        }
    }

   

    /// <summary>
    /// Get all exams for the instructor
    /// </summary>
    /// <returns>List of instructor exams</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<InstructorExamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<InstructorExamDto>>> GetInstructorExams()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var exams = await _examService.GetInstructorExamsAsync(instructorId);
            return Ok(exams);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized instructor exams access attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid instructor exams operation ");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instructor exams");
            return StatusCode(500, new { message = "An error occurred while retrieving instructor exams" });
        }
    }

    /// <summary>
    /// Get all written (short-answer) questions and student answers for a specific student in a specific exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <param name="studentId">Student ID</param>
    /// <returns>Written answers with grading summary</returns>
    [HttpGet("{examId}/students/{studentId}/written-answers")]
    [ProducesResponseType(typeof(StudentWrittenAnswersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StudentWrittenAnswersResponseDto>> GetWrittenAnswers(int examId, int studentId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var result = await _questionAnswerService.GetWrittenAnswersAsync(examId, studentId, instructorId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized written answers access attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid written answers operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving written answers");
            return StatusCode(500, new { message = "An error occurred while retrieving written answers" });
        }
    }

    /// <summary>
    /// Batch grade written (short-answer) questions for a specific student in a specific exam
    /// </summary>
    /// <param name="examId">Exam ID</param>
    /// <param name="studentId">Student ID</param>
    /// <param name="dto">Batch grading data</param>
    /// <returns>Graded answers and updated attempt scores</returns>
    [HttpPost("{examId}/students/{studentId}/written-answers/grade")]
    [ProducesResponseType(typeof(BatchGradeWrittenAnswersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BatchGradeWrittenAnswersResponseDto>> BatchGradeWrittenAnswers(
        int examId, int studentId, [FromBody] BatchGradeWrittenAnswersDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int instructorId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var result = await _questionAnswerService.BatchGradeWrittenAnswersAsync(examId, studentId, instructorId, dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized batch grading attempt");
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid batch grading operation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch grading written answers");
            return StatusCode(500, new { message = "An error occurred while grading written answers" });
        }
    }

}
