using System.ComponentModel.DataAnnotations;

namespace WebAPI_FlowerShopSWP.DTO
{
    public class CartDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public string? Status { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalAmount { get; set; }
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int FlowerId { get; set; }
        public string FlowerName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public bool IsCustomOrder { get; set; }
        public string SellerFullName { get; set; }
    }

    public class AddToCartDto
    {
        [Required]
        public int FlowerId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
        public bool IsCustomOrder { get; set; } = false;
        public int BuyerId { get; set; }
    }

    public class UpdateCartItemDto
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
}