using System;

namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamResultResponseDto
    {
        public string ExamTitle { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SubmitTime { get; set; }
    }
}
