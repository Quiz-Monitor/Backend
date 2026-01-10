using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class QuestionAnswer
{
    public int AnswerId { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public string? SelectedChoices { get; set; }

    public decimal? Score { get; set; }

    public bool? IsCorrect { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public int? TimeSpentSeconds { get; set; }

    public int? ViolationCount { get; set; }

    public string? InstructorFeedback { get; set; }

    public bool? IsManuallyGraded { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AnswerViolation> AnswerViolations { get; set; } = new List<AnswerViolation>();

    public virtual ExamAttempt Attempt { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
