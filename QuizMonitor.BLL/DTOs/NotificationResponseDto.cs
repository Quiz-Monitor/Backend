using System;
using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class NotificationResponseDto
    {
        public int UserNotificationId { get; set; }
        public int NotificationId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public NotificationExamDto? Exam { get; set; }
        public NotificationAttemptDto? Attempt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
