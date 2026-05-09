namespace QuizMonitor.BLL.DTOs
{
    public class SubmitExamResponseDto
    {
        /// <summary>"GRADED" when all questions are MCQ (auto-graded). "SUBMITTED" when manual grading is needed.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>"auto_graded" = all MCQ, grade is final now.
        /// "pending_manual_grading" = has short-answer questions, instructor must grade.</summary>
        public string GradingStatus { get; set; } = string.Empty;

        public decimal McqScore { get; set; }

        public decimal? ManualScore { get; set; }

        /// <summary>Populated immediately when all-MCQ. Null until instructor finishes grading otherwise.</summary>
        public decimal? FinalScore { get; set; }

        public int TotalViolations { get; set; }

        public string CheatingStatus { get; set; } = string.Empty;
    }
}
