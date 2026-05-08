using System;

namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamResponseDto
    {
        public int ExamId { get; set; } 
        public string ExamTitle { get; set; } = string.Empty;
        public string ExamCode { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public int QuestionCount { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string ExamStatus { get; set; } = string.Empty;
    }
}
