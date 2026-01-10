using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class ViolationEvent
{
    public int ViolationId { get; set; }

    public int AttemptId { get; set; }

    public int? QuestionId { get; set; }

    public string ViolationType { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string? Description { get; set; }

    public string? ScreenshotUrl { get; set; }

    public int? DurationSeconds { get; set; }

    public string? Metadata { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<AnswerViolation> AnswerViolations { get; set; } = new List<AnswerViolation>();

    public virtual ExamAttempt Attempt { get; set; } = null!;

    public virtual Question? Question { get; set; }
}
