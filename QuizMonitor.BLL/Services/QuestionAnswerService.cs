using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;

namespace QuizMonitor.BLL.Services
{
    public class QuestionAnswerService : IQuestionAnswerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuestionAnswerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GradeAnswerResponseDto> GradeAnswerAsync(int answerId, int instructorId, GradeAnswerDto dto)
        {
            // 1. Fetch answer and validate existence
            var answer = await _unitOfWork.QuestionAnswers.GetByIdAsync(answerId);

            if (answer == null || answer.DeletedAt != null)
            {
                throw new InvalidOperationException("Answer not found");
            }

            // 2. Fetch related entities for authorization and validation
            var attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(answer.AttemptId);
            if (attempt == null || attempt.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam attempt not found");
            }

            var exam = await _unitOfWork.Exams.GetByIdAsync(attempt.ExamId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // 3. Authorization check: Verify instructor owns the exam
            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to grade answers for this exam");
            }

            // 4. Validate exam attempt status
            if (attempt.Status != "submitted")
            {
                throw new InvalidOperationException("Cannot grade answers for an exam that has not been submitted");
            }

            // 5. Fetch question to validate score
            var question = await _unitOfWork.Questions.GetByIdAsync(answer.QuestionId);
            if (question == null || question.DeletedAt != null)
            {
                throw new InvalidOperationException("Question not found");
            }

            // 6. Validate score does not exceed question points
            if (dto.Score < 0)
            {
                throw new InvalidOperationException("Score cannot be negative");
            }

            if (dto.Score > question.Points)
            {
                throw new InvalidOperationException(
                    $"Score ({dto.Score}) cannot exceed maximum points ({question.Points}) for this question");
            }

            // 7. Validate question type (Only open-ended questions can be manually graded)
            if (question.QuestionType.ToLower().StartsWith("mcq"))
            {
                throw new InvalidOperationException("MCQ questions are auto-graded and cannot be manually graded");
            }

            // 8. Update answer record
            answer.Score = dto.Score;
            answer.InstructorFeedback = dto.Feedback;
            answer.IsManuallyGraded = true;

            _unitOfWork.QuestionAnswers.Update(answer);

            // 9. Recalculate exam attempt scores
            // Get all answers for this attempt
            var allAnswers = await _unitOfWork.QuestionAnswers.FindAsync(
                qa => qa.AttemptId == attempt.AttemptId && qa.DeletedAt == null);

            // Calculate MCQ score (auto-graded answers)
            var mcqScore = allAnswers
                .Where(a => a.Score.HasValue && a.IsManuallyGraded != true)
                .Sum(a => a.Score.Value);

            // Calculate Manual score (manually graded answers)
            var manualScore = allAnswers
                .Where(a => a.Score.HasValue && a.IsManuallyGraded == true)
                .Sum(a => a.Score.Value);

            // Calculate Final score
            var finalScore = mcqScore + manualScore;

            // 10. Check if all answers are graded
            var questions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == attempt.ExamId && q.DeletedAt == null);

            var totalQuestions = questions.Count();
            var gradedAnswers = allAnswers.Where(a => a.Score.HasValue).Count();

            bool isFullyGraded = (gradedAnswers == totalQuestions);

            // 11. Update attempt metadata
            attempt.McqScore = mcqScore;
            attempt.ManualScore = manualScore;
            attempt.FinalScore = finalScore;

            if (isFullyGraded)
            {
                attempt.IsGraded = true;
                attempt.GradedAt = DateTime.UtcNow;
                attempt.GradedBy = instructorId;
                attempt.Status = "graded";
            }

            _unitOfWork.ExamAttempts.Update(attempt);

            // 12. Save changes
            await _unitOfWork.SaveChangesAsync();

            // 13. Return response
            return new GradeAnswerResponseDto
            {
                AnswerId = answer.AnswerId,
                QuestionId = answer.QuestionId,
                AttemptId = answer.AttemptId,
                Score = answer.Score.Value,
                Feedback = answer.InstructorFeedback,
                GradedAt = attempt.GradedAt ?? DateTime.UtcNow,
                GradedBy = instructorId,
                McqScore = attempt.McqScore,
                ManualScore = attempt.ManualScore,
                FinalScore = attempt.FinalScore,
                IsAttemptFullyGraded = isFullyGraded
            };
        }
    }
}
