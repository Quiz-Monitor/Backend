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
    }
}
