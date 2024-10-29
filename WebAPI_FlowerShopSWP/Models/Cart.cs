using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using static WebAPI_FlowerShopSWP.Controllers.OrdersController;

namespace WebAPI_FlowerShopSWP.Models
{
    public partial class Cart
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = "Active";

        [JsonIgnore]
        public virtual User User { get; set; }
        [JsonIgnore]
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
