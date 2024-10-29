using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.DTO;

namespace WebAPI_FlowerShopSWP.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDTO>> GetUserNotifications(int userId, int page, int pageSize);
        Task<NotificationDTO> CreateNotification(CreateNotificationDTO dto);
        Task MarkAsRead(int notificationId);
        Task MarkAllAsRead(int userId);
        Task<int> GetUnreadCount(int userId);
    }
}
