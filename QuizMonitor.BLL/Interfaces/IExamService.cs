using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IExamService
    {
        // Exam Management
        Task<ExamResponseDto> CreateExamAsync(int instructorId, CreateExamDto dto);
        Task<ExamResponseDto> PublishExamAsync(int examId, int instructorId);
        Task<ExamResponseDto> UpdateExamAsync(int examId, int instructorId, UpdateExamDto dto);

        // Question Management
        Task<QuestionResponseDto> AddQuestionAsync(int examId, int instructorId, CreateQuestionDto dto);
        Task<QuestionResponseDto> UpdateQuestionAsync(int examId, int questionId, int instructorId, UpdateQuestionDto dto);
        Task<bool> RemoveQuestionAsync(int examId, int questionId, int instructorId);
        Task<InstructorExamQuestionsResponseDto> GetExamQuestionsAsync(int examId, int instructorId);

        // Exam Results
        Task<List<StudentExamResultDto>> GetExamResultsAsync(int examId, int instructorId);
        Task<SubmittedStudentsResponseDto> GetSubmittedStudentsAsync(int examId, int instructorId);


        // Instructor Exams
        Task<List<InstructorExamDto>> GetInstructorExamsAsync(int instructorId);

        /// <summary>
        /// Update exam metadata + replace all questions in one transactional call.
        /// Exam must not be published.
        /// </summary>
        Task<FullEditExamResponseDto> FullEditExamAsync(int examId, int instructorId, FullEditExamDto dto);

        /// <summary>
        /// Soft-delete the exam and all its questions.
        /// Allowed when: exam is not published, OR exam has already ended.
        /// </summary>
        Task<DeleteExamResponseDto> DeleteExamAsync(int examId, int instructorId);

    }
}
