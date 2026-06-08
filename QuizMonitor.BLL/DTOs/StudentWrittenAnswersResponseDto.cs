using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class StudentWrittenAnswersResponseDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = null!;
        public int AttemptId { get; set; }
        public string AttemptStatus { get; set; } = null!;
        public List<WrittenAnswerDto> WrittenAnswers { get; set; } = new();
        public WrittenAnswersSummaryDto Summary { get; set; } = null!;
    }
}
