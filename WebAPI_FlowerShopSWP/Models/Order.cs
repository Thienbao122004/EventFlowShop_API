using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebAPI_FlowerShopSWP.Models;

public partial class Order
{
    public int OrderId { get; set; }
    //public int BuyerId { get; set; }

    public int UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string? DeliveryAddress { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual User User { get; set; } = null!;

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
