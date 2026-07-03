using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;

namespace QuizMonitor.BLL.Services
{
    public class InstructorService : IInstructorService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InstructorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<InstructorStatisticsResponseDto> GetInstructorStatisticsAsync(int instructorId)
        {
            // Verify instructor exists and has the correct role
            var instructor = await _unitOfWork.Users.GetByIdAsync(instructorId);
            if (instructor == null || !string.Equals(instructor.Role, "instructor", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only instructors can access this endpoint");
            }

            // Fetch all non-deleted exams for this instructor
            var exams = (await _unitOfWork.Exams.FindAsync(e =>
                e.InstructorId == instructorId &&
                e.DeletedAt == null)).ToList();

            var response = new InstructorStatisticsResponseDto();

            // === Exam Overview ===
            response.ExamOverview = new InstructorExamOverviewDto
            {
                TotalExamsCreated = exams.Count,
                TotalExamsPublished = exams.Count(e => e.IsPublished == true),
                TotalExamsDraft = exams.Count(e => e.IsPublished != true)
            };

            if (!exams.Any())
            {
                return response;
            }

            // Fetch all submitted/graded attempts for this instructor's exams
            var examIds = exams.Select(e => e.ExamId).ToList();
            var validStatuses = new[] { "submitted", "graded" };
            var attempts = (await _unitOfWork.ExamAttempts.FindAsync(ea =>
                examIds.Contains(ea.ExamId) &&
                ea.DeletedAt == null &&
                validStatuses.Contains(ea.Status))).ToList();

            // Count exams that have at least one attempt
            var examIdsWithAttempts = attempts.Select(a => a.ExamId).Distinct().ToHashSet();
            response.ExamOverview.TotalExamsWithAttempts = examIdsWithAttempts.Count;

            // === Student Overview ===
            response.StudentOverview = new InstructorStudentOverviewDto
            {
                TotalUniqueStudents = attempts.Select(a => a.StudentId).Distinct().Count(),
                TotalAttempts = attempts.Count,
                TotalGradedAttempts = attempts.Count(a => string.Equals(a.Status, "graded", StringComparison.OrdinalIgnoreCase)),
                TotalPendingGradingAttempts = attempts.Count(a => string.Equals(a.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            };

            // Fetch questions for total points per exam
            var questions = await _unitOfWork.Questions.FindAsync(q =>
                examIds.Contains(q.ExamId) &&
                q.DeletedAt == null);
            var examTotalPointsDict = questions
                .GroupBy(q => q.ExamId)
                .ToDictionary(g => g.Key, g => g.Sum(q => q.Points));

            // === Score Statistics (graded attempts only) ===
            var gradedAttempts = attempts
                .Where(a => string.Equals(a.Status, "graded", StringComparison.OrdinalIgnoreCase) && a.FinalScore.HasValue)
                .ToList();

            if (gradedAttempts.Any())
            {
                // Compute per-attempt score percentages
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

                    // Per-exam averages for highest/lowest
                    var examDictionary = exams.ToDictionary(e => e.ExamId);
                    var perExamAverages = attemptPercentages
                        .GroupBy(a => a.ExamId)
                        .Select(g => new ExamAverageScoreDto
                        {
                            ExamId = g.Key,
                            ExamTitle = examDictionary.TryGetValue(g.Key, out var exam) ? exam.Title : "Unknown",
                            AverageScorePercentage = Math.Round(g.Average(a => a.Percentage), 1),
                            AttemptCount = g.Count()
                        })
                        .ToList();

                    var highest = perExamAverages.OrderByDescending(e => e.AverageScorePercentage).First();
                    var lowest = perExamAverages.OrderBy(e => e.AverageScorePercentage).First();

                    response.ScoreStatistics.HighestAverageExam = highest;
                    response.ScoreStatistics.LowestAverageExam = lowest;

                    // Pass rate: percentage of graded attempts with score >= 50%
                    var passingCount = attemptPercentages.Count(a => a.Percentage >= 50m);
                    response.ScoreStatistics.PassRate = Math.Round((decimal)passingCount / attemptPercentages.Count * 100m, 1);
                }
            }

            // === Integrity Statistics (all submitted attempts) ===
            var totalViolations = attempts.Sum(a => a.TotalViolations ?? 0);
            response.IntegrityStatistics = new InstructorIntegrityStatisticsDto
            {
                TotalViolationsAcrossAllExams = totalViolations,
                AverageViolationsPerAttempt = attempts.Any()
                    ? Math.Round((decimal)totalViolations / attempts.Count, 2)
                    : 0m,
                CleanAttempts = attempts.Count(a => string.Equals(a.CheatingStatus, "clean", StringComparison.OrdinalIgnoreCase)),
                WarningAttempts = attempts.Count(a => string.Equals(a.CheatingStatus, "warning", StringComparison.OrdinalIgnoreCase)),
                FlaggedAttempts = attempts.Count(a => string.Equals(a.CheatingStatus, "flagged", StringComparison.OrdinalIgnoreCase))
            };

            return response;
        }
    }
}
