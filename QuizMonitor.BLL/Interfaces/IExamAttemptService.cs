using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IExamAttemptService
    {
        Task<JoinExamResponseDto> JoinExamAsync(int studentId, JoinExamDto dto);
        Task<StartExamResponseDto> StartExamAsync(int studentId, StartExamDto dto);
        Task<QuestionResponseDto> GetQuestionByOrderAsync(int attemptId, int studentId, int orderNumber);
        Task<SaveAnswerResponseDto> SaveAnswerAsync(int attemptId, int studentId, SaveAnswerDto dto);
        Task<LogViolationResponseDto> LogViolationAsync(int attemptId, int studentId, LogViolationDto dto);
        Task<SubmitExamResponseDto> SubmitExamAsync(int attemptId, int studentId);
        Task<ExamAttemptDetailResponseDto> GetExamAttemptDetailsAsync(int attemptId, int instructorId);

        /// <summary>Returns all questions (with choices) for an active exam attempt — no pagination.</summary>
        Task<ExamQuestionsResponseDto> GetAllQuestionsAsync(int attemptId, int studentId);

        /// <summary>Saves (upsert) every answer in a single transaction.</summary>
        Task<BulkSaveAnswersResponseDto> BulkSaveAnswersAsync(int attemptId, int studentId, BulkSaveAnswersDto dto);

    }
}
