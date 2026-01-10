using System;
using System.Collections.Generic;

namespace QuizMonitor.DAL.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int? ExamId { get; set; }

    public int? AttemptId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Metadata { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ExamAttempt? Attempt { get; set; }

    public virtual Exam? Exam { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
