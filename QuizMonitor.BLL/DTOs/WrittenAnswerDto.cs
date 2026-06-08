namespace QuizMonitor.BLL.DTOs
{
    public class WrittenAnswerDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string? QuestionImageUrl { get; set; }
        public decimal Points { get; set; }
        public int OrderNumber { get; set; }
        public string? AnswerText { get; set; }
        public decimal? Score { get; set; }
        public string? InstructorFeedback { get; set; }
        public bool IsManuallyGraded { get; set; }
        public int? TimeSpentSeconds { get; set; }
    }
}
