using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IInstructorService
    {
        Task<InstructorStatisticsResponseDto> GetInstructorStatisticsAsync(int instructorId);
    }
}
