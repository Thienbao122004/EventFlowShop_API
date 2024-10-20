using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebAPI_FlowerShopSWP.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int FlowerId { get; set; }

    public string FlowerName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public virtual Flower Flower { get; set; } = null!;

    [JsonIgnore]
    public virtual Order Order { get; set; } = null!;
}
