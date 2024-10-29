namespace WebAPI_FlowerShopSWP.Models
{
    public class OrderReport
    {
        public int ReportId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public Order Order { get; set; }
        public User User { get; set; }
    }

}
