using System;

namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class MarkNotificationReadResponseDto
    {
        public int UserNotificationId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
