using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class SubmittedStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AttemptId { get; set; }
        public string AttemptStatus { get; set; } = string.Empty;
        public DateTime? SubmitTime { get; set; }
        public decimal? McqScore { get; set; }
        public decimal? ManualScore { get; set; }
        public decimal? FinalScore { get; set; }
        public int TotalViolations { get; set; }
        public string CheatingStatus { get; set; } = string.Empty;
        public bool HasWrittenAnswers { get; set; }
        public bool WrittenAnswersGraded { get; set; }
    }

    public class SubmittedStudentsResponseDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int TotalSubmitted { get; set; }
        public List<SubmittedStudentDto> Students { get; set; } = new();
    }
}
