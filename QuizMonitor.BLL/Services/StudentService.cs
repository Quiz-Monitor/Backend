using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;

namespace QuizMonitor.BLL.Services
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<StudentExamResultResponseDto>> GetMyExamResultsAsync(int studentId)
        {
            // Verify student exists
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only students can access this endpoint");
            }

            // get all exam attempt for this student with status SUBMITTED or GRADED
            var validStatuses = new[] { "submitted", "graded" };
            var examAttempts = await _unitOfWork.ExamAttempts.FindAsync(ea =>
                ea.StudentId == studentId &&
                ea.DeletedAt == null &&
                validStatuses.Contains(ea.Status));

            if (!examAttempts.Any())
            {
                return new List<StudentExamResultResponseDto>();
            }

            // Batch fetch all required exams to avoid N+1 problem
            var examIds = examAttempts.Select(ea => ea.ExamId).Distinct().ToList();
            var exams = await _unitOfWork.Exams.FindAsync(e => examIds.Contains(e.ExamId) && e.DeletedAt == null);
            var examDictionary = exams.ToDictionary(e => e.ExamId);

            // Batch fetch questions and compute total points per exam.
            var questions = await _unitOfWork.Questions.FindAsync(q =>
                examIds.Contains(q.ExamId) &&
                q.DeletedAt == null);
            var examTotalPointsDictionary = questions
                .GroupBy(q => q.ExamId)
                .ToDictionary(g => g.Key, g => g.Sum(q => q.Points));

            var results = new List<StudentExamResultResponseDto>();

            foreach (var attempt in examAttempts)
            {
                if (examDictionary.TryGetValue(attempt.ExamId, out var exam))
                {
                    var resultDto = new StudentExamResultResponseDto
                    {
                        ExamTitle = exam.Title,
                        SubmitTime = attempt.SubmitTime,
                        ExamTotalPoints = examTotalPointsDictionary.TryGetValue(attempt.ExamId, out var totalPoints)
                            ? totalPoints
                            : 0m
                    };

                    if (string.Equals(attempt.Status, "graded", StringComparison.OrdinalIgnoreCase))
                    {
                        resultDto.Status = "Graded";
                        resultDto.FinalScore = attempt.FinalScore;
                    }
                    else // SUBMITTED
                    {
                        resultDto.Status = "Pending";
                        resultDto.FinalScore = null;
                    }

                    results.Add(resultDto);
                }
            }

            return results;
        }

        public async Task<List<StudentExamResponseDto>> GetAvailableExamsForStudentAsync(int studentId)
        {
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only students can access this endpoint");
            }

            var examAttempts = (await _unitOfWork.ExamAttempts.FindAsync(ea =>
                ea.StudentId == studentId &&
                ea.DeletedAt == null)).ToList();//

            if (!examAttempts.Any())
            {
                return new List<StudentExamResponseDto>();
            }

            var examIds = examAttempts.Select(ea => ea.ExamId).Distinct().ToList();

            var exams = (await _unitOfWork.Exams.FindAsync(e =>
                examIds.Contains(e.ExamId) &&
                e.DeletedAt == null)).ToList();
            var examDictionary = exams.ToDictionary(e => e.ExamId);

            var questions = await _unitOfWork.Questions.FindAsync(q =>
                examIds.Contains(q.ExamId) &&
                q.DeletedAt == null);
            var questionCountByExamId = questions
                .GroupBy(q => q.ExamId)
                .ToDictionary(g => g.Key, g => g.Count());

            var instructorIds = exams.Select(e => e.InstructorId).Distinct().ToList();
            var instructors = await _unitOfWork.Users.FindAsync(u =>
                instructorIds.Contains(u.UserId) &&
                u.DeletedAt == null);
            var instructorDictionary = instructors.ToDictionary(u => u.UserId, u => u.FullName);

            var response = new List<StudentExamResponseDto>();

            foreach (var attempt in examAttempts)
            {
                if (!examDictionary.TryGetValue(attempt.ExamId, out var exam))
                {
                    continue;
                }

                response.Add(new StudentExamResponseDto
                {
                    ExamId = exam.ExamId,
                    ExamTitle = exam.Title,
                    ExamCode = exam.ExamCode,
                    StartTime = exam.StartTime,
                    EndTime = exam.EndTime,
                    DurationMinutes = exam.DurationMinutes,
                    QuestionCount = questionCountByExamId.TryGetValue(attempt.ExamId, out var questionCount)
                        ? questionCount
                        : 0,
                    InstructorName = instructorDictionary.TryGetValue(exam.InstructorId, out var instructorName)
                        ? instructorName
                        : string.Empty,
                    ExamStatus = attempt.Status ?? string.Empty
                });
            }

            return response;
        }

        public async Task<StudentSubmittedExamsResponseDto> GetSubmittedExamsAsync(int studentId)
        {
            // Verify student exists
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only students can access this endpoint");
            }

            // Get all submitted/graded attempts
            var validStatuses = new[] { "submitted", "graded" };
            var examAttempts = (await _unitOfWork.ExamAttempts.FindAsync(ea =>
                ea.StudentId == studentId &&
                ea.DeletedAt == null &&
                validStatuses.Contains(ea.Status)))
                .OrderByDescending(ea => ea.SubmitTime)
                .ToList();

            if (!examAttempts.Any())
            {
                return new StudentSubmittedExamsResponseDto
                {
                    TotalExams = 0,
                    Exams = new List<SubmittedExamDto>()
                };
            }

            // Batch fetch exams
            var examIds = examAttempts.Select(ea => ea.ExamId).Distinct().ToList();
            var exams = await _unitOfWork.Exams.FindAsync(e => examIds.Contains(e.ExamId) && e.DeletedAt == null);
            var examDictionary = exams.ToDictionary(e => e.ExamId);

            // Batch fetch questions for total points and question count
            var questions = await _unitOfWork.Questions.FindAsync(q =>
                examIds.Contains(q.ExamId) &&
                q.DeletedAt == null);
            var questionsByExam = questions.GroupBy(q => q.ExamId)
                .ToDictionary(g => g.Key, g => new { Count = g.Count(), TotalPoints = g.Sum(q => q.Points) });

            // Batch fetch instructors
            var instructorIds = exams.Select(e => e.InstructorId).Distinct().ToList();
            var instructors = await _unitOfWork.Users.FindAsync(u =>
                instructorIds.Contains(u.UserId) &&
                u.DeletedAt == null);
            var instructorDictionary = instructors.ToDictionary(u => u.UserId, u => u.FullName);

            var submittedExams = new List<SubmittedExamDto>();

            foreach (var attempt in examAttempts)
            {
                if (!examDictionary.TryGetValue(attempt.ExamId, out var exam))
                {
                    continue;
                }

                var questionInfo = questionsByExam.TryGetValue(attempt.ExamId, out var qi)
                    ? qi
                    : new { Count = 0, TotalPoints = 0m };

                var isGraded = string.Equals(attempt.Status, "graded", StringComparison.OrdinalIgnoreCase);

                submittedExams.Add(new SubmittedExamDto
                {
                    AttemptId = attempt.AttemptId,
                    ExamId = exam.ExamId,
                    ExamTitle = exam.Title,
                    ExamCode = exam.ExamCode,
                    InstructorName = instructorDictionary.TryGetValue(exam.InstructorId, out var name) ? name : string.Empty,
                    SubmitTime = attempt.SubmitTime,
                    DurationMinutes = exam.DurationMinutes,
                    TimeSpentSeconds = attempt.TotalDurationSeconds,
                    QuestionCount = questionInfo.Count,
                    ExamTotalPoints = questionInfo.TotalPoints,
                    GradingStatus = isGraded ? "graded" : "pending",
                    McqScore = isGraded ? attempt.McqScore : null,
                    ManualScore = isGraded ? attempt.ManualScore : null,
                    FinalScore = isGraded ? attempt.FinalScore : null,
                    ScorePercentage = isGraded && attempt.FinalScore.HasValue && questionInfo.TotalPoints > 0
                        ? Math.Round((attempt.FinalScore.Value / questionInfo.TotalPoints) * 100m, 1)
                        : null,
                    TotalViolations = attempt.TotalViolations ?? 0,
                    CheatingStatus = attempt.CheatingStatus ?? "clean"
                });
            }

            return new StudentSubmittedExamsResponseDto
            {
                TotalExams = submittedExams.Count,
                Exams = submittedExams
            };
        }

        public async Task<StudentStatisticsResponseDto> GetStudentStatisticsAsync(int studentId)
        {
            // Verify student exists
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only students can access this endpoint");
            }

            // Get all submitted/graded attempts
            var validStatuses = new[] { "submitted", "graded" };
            var examAttempts = (await _unitOfWork.ExamAttempts.FindAsync(ea =>
                ea.StudentId == studentId &&
                ea.DeletedAt == null &&
                validStatuses.Contains(ea.Status))).ToList();

            var response = new StudentStatisticsResponseDto();

            // === Overview ===
            var gradedCount = examAttempts.Count(a => string.Equals(a.Status, "graded", StringComparison.OrdinalIgnoreCase));
            var pendingCount = examAttempts.Count(a => string.Equals(a.Status, "submitted", StringComparison.OrdinalIgnoreCase));
            response.Overview = new StudentOverviewDto
            {
                TotalExamsSubmitted = examAttempts.Count,
                TotalExamsGraded = gradedCount,
                TotalExamsPendingGrading = pendingCount
            };

            if (!examAttempts.Any())
            {
                return response;
            }

            // Batch fetch exams and questions for total points
            var examIds = examAttempts.Select(ea => ea.ExamId).Distinct().ToList();
            var exams = await _unitOfWork.Exams.FindAsync(e => examIds.Contains(e.ExamId) && e.DeletedAt == null);
            var examDictionary = exams.ToDictionary(e => e.ExamId);

            var questions = await _unitOfWork.Questions.FindAsync(q =>
                examIds.Contains(q.ExamId) &&
                q.DeletedAt == null);
            var examTotalPointsDict = questions
                .GroupBy(q => q.ExamId)
                .ToDictionary(g => g.Key, g => g.Sum(q => q.Points));

            // === Score Statistics (graded attempts only) ===
            var gradedAttempts = examAttempts
                .Where(a => string.Equals(a.Status, "graded", StringComparison.OrdinalIgnoreCase) && a.FinalScore.HasValue)
                .ToList();

            if (gradedAttempts.Any())
            {
                var attemptPercentages = gradedAttempts
                    .Where(a => examTotalPointsDict.ContainsKey(a.ExamId) && examTotalPointsDict[a.ExamId] > 0)
                    .Select(a => new
                    {
                        a.ExamId,
                        a.FinalScore,
                        TotalPoints = examTotalPointsDict[a.ExamId],
                        Percentage = (a.FinalScore!.Value / examTotalPointsDict[a.ExamId]) * 100m
                    })
                    .ToList();

                if (attemptPercentages.Any())
                {
                    response.ScoreStatistics.AverageScorePercentage = Math.Round(attemptPercentages.Average(a => a.Percentage), 1);
                    response.ScoreStatistics.HighestScorePercentage = Math.Round(attemptPercentages.Max(a => a.Percentage), 1);
                    response.ScoreStatistics.LowestScorePercentage = Math.Round(attemptPercentages.Min(a => a.Percentage), 1);

                    var highest = attemptPercentages.OrderByDescending(a => a.Percentage).First();
                    var lowest = attemptPercentages.OrderBy(a => a.Percentage).First();

                    response.ScoreStatistics.HighestScoringExam = new ExamScoreInfoDto
                    {
                        ExamTitle = examDictionary.TryGetValue(highest.ExamId, out var hExam) ? hExam.Title : "Unknown",
                        ScorePercentage = Math.Round(highest.Percentage, 1),
                        FinalScore = highest.FinalScore!.Value,
                        ExamTotalPoints = highest.TotalPoints
                    };

                    response.ScoreStatistics.LowestScoringExam = new ExamScoreInfoDto
                    {
                        ExamTitle = examDictionary.TryGetValue(lowest.ExamId, out var lExam) ? lExam.Title : "Unknown",
                        ScorePercentage = Math.Round(lowest.Percentage, 1),
                        FinalScore = lowest.FinalScore!.Value,
                        ExamTotalPoints = lowest.TotalPoints
                    };
                }
            }

            // === Integrity Statistics (all submitted attempts) ===
            var totalViolations = examAttempts.Sum(a => a.TotalViolations ?? 0);
            response.IntegrityStatistics = new StudentIntegrityStatisticsDto
            {
                TotalViolationsAcrossAllExams = totalViolations,
                AverageViolationsPerExam = examAttempts.Any()
                    ? Math.Round((decimal)totalViolations / examAttempts.Count, 2)
                    : 0m,
                CleanExams = examAttempts.Count(a => string.Equals(a.CheatingStatus, "clean", StringComparison.OrdinalIgnoreCase)),
                WarningExams = examAttempts.Count(a => string.Equals(a.CheatingStatus, "warning", StringComparison.OrdinalIgnoreCase)),
                FlaggedExams = examAttempts.Count(a => string.Equals(a.CheatingStatus, "flagged", StringComparison.OrdinalIgnoreCase))
            };

            // === Time Statistics (all submitted attempts) ===
            var totalTimeSpent = examAttempts.Sum(a => a.TotalDurationSeconds ?? 0);
            response.TimeStatistics = new StudentTimeStatisticsDto
            {
                TotalTimeSpentSeconds = totalTimeSpent,
                AverageTimeSpentSeconds = examAttempts.Any()
                    ? totalTimeSpent / examAttempts.Count
                    : 0
            };

            return response;
        }

        public async Task<StudentExamReviewDto> GetExamReviewAsync(int examId, int studentId)
        {
            var student = await _unitOfWork.Users.GetByIdAsync(studentId);
            if (student == null || !string.Equals(student.Role, "student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only students can access this endpoint");
            }

            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);
            if (exam == null || exam.DeletedAt != null)
            {
                throw new InvalidOperationException("Exam not found");
            }

            var attempt = await _unitOfWork.ExamAttempts.FirstOrDefaultAsync(
                a => a.ExamId == examId && a.StudentId == studentId && a.DeletedAt == null);

            if (attempt == null)
            {
                throw new InvalidOperationException("You have not joined this exam");
            }

            if (!string.Equals(attempt.Status, "graded", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Exam has not been graded yet");
            }

            // Load questions in order
            var questions = (await _unitOfWork.Questions.FindAsync(
                q => q.ExamId == examId && q.DeletedAt == null))
                .OrderBy(q => q.OrderNumber)
                .ToList();

            // Load this attempt's answers
            var answers = (await _unitOfWork.QuestionAnswers.FindAsync(
                qa => qa.AttemptId == attempt.AttemptId && qa.DeletedAt == null))
                .ToList();
            var answerDict = answers.ToDictionary(a => a.QuestionId);

            // Batch-load all choices for these questions
            var questionIds = questions.Select(q => q.QuestionId).ToList();
            var allChoices = (await _unitOfWork.Choices.FindAsync(
                c => questionIds.Contains(c.QuestionId)))
                .ToList();
            var choicesByQuestion = allChoices
                .GroupBy(c => c.QuestionId)
                .ToDictionary(g => g.Key, g => g.OrderBy(c => c.OrderNumber).ToList());

            var examTotalPoints = questions.Sum(q => q.Points);
            var reviewQuestions = new List<ReviewQuestionDto>();

            foreach (var q in questions)
            {
                answerDict.TryGetValue(q.QuestionId, out var answer);
                var isMcq = q.QuestionType.ToLower().StartsWith("mcq");

                List<ReviewChoiceDto>? reviewChoices = null;
                string? studentAnswerText = null;
                string? correctAnswerText = null;
                bool? isCorrect = null;
                string? feedback = null;

                if (isMcq)
                {
                    choicesByQuestion.TryGetValue(q.QuestionId, out var choices);
                    choices ??= new List<DAL.Models.Choice>();

                    var selectedIds = new List<int>();
                    if (!string.IsNullOrEmpty(answer?.SelectedChoices))
                    {
                        try
                        {
                            selectedIds = JsonSerializer.Deserialize<List<int>>(answer.SelectedChoices) ?? new List<int>();
                        }
                        catch (JsonException)
                        {
                            selectedIds = new List<int>();
                        }
                    }

                    reviewChoices = choices.Select(c => new ReviewChoiceDto
                    {
                        ChoiceId = c.ChoiceId,
                        Text = c.ChoiceText,
                        IsCorrect = c.IsCorrect ?? false,
                        IsSelected = selectedIds.Contains(c.ChoiceId)
                    }).ToList();

                    studentAnswerText = string.Join(", ",
                        choices.Where(c => selectedIds.Contains(c.ChoiceId)).Select(c => c.ChoiceText));
                    correctAnswerText = string.Join(", ",
                        choices.Where(c => c.IsCorrect == true).Select(c => c.ChoiceText));
                    isCorrect = answer?.IsCorrect;
                }
                else
                {
                    studentAnswerText = answer?.AnswerText;
                    feedback = answer?.InstructorFeedback;
                }

                reviewQuestions.Add(new ReviewQuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Points = q.Points,
                    EarnedPoints = answer?.Score,
                    OrderNumber = q.OrderNumber,
                    StudentAnswer = studentAnswerText,
                    CorrectAnswer = correctAnswerText,
                    IsCorrect = isCorrect,
                    InstructorFeedback = feedback,
                    Choices = reviewChoices
                });
            }

            decimal? scorePercentage = null;
            if (attempt.FinalScore.HasValue && examTotalPoints > 0)
            {
                scorePercentage = Math.Round((attempt.FinalScore.Value / examTotalPoints) * 100, 1);
            }

            return new StudentExamReviewDto
            {
                ExamId = exam.ExamId,
                ExamTitle = exam.Title,
                ExamTotalPoints = examTotalPoints,
                StudentScore = attempt.FinalScore,
                ScorePercentage = scorePercentage,
                Status = attempt.Status ?? string.Empty,
                Questions = reviewQuestions
            };
        }
    }
}
