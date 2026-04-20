namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamResultResponseDto
    {
        public string ExamTitle { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string CheatingStatus { get; set; } = string.Empty;
    }
}
