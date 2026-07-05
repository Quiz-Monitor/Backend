using QuizMonitor.BLL.DTOs.NotificationDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizMonitor.BLL.Interfaces
{
    public interface INotificationService
    {

        // send to user and save to DB
        Task<NotificationResponseDto> SendToUserAsync(int userId, CreateNotificationDto dto);

        // send to users and save to DB
        Task<IReadOnlyList<NotificationResponseDto>> SendToUsersAsync(IEnumerable<int> userIds, CreateNotificationDto dto);


        // get all user notification related to endpoint you will create GET /api/notifications
        Task<NotificationsListResponseDto> GetUserNotificationsAsync(int userId, bool? isRead, int limit, int offset);


        // Get Notification by ID    related to endpoint you will create    GET /api/notifications/{userNotificationId}
        Task<NotificationResponseDto> GetUserNotificationByIdAsync(int userId, int userNotificationId);


        // get unread notification count   related to endpoint you will create GET /api/notifications/unread-count
        Task<UnreadCountResponseDto> GetUnreadCountAsync(int userId);


        // Mark Notification as Read   related to endpoint you will create  PUT /api/notifications/{userNotificationId}/read
        Task<MarkNotificationReadResponseDto> MarkNotificationAsReadAsync(int userId, int userNotificationId);


        // Mark All Notifications as Read   related to endpoint you will create  PUT /api/notifications/mark-all-read
        Task<MarkAllNotificationsReadResponseDto> MarkAllNotificationsAsReadAsync(int userId);


        // Delete Notification (for user)   related to endpoint you will create   DELETE /api/notifications/{userNotificationId}
        Task<DeleteNotificationResponseDto> DeleteUserNotificationAsync(int userId, int userNotificationId);

    }
}
