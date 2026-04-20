using System;

namespace QuizMonitor.BLL.DTOs
{
    public class GradeAnswerResponseDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public int AttemptId { get; set; }
        public decimal Score { get; set; }
        public string? Feedback { get; set; }
        public DateTime GradedAt { get; set; }
        public int GradedBy { get; set; }

        // Attempt score summary
        public decimal? McqScore { get; set; }
        public decimal? ManualScore { get; set; }
        public decimal? FinalScore { get; set; }
        public bool IsAttemptFullyGraded { get; set; }
    }
}
