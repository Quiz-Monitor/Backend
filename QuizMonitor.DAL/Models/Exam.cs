using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class Exam
{
    public int ExamId { get; set; }

    public int InstructorId { get; set; }

    public string ExamCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int DurationMinutes { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool? CameraRequired { get; set; }

    public bool? TabSwitchingDetection { get; set; }

    public bool? EyeTrackingEnabled { get; set; }

    public bool? MultiplePersonDetection { get; set; }

    public int? MaxTabSwitches { get; set; }

    public int? MaxEyeAwaySeconds { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool? IsPublished { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public virtual User? DeletedByNavigation { get; set; }

    public virtual ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();

    public virtual User Instructor { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
