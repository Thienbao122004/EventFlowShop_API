namespace WebAPI_FlowerShopSWP.DTO
{
    public class NotificationDTO
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
    }

    public class CreateNotificationDTO
    {
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public int? RelatedId { get; set; }
        public string RelatedType { get; set; }
    }
}
