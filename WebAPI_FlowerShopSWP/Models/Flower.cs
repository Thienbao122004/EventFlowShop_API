﻿using Microsoft.AspNetCore.Http;
using Shopping.Repository.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

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

    // Thêm các trường mới
    public bool IsVisible { get; set; } = true;

    public bool IsCustomOrder { get; set; } = false;

    // Navigation properties
    [JsonIgnore]
    public virtual Category? Category { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [JsonIgnore]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [JsonIgnore]
    public virtual User? Seller { get; set; } = null!;

    // Constructor
    public Flower()
    {
        ListingDate = DateTime.UtcNow;
        IsVisible = true;
        IsCustomOrder = false;
        Status = "Available";
        Condition = "New";
    }
}

// Thêm DTO cho Custom Order
public class CustomOrderDto
{
    public string FlowerName { get; set; } = null!;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public int CartId { get; set; }
}