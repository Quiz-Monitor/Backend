using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class InstructorExamQuestionsResponseDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int TotalQuestions { get; set; }
        public List<QuestionResponseDto> Questions { get; set; } = new();
    }
}
