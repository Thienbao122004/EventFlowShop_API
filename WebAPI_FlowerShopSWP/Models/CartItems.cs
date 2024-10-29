using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace WebAPI_FlowerShopSWP.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int FlowerId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool IsCustomOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Cart Cart { get; set; }
        public Flower Flower { get; set; }
    }
}
