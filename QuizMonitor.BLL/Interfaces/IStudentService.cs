using System.Collections.Generic;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IStudentService
    {
        Task<List<StudentExamResultResponseDto>> GetMyExamResultsAsync(int studentId);
        Task<List<StudentExamResponseDto>> GetAvailableExamsForStudentAsync(int studentId);
        Task<StudentSubmittedExamsResponseDto> GetSubmittedExamsAsync(int studentId);
        Task<StudentStatisticsResponseDto> GetStudentStatisticsAsync(int studentId);

        /// <summary>
        /// Returns the student's answers alongside correct answers for a graded exam.
        /// Throws if the attempt is not yet graded.
        /// </summary>
        Task<StudentExamReviewDto> GetExamReviewAsync(int examId, int studentId);
    }
}
