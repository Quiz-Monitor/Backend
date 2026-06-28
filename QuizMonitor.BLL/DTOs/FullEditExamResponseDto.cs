using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class FullEditExamResponseDto
    {
        public ExamResponseDto         Exam      { get; set; } = new();
        public List<QuestionResponseDto> Questions { get; set; } = new();
        public FullEditSummaryDto      Summary   { get; set; } = new();
    }

    public class FullEditSummaryDto
    {
        public int QuestionsAdded   { get; set; }
        public int QuestionsUpdated { get; set; }
        public int QuestionsDeleted { get; set; }
        public int TotalQuestions   { get; set; }
    }
}
