using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static WebAPI_FlowerShopSWP.Controllers.OrdersController;
using WebAPI_FlowerShopSWP.Controllers;

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

    public OrderDelivery? OrderDelivery { get; set; }


    public virtual User User { get; set; } = null!;


    
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
