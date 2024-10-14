using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController([FromServices] FlowerEventShopsContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous] // Allow this action to be accessed without authentication
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Include(n => n.Seller)
                    .Where(n => n.Seller != null && n.Seller.UserType == "Seller")
                    .OrderByDescending(n => n.NotificationDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.Message,
                        n.NotificationDate,
                        n.IsRead,
                        SellerName = n.Seller.FullName ?? "Unknown"
                    })
                    .ToListAsync();

                var totalCount = await _context.Notifications.CountAsync(n => n.Seller != null && n.Seller.UserType == "Seller");

                return Ok(new
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching notifications");
                return StatusCode(500, "An error occurred while fetching notifications");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] Notification notification)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            notification.NotificationDate = DateTime.Now;
            notification.IsRead = false;

            if (notification.SellerId == 0)
            {
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    notification.SellerId = userId;
                }
                else
                {
                    return BadRequest("SellerId is required");
                }
            }

            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetNotifications), new { id = notification.NotificationId }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the notification");
                return StatusCode(500, "An error occurred while creating the notification");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}