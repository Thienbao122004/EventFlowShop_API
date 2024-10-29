using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;
using Google;

namespace WebAPI_FlowerShopSWP.Services
{
    public class NotificationService : INotificationService
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IMapper _mapper;

        public NotificationService(FlowerEventShopsContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<NotificationDTO>> GetUserNotifications(int userId, int page, int pageSize)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return _mapper.Map<List<NotificationDTO>>(notifications);
        }

        public async Task<NotificationDTO> CreateNotification(CreateNotificationDTO dto)
        {
            var notification = _mapper.Map<Notification>(dto);
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsActive = true;
            notification.IsRead = false;

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return _mapper.Map<NotificationDTO>(notification);
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsRead(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && n.IsActive);
        }
    }
}