namespace QuizMonitor.BLL.DTOs
{
    public class SubmittedExamDto
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public string ExamCode { get; set; } = null!;
        public string InstructorName { get; set; } = null!;
        public DateTime? SubmitTime { get; set; }
        public int DurationMinutes { get; set; }
        public int? TimeSpentSeconds { get; set; }
        public int QuestionCount { get; set; }
        public decimal ExamTotalPoints { get; set; }
        public string GradingStatus { get; set; } = null!;
        public decimal? McqScore { get; set; }
        public decimal? ManualScore { get; set; }
        public decimal? FinalScore { get; set; }
        public decimal? ScorePercentage { get; set; }
        public int TotalViolations { get; set; }
        public string CheatingStatus { get; set; } = null!;
    }

    public class StudentSubmittedExamsResponseDto
    {
        public int TotalExams { get; set; }
        public List<SubmittedExamDto> Exams { get; set; } = new();
    }
}
