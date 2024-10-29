using System.ComponentModel.DataAnnotations;

namespace WebAPI_FlowerShopSWP.DTO
{
    public class CreateCustomOrderDTO
    {
        [Required]
        public int BuyerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FlowerName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string Description { get; set; }

        public IFormFile Image { get; set; }
    }
}
