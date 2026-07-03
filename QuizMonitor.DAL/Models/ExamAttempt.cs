using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class ExamAttempt
{
    public int AttemptId { get; set; }

    public int ExamId { get; set; }

    public int StudentId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? SubmitTime { get; set; }

    public int? TotalDurationSeconds { get; set; }

    public decimal? McqScore { get; set; }

    public decimal? ManualScore { get; set; }

    public decimal? FinalScore { get; set; }

    public string? Status { get; set; }

    public int? TotalViolations { get; set; }

    public int? TabSwitchCount { get; set; }

    public int? EyeAwayCount { get; set; }

    public int? ObjectDetectedCount { get; set; }

    public int? MultiplePersonCount { get; set; }

    public int? FaceMissingCount { get; set; }

    public int? LowVisibilityCount { get; set; }

    public int? SuspiciousObjectCount { get; set; }

    public string? InstructorNotes { get; set; }

    public bool? IsGraded { get; set; }

    public DateTime? GradedAt { get; set; }

    public int? GradedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public string? CheatingStatus { get; set; }

    public virtual User? DeletedByNavigation { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual User? GradedByNavigation { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<QuestionAnswer> QuestionAnswers { get; set; } = new List<QuestionAnswer>();

    public virtual User Student { get; set; } = null!;

    public virtual ICollection<ViolationEvent> ViolationEvents { get; set; } = new List<ViolationEvent>();
}
