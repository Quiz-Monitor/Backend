using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class ExamQuestionsResponseDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public List<QuestionResponseDto> Questions { get; set; } = new();
    }
}
