using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizMonitor.BLL.DTOs.NotificationDTOs;
using QuizMonitor.BLL.Interfaces;

namespace QuizMonitor.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(NotificationsListResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int? limit, [FromQuery] int? offset)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.GetUserNotificationsAsync(userId, isRead, limit ?? 20, offset ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { message = "An error occurred while retrieving notifications" });
            }
        }

        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(UnreadCountResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread notification count");
                return StatusCode(500, new { message = "An error occurred while retrieving unread notifications" });
            }
        }

        [HttpGet("{userNotificationId:int}")]
        [ProducesResponseType(typeof(NotificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNotificationById(int userNotificationId)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.GetUserNotificationByIdAsync(userId, userNotificationId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Notification not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification");
                return StatusCode(500, new { message = "An error occurred while retrieving the notification" });
            }
        }

        [HttpPut("{userNotificationId:int}/read")]
        [ProducesResponseType(typeof(MarkNotificationReadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkNotificationAsRead(int userNotificationId)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.MarkNotificationAsReadAsync(userId, userNotificationId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Notification not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { message = "An error occurred while updating the notification" });
            }
        }

        [HttpPut("mark-all-read")]
        [ProducesResponseType(typeof(MarkAllNotificationsReadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { message = "An error occurred while updating notifications" });
            }
        }

        [HttpDelete("{userNotificationId:int}")]
        [ProducesResponseType(typeof(DeleteNotificationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNotification(int userNotificationId)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var result = await _notificationService.DeleteUserNotificationAsync(userId, userNotificationId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Notification not found");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new { message = "An error occurred while deleting the notification" });
            }
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out userId);
        }
    }
}
