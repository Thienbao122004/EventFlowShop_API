using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebAPI_FlowerShopSWP.Models;
public class SellerFollow
{
    public int FollowId { get; set; }
    public int UserId { get; set; }
    public int SellerId { get; set; }
    public DateTime FollowDate { get; set; }
    public User User { get; set; }
    public User Seller { get; set; }
}