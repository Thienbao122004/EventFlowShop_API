using System.ComponentModel.DataAnnotations;

namespace WebAPI_FlowerShopSWP.DTO
{
    public class PaymentDto
    {
        public int OrderId { get; set; }

        public decimal Amount { get; set; }

        [RegularExpression("^(Success|Failed)$", ErrorMessage = "Invalid payment status.")]
        public string PaymentStatus { get; set; }
    }
}
