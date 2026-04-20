using System;
using System.Collections.Generic;
using System.Linq;
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

            // Get all exam attempts for this student (only graded)
            var examAttempts = await _unitOfWork.ExamAttempts.FindAsync(ea =>
                ea.StudentId == studentId &&
                ea.DeletedAt == null &&
                ea.IsGraded == true);

            if (!examAttempts.Any())
            {
                return new List<StudentExamResultResponseDto>();
            }

            // Batch fetch all required exams to avoid N+1 problem
            var examIds = examAttempts.Select(ea => ea.ExamId).Distinct().ToList();
            var exams = await _unitOfWork.Exams.FindAsync(e => examIds.Contains(e.ExamId) && e.DeletedAt == null);
            var examDictionary = exams.ToDictionary(e => e.ExamId);

            var results = new List<StudentExamResultResponseDto>();

            foreach (var attempt in examAttempts)
            {
                if (examDictionary.TryGetValue(attempt.ExamId, out var exam))
                {
                    results.Add(new StudentExamResultResponseDto
                    {
                        ExamTitle = exam.Title,
                        FinalScore = attempt.FinalScore,
                        CheatingStatus = attempt.CheatingStatus ?? "CLEAN"
                    });
                }
            }

            return results;
        }
    }
}
