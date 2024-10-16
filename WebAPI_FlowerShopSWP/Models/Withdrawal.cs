namespace WebAPI_FlowerShopSWP.Models
{
    public class WithdrawalRequest
    {
        public int RequestId { get; set; } 
        public int UserId { get; set; } 
        public string AccountNumber { get; set; } 
        public string FullName { get; set; } 
        public string Phone { get; set; } 
        public decimal Amount { get; set; } 
        public DateTime RequestDate { get; set; } = DateTime.Now; 
        public string Status { get; set; } = "Pending"; 
        public string? Remarks { get; set; } 
    }
}
