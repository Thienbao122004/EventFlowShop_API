﻿using Microsoft.AspNetCore.Http;
using Shopping.Repository.Validation;
using System;
using System.Collections.Generic;
<<<<<<< HEAD
using System.Text.Json.Serialization;
=======
using System.ComponentModel.DataAnnotations.Schema;
>>>>>>> 191ef86e8b5fd800c38c3a8db5132af426cb8c6b

namespace WebAPI_FlowerShopSWP.Models;

public partial class Flower
{
    public int FlowerId { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public string FlowerName { get; set; } = null!;

    public int Quantity { get; set; }

    public string Condition { get; set; } = null!;

    public decimal Price { get; set; }

    public DateTime? ListingDate { get; set; }

    public string Status { get; set; } = null!;

    public string? ImageUrl { get; set; }
   

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual Category? Category { get; set; } = null!;

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [JsonIgnore] // Ngăn không cho thuộc tính này được serialize
    public virtual User? Seller { get; set; } = null!;

}
