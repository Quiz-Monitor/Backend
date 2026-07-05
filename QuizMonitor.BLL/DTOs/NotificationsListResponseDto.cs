using System.Collections.Generic;

namespace QuizMonitor.BLL.DTOs.NotificationDTOs
{
    public class NotificationsListResponseDto
    {
        public List<NotificationResponseDto> Notifications { get; set; } = new List<NotificationResponseDto>();
        public NotificationPaginationDto Pagination { get; set; } = new NotificationPaginationDto();
        public int UnreadCount { get; set; }
    }
}
