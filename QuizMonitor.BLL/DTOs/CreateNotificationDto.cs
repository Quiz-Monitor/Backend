using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class CreateNotificationDto
    {
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? ExamId { get; set; }
        public int? AttemptId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
