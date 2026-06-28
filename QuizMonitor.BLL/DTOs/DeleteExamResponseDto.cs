using System;

namespace QuizMonitor.BLL.DTOs
{
    public class DeleteExamResponseDto
    {
        public string   Message          { get; set; } = string.Empty;
        public int      ExamId           { get; set; }
        public string   ExamTitle        { get; set; } = string.Empty;
        public int      QuestionsDeleted { get; set; }
        public DateTime DeletedAt        { get; set; }
    }
}
