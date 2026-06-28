using System;

namespace QuizMonitor.BLL.DTOs
{
    public class InstructorExamDto
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamCode { get; set; } = string.Empty;
        public bool? IsPublished {get; set;}
    }
}
