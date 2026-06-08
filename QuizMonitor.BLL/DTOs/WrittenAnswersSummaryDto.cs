namespace QuizMonitor.BLL.DTOs
{
    public class WrittenAnswersSummaryDto
    {
        public int TotalWrittenQuestions { get; set; }
        public int GradedCount { get; set; }
        public int UngradedCount { get; set; }
        public decimal TotalWrittenPoints { get; set; }
        public decimal AwardedPoints { get; set; }
    }
}
