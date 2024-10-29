using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Services;
using System.Security.Claims;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationDTO>>> GetMyNotifications(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var notifications = await _notificationService.GetUserNotifications(userId.Value, page, pageSize);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return StatusCode(500, "An error occurred while fetching notifications");
            }
        }

        [HttpPost]
        public async Task<ActionResult<NotificationDTO>> CreateNotification(
            [FromBody] CreateNotificationDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var notification = await _notificationService.CreateNotification(createDto);
                return CreatedAtAction(
                    nameof(GetMyNotifications),
                    new { id = notification.NotificationId },
                    notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, "An error occurred while creating the notification");
            }
        }

        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsRead(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, "An error occurred while marking the notification as read");
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                await _notificationService.MarkAllAsRead(userId.Value);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "An error occurred while marking all notifications as read");
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var count = await _notificationService.GetUnreadCount(userId.Value);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return StatusCode(500, "An error occurred while getting unread count");
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }
}