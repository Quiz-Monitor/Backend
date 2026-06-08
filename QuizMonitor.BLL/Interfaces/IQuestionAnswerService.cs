using QuizMonitor.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.Interfaces
{
    public interface IQuestionAnswerService
    {

        // Manual Grading
        Task<GradeAnswerResponseDto> GradeAnswerAsync(int answerId, int instructorId, GradeAnswerDto dto);

        // Written Answers for Grading
        Task<StudentWrittenAnswersResponseDto> GetWrittenAnswersAsync(int examId, int studentId, int instructorId);
        Task<BatchGradeWrittenAnswersResponseDto> BatchGradeWrittenAnswersAsync(int examId, int studentId, int instructorId, BatchGradeWrittenAnswersDto dto);
    }
}
