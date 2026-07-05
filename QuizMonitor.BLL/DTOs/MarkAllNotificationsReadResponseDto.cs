namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class MarkAllNotificationsReadResponseDto
    {
        public int MarkedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
