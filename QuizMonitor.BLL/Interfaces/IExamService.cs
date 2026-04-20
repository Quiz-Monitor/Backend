using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IExamService
    {
        // Exam Management
        Task<ExamResponseDto> CreateExamAsync(int instructorId, CreateExamDto dto);
        Task<ExamResponseDto> PublishExamAsync(int examId, int instructorId);

        // Question Management
        Task<QuestionResponseDto> AddQuestionAsync(int examId, int instructorId, CreateQuestionDto dto);
        Task<QuestionResponseDto> UpdateQuestionAsync(int examId, int questionId, int instructorId, UpdateQuestionDto dto);
        Task<bool> RemoveQuestionAsync(int examId, int questionId, int instructorId);

        // Exam Results
        Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int instructorId);

        
        // Instructor Exams
        Task<List<InstructorExamDto>> GetInstructorExamsAsync(int instructorId);

    }
}
