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
            if (dto.Score.Value < 0)
            {
                throw new InvalidOperationException("Score cannot be negative");
            }

            if (dto.Score.Value > question.Points)
            {
                throw new InvalidOperationException(
                    $"Score ({dto.Score.Value}) cannot exceed maximum points ({question.Points}) for this question");
            }

            // 7. Validate question type (Only open-ended questions can be manually graded)
            if (question.QuestionType.ToLower().StartsWith("mcq"))
            {
                throw new InvalidOperationException("MCQ questions are auto-graded and cannot be manually graded");
            }

            // 8. Update answer record
            answer.Score = dto.Score.Value;
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

        public async Task<StudentWrittenAnswersResponseDto> GetWrittenAnswersAsync(int examId, int studentId, int instructorId)
        {
            // 1. Fetch exam and validate existence
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // 2. Authorization check: Verify instructor owns the exam
            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to view answers for this exam");
            }

            // 3. Find the student's attempt for this exam
            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync(
                a => a.ExamId == examId && a.StudentId == studentId && a.DeletedAt == null);

            if (attempt == null)
            {
                throw new InvalidOperationException("No exam attempt found for this student in this exam");
            }

            // 4. Validate attempt status
            if (attempt.Status != "submitted" && attempt.Status != "graded")
            {
                throw new InvalidOperationException("Exam attempt has not been submitted yet");
            }

            // 5. Fetch the student info
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null)
            {
                throw new InvalidOperationException("Student not found");
            }

            // 6. Get all written (non-MCQ) questions for this exam
            var allQuestions = await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null);

            var writtenQuestions = allQuestions
                .Where(q => !q.QuestionType.ToLower().StartsWith("mcq"))
                .OrderBy(q => q.OrderNumber)
                .ToList();

            // 7. Get all answers for this attempt
            var allAnswers = await _unitOfWork.QuestionAnswers.FindAsync(
                qa => qa.AttemptId == attempt.AttemptId && qa.DeletedAt == null);

            var answersByQuestionId = allAnswers.ToDictionary(a => a.QuestionId, a => a);

            // 8. Build the written answers list
            var writtenAnswerDtos = writtenQuestions.Select(q =>
            {
                answersByQuestionId.TryGetValue(q.QuestionId, out var answer);

                return new WrittenAnswerDto
                {
                    AnswerId = answer?.AnswerId ?? 0,
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionImageUrl = q.QuestionImageUrl,
                    Points = q.Points,
                    OrderNumber = q.OrderNumber,
                    AnswerText = answer?.AnswerText,
                    Score = answer?.Score,
                    InstructorFeedback = answer?.InstructorFeedback,
                    IsManuallyGraded = answer?.IsManuallyGraded ?? false,
                    TimeSpentSeconds = answer?.TimeSpentSeconds
                };
            }).ToList();

            // 9. Build summary
            var gradedCount = writtenAnswerDtos.Count(a => a.Score.HasValue);
            var summary = new WrittenAnswersSummaryDto
            {
                TotalWrittenQuestions = writtenAnswerDtos.Count,
                GradedCount = gradedCount,
                UngradedCount = writtenAnswerDtos.Count - gradedCount,
                TotalWrittenPoints = writtenQuestions.Sum(q => q.Points),
                AwardedPoints = writtenAnswerDtos.Where(a => a.Score.HasValue).Sum(a => a.Score!.Value)
            };

            // 10. Return response
            return new StudentWrittenAnswersResponseDto
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                StudentId = studentId,
                StudentName = student.FullName,
                AttemptId = attempt.AttemptId,
                AttemptStatus = attempt.Status!,
                WrittenAnswers = writtenAnswerDtos,
                Summary = summary
            };
        }

        public async Task<BatchGradeWrittenAnswersResponseDto> BatchGradeWrittenAnswersAsync(
            int examId, int studentId, int instructorId, BatchGradeWrittenAnswersDto dto)
        {
            // 1. Fetch exam and validate existence
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            // 2. Authorization check: Verify instructor owns the exam
            if (exam.InstructorId != instructorId)
            {
                throw new UnauthorizedAccessException("You are not authorized to grade answers for this exam");
            }

            // 3. Find the student's attempt for this exam
            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync(
                a => a.ExamId == examId && a.StudentId == studentId && a.DeletedAt == null);

            if (attempt == null)
            {
                throw new InvalidOperationException("No exam attempt found for this student in this exam");
            }

            // 4. Validate attempt status
            if (attempt.Status != "submitted" && attempt.Status != "graded")
            {
                throw new InvalidOperationException("Cannot grade answers for an exam that has not been submitted");
            }

            // 5. Begin transaction for all-or-nothing grading
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var gradedAnswerDtos = new List<GradedAnswerDto>();

                // 6. Process each grade entry
                foreach (var gradeEntry in dto.Grades)
                {
                    // 6a. Fetch the answer
                    var answer = await _unitOfWork.QuestionAnswers.GetByIdAsync(gradeEntry.AnswerId);
                    if (answer == null || answer.DeletedAt != null)
                    {
                        throw new InvalidOperationException($"Answer with ID {gradeEntry.AnswerId} not found");
                    }

                    // 6b. Validate the answer belongs to this attempt
                    if (answer.AttemptId != attempt.AttemptId)
                    {
                        throw new InvalidOperationException(
                            $"Answer {gradeEntry.AnswerId} does not belong to this student's attempt");
                    }

                    // 6c. Fetch the question
                    var question = await _unitOfWork.Questions.GetByIdAsync(answer.QuestionId);
                    if (question == null || question.DeletedAt != null)
                    {
                        throw new InvalidOperationException(
                            $"Question for answer {gradeEntry.AnswerId} not found");
                    }

                    // 6d. Validate question is non-MCQ
                    if (question.QuestionType.ToLower().StartsWith("mcq"))
                    {
                        throw new InvalidOperationException(
                            $"Answer {gradeEntry.AnswerId} is for an MCQ question and cannot be manually graded");
                    }

                    // 6e. Validate score does not exceed question points
                    if (gradeEntry.Score > question.Points)
                    {
                        throw new InvalidOperationException(
                            $"Score ({gradeEntry.Score}) cannot exceed maximum points ({question.Points}) for answer {gradeEntry.AnswerId}");
                    }

                    // 6f. Update the answer
                    answer.Score = gradeEntry.Score;
                    answer.InstructorFeedback = gradeEntry.Feedback;
                    answer.IsManuallyGraded = true;

                    _unitOfWork.QuestionAnswers.Update(answer);

                    gradedAnswerDtos.Add(new GradedAnswerDto
                    {
                        AnswerId = answer.AnswerId,
                        QuestionId = answer.QuestionId,
                        Score = gradeEntry.Score,
                        Feedback = gradeEntry.Feedback
                    });
                }

                // 7. Recalculate attempt scores
                var allAnswers = await _unitOfWork.QuestionAnswers.FindAsync(
                    qa => qa.AttemptId == attempt.AttemptId && qa.DeletedAt == null);

                var mcqScore = allAnswers
                    .Where(a => a.Score.HasValue && a.IsManuallyGraded != true)
                    .Sum(a => a.Score!.Value);

                var manualScore = allAnswers
                    .Where(a => a.Score.HasValue && a.IsManuallyGraded == true)
                    .Sum(a => a.Score!.Value);

                var finalScore = mcqScore + manualScore;

                // 8. Check if all questions are graded
                var allQuestions = await _unitOfWork.Questions.FindAsync(
                    q => q.ExamId == attempt.ExamId && q.DeletedAt == null);

                var totalQuestions = allQuestions.Count();
                var gradedAnswerCount = allAnswers.Where(a => a.Score.HasValue).Count();
                bool isFullyGraded = (gradedAnswerCount == totalQuestions);

                // 9. Update attempt metadata
                attempt.McqScore = mcqScore;
                attempt.ManualScore = manualScore;
                attempt.FinalScore = finalScore;

                var gradedAt = DateTime.UtcNow;

                if (isFullyGraded)
                {
                    attempt.IsGraded = true;
                    attempt.GradedAt = gradedAt;
                    attempt.GradedBy = instructorId;
                    attempt.Status = "graded";
                }

                _unitOfWork.ExamAttempts.Update(attempt);

                // 10. Save and commit transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // 11. Return response
                return new BatchGradeWrittenAnswersResponseDto
                {
                    ExamId = examId,
                    StudentId = studentId,
                    AttemptId = attempt.AttemptId,
                    GradedAnswers = gradedAnswerDtos,
                    AttemptScoreSummary = new AttemptScoreSummaryDto
                    {
                        McqScore = mcqScore,
                        ManualScore = manualScore,
                        FinalScore = finalScore,
                        IsAttemptFullyGraded = isFullyGraded,
                        AttemptStatus = attempt.Status!
                    },
                    GradedAt = gradedAt
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
