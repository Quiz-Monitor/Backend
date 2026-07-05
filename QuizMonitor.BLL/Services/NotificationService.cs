using Microsoft.AspNetCore.SignalR;
using QuizMonitor.BLL.Hubs;
using QuizMonitor.BLL.Interfaces;
using QuizMonitor.DAL.Interfaces;
using QuizMonitor.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QuizMonitor.BLL.DTOs.NotificationDTOs;

namespace QuizMonitor.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub> hub)
        {
            _unitOfWork = unitOfWork;
            _hub = hub;
        }

        public async Task<NotificationResponseDto> SendToUserAsync(int userId, CreateNotificationDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null || user.DeletedAt != null)
            {
                throw new InvalidOperationException("User not found");
            }

            var (exam, attempt) = await ValidateRelatedEntitiesAsync(dto.ExamId, dto.AttemptId);

            var notification = new Notification
            {
                ExamId = dto.ExamId,
                AttemptId = dto.AttemptId,
                NotificationType = dto.NotificationType,
                Title = dto.Title,
                Message = dto.Message,
                Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var userNotification = new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                DeliveredAt = DateTime.UtcNow
            };

            await _unitOfWork.UserNotifications.AddAsync(userNotification);
            await _unitOfWork.SaveChangesAsync();

            var response = MapToResponse(userNotification, notification, exam, attempt);
            await _hub.Clients.User(userId.ToString()).SendAsync("NotificationReceived", response);

            return response;
        }

        public async Task<IReadOnlyList<NotificationResponseDto>> SendToUsersAsync(IEnumerable<int> userIds, CreateNotificationDto dto)
        {
            if (userIds == null)
            {
                throw new ArgumentNullException(nameof(userIds));
            }

            var distinctUserIds = userIds.Distinct().ToList();
            if (!distinctUserIds.Any())
            {
                return Array.Empty<NotificationResponseDto>();
            }

            var (exam, attempt) = await ValidateRelatedEntitiesAsync(dto.ExamId, dto.AttemptId);

            var notification = new Notification
            {
                ExamId = dto.ExamId,
                AttemptId = dto.AttemptId,
                NotificationType = dto.NotificationType,
                Title = dto.Title,
                Message = dto.Message,
                Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var userNotifications = distinctUserIds.Select(userId => new UserNotification
            {
                UserId = userId,
                NotificationId = notification.NotificationId,
                IsRead = false,
                DeliveredAt = DateTime.UtcNow
            }).ToList();

            await _unitOfWork.UserNotifications.AddRangeAsync(userNotifications);
            await _unitOfWork.SaveChangesAsync();

            var responses = new List<NotificationResponseDto>();
            foreach (var userNotification in userNotifications)
            {
                var response = MapToResponse(userNotification, notification, exam, attempt);
                responses.Add(response);
                await _hub.Clients.User(userNotification.UserId.ToString()).SendAsync("NotificationReceived", response);
            }

            return responses;
        }

        public async Task<NotificationsListResponseDto> GetUserNotificationsAsync(int userId, bool? isRead, int limit, int offset)
        {
            if (limit <= 0)
            {
                limit = 20;
            }

            if (offset < 0)
            {
                offset = 0;
            }

            var userNotifications = await _unitOfWork.UserNotifications.FindAsync(un => un.UserId == userId);

            var filtered = isRead.HasValue
                ? userNotifications.Where(un => (un.IsRead ?? false) == isRead.Value)
                : userNotifications;

            var ordered = filtered
                .OrderByDescending(un => un.DeliveredAt)
                .ToList();

            var total = ordered.Count;
            var page = ordered.Skip(offset).Take(limit).ToList();

            var notificationIds = page.Select(p => p.NotificationId).Distinct().ToList();
            var notifications = notificationIds.Any()
                ? (await _unitOfWork.Notifications.FindAsync(n => notificationIds.Contains(n.NotificationId) && n.DeletedAt == null)).ToList()
                : new List<Notification>();

            var notificationById = notifications.ToDictionary(n => n.NotificationId);

            var examIds = notifications.Where(n => n.ExamId.HasValue).Select(n => n.ExamId!.Value).Distinct().ToList();
            var attemptIds = notifications.Where(n => n.AttemptId.HasValue).Select(n => n.AttemptId!.Value).Distinct().ToList();

            var exams = examIds.Any()
                ? (await _unitOfWork.Exams.FindAsync(e => examIds.Contains(e.ExamId) && e.DeletedAt == null)).ToList()
                : new List<Exam>();

            var attempts = attemptIds.Any()
                ? (await _unitOfWork.ExamAttempts.FindAsync(a => attemptIds.Contains(a.AttemptId) && a.DeletedAt == null)).ToList()
                : new List<ExamAttempt>();

            var examById = exams.ToDictionary(e => e.ExamId);
            var attemptById = attempts.ToDictionary(a => a.AttemptId);

            var responses = new List<NotificationResponseDto>();
            foreach (var userNotification in page)
            {
                if (!notificationById.TryGetValue(userNotification.NotificationId, out var notification))
                {
                    continue;
                }

                Exam? exam = null;
                if (notification.ExamId.HasValue)
                {
                    examById.TryGetValue(notification.ExamId.Value, out exam);
                }

                ExamAttempt? attempt = null;
                if (notification.AttemptId.HasValue)
                {
                    attemptById.TryGetValue(notification.AttemptId.Value, out attempt);
                }

                responses.Add(MapToResponse(userNotification, notification, exam, attempt));
            }

            var unreadCount = await _unitOfWork.UserNotifications.CountAsync(un =>
                un.UserId == userId && (un.IsRead == null || un.IsRead == false));

            return new NotificationsListResponseDto
            {
                Notifications = responses,
                Pagination = new NotificationPaginationDto
                {
                    Total = total,
                    Limit = limit,
                    Offset = offset,
                    HasMore = offset + limit < total
                },
                UnreadCount = unreadCount
            };
        }

        public async Task<NotificationResponseDto> GetUserNotificationByIdAsync(int userId, int userNotificationId)
        {
            var userNotification = await _unitOfWork.UserNotifications.FirstOrDefaultAsync(
                un => un.UserNotificationId == userNotificationId && un.UserId == userId);

            if (userNotification == null)
            {
                throw new InvalidOperationException("Notification not found");
            }

            var notification = await _unitOfWork.Notifications.GetByIdAsync(userNotification.NotificationId);
            if (notification == null || notification.DeletedAt != null)
            {
                throw new InvalidOperationException("Notification not found");
            }

            Exam? exam = null;
            if (notification.ExamId.HasValue)
            {
                exam = await _unitOfWork.Exams.GetByIdAsync(notification.ExamId.Value);
                if (exam != null && exam.DeletedAt != null)
                {
                    exam = null;
                }
            }

            ExamAttempt? attempt = null;
            if (notification.AttemptId.HasValue)
            {
                attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(notification.AttemptId.Value);
                if (attempt != null && attempt.DeletedAt != null)
                {
                    attempt = null;
                }
            }

            return MapToResponse(userNotification, notification, exam, attempt);
        }

        public async Task<UnreadCountResponseDto> GetUnreadCountAsync(int userId)
        {
            var count = await _unitOfWork.UserNotifications.CountAsync(un =>
                un.UserId == userId && (un.IsRead == null || un.IsRead == false));

            return new UnreadCountResponseDto
            {
                UnreadCount = count
            };
        }

        public async Task<MarkNotificationReadResponseDto> MarkNotificationAsReadAsync(int userId, int userNotificationId)
        {
            var userNotification = await _unitOfWork.UserNotifications.FirstOrDefaultAsync(
                un => un.UserNotificationId == userNotificationId && un.UserId == userId);

            if (userNotification == null)
            {
                throw new InvalidOperationException("Notification not found");
            }

            if (userNotification.IsRead != true)
            {
                userNotification.IsRead = true;
                userNotification.ReadAt = DateTime.UtcNow;
                _unitOfWork.UserNotifications.Update(userNotification);
                await _unitOfWork.SaveChangesAsync();
            }

            return new MarkNotificationReadResponseDto
            {
                UserNotificationId = userNotification.UserNotificationId,
                IsRead = userNotification.IsRead ?? true,
                ReadAt = userNotification.ReadAt
            };
        }

        public async Task<MarkAllNotificationsReadResponseDto> MarkAllNotificationsAsReadAsync(int userId)
        {
            var userNotifications = await _unitOfWork.UserNotifications.FindAsync(un =>
                un.UserId == userId && (un.IsRead == null || un.IsRead == false));

            var items = userNotifications.ToList();
            if (!items.Any())
            {
                return new MarkAllNotificationsReadResponseDto
                {
                    MarkedCount = 0,
                    Message = "All notifications marked as read"
                };
            }

            var now = DateTime.UtcNow;
            foreach (var userNotification in items)
            {
                userNotification.IsRead = true;
                userNotification.ReadAt = now;
            }

            _unitOfWork.UserNotifications.UpdateRange(items);
            await _unitOfWork.SaveChangesAsync();

            return new MarkAllNotificationsReadResponseDto
            {
                MarkedCount = items.Count,
                Message = "All notifications marked as read"
            };
        }

        public async Task<DeleteNotificationResponseDto> DeleteUserNotificationAsync(int userId, int userNotificationId)
        {
            var userNotification = await _unitOfWork.UserNotifications.FirstOrDefaultAsync(
                un => un.UserNotificationId == userNotificationId && un.UserId == userId);

            if (userNotification == null)
            {
                throw new InvalidOperationException("Notification not found");
            }

            _unitOfWork.UserNotifications.Delete(userNotification);
            await _unitOfWork.SaveChangesAsync();

            return new DeleteNotificationResponseDto
            {
                Message = "Notification deleted successfully"
            };
        }

        private static NotificationResponseDto MapToResponse(
            UserNotification userNotification,
            Notification notification,
            Exam? exam,
            ExamAttempt? attempt)
        {
            return new NotificationResponseDto
            {
                UserNotificationId = userNotification.UserNotificationId,
                NotificationId = notification.NotificationId,
                NotificationType = notification.NotificationType,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = userNotification.IsRead ?? false,
                DeliveredAt = userNotification.DeliveredAt,
                ReadAt = userNotification.ReadAt,
                CreatedAt = notification.CreatedAt,
                Exam = exam == null
                    ? null
                    : new NotificationExamDto
                    {
                        ExamId = exam.ExamId,
                        ExamCode = exam.ExamCode,
                        Title = exam.Title
                    },
                Attempt = attempt == null
                    ? null
                    : new NotificationAttemptDto
                    {
                        AttemptId = attempt.AttemptId
                    },
                Metadata = ParseMetadata(notification.Metadata)
            };
        }

        private static Dictionary<string, object>? ParseMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<(Exam? Exam, ExamAttempt? Attempt)> ValidateRelatedEntitiesAsync(int? examId, int? attemptId)
        {
            Exam? exam = null;
            if (examId.HasValue)
            {
                exam = await _unitOfWork.Exams.GetByIdAsync(examId.Value);
                if (exam == null || exam.DeletedAt != null)
                {
                    throw new InvalidOperationException("Exam not found");
                }
            }

            ExamAttempt? attempt = null;
            if (attemptId.HasValue)
            {
                attempt = await _unitOfWork.ExamAttempts.GetByIdAsync(attemptId.Value);
                if (attempt == null || attempt.DeletedAt != null)
                {
                    throw new InvalidOperationException("Attempt not found");
                }
            }

            return (exam, attempt);
        }

    }
}
