namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamResultDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal? FinalScore { get; set; }
        public string CheatingStatus { get; set; } = string.Empty;
        public int TotalViolations { get; set; }
    }
}
