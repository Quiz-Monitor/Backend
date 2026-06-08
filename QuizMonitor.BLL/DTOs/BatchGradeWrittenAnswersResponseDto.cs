using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs
{
    public class BatchGradeWrittenAnswersResponseDto
    {
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public int AttemptId { get; set; }
        public List<GradedAnswerDto> GradedAnswers { get; set; } = new();
        public AttemptScoreSummaryDto AttemptScoreSummary { get; set; } = null!;
        public DateTime GradedAt { get; set; }
    }

    public class GradedAnswerDto
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public decimal Score { get; set; }
        public string? Feedback { get; set; }
    }

    public class AttemptScoreSummaryDto
    {
        public decimal? McqScore { get; set; }
        public decimal? ManualScore { get; set; }
        public decimal? FinalScore { get; set; }
        public bool IsAttemptFullyGraded { get; set; }
        public string AttemptStatus { get; set; } = null!;
    }
}
