using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Enums;

namespace WebAPI_FlowerShopSWP.Helpers
{
    public static class NotificationHelper
    {
        public static CreateNotificationDTO CreateOrderNotification(int userId, int orderId, string status)
        {
            return new CreateNotificationDTO
            {
                UserId = userId,
                Title = "Cập nhật đơn hàng",
                Content = $"Đơn hàng #{orderId} {status}",
                Type = "ORDER_STATUS",
                RelatedId = orderId,
                RelatedType = "order"
            };
        }

        public static CreateNotificationDTO CreateReviewNotification(int userId, int reviewId)
        {
            return new CreateNotificationDTO
            {
                UserId = userId,
                Type = NotificationType.ReviewReceived,
                RelatedId = reviewId,
                RelatedType = RelatedType.Review,
                Title = "Đánh giá mới",
                Content = "Bạn vừa nhận được một đánh giá mới"
            };
        }
    }
}
