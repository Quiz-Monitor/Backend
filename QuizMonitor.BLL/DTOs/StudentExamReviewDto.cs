using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class StudentExamReviewDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public decimal ExamTotalPoints { get; set; }
        public decimal? StudentScore { get; set; }
        public decimal? ScorePercentage { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ReviewQuestionDto> Questions { get; set; } = new();
    }
}
