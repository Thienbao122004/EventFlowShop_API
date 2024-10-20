using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebAPI_FlowerShopSWP.Models;

public partial class User
{
    [Key]
    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? FullName { get; set; }

    public string? ProfileImageUrl { get; set; }
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? UserType { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual ICollection<Flower> Flowers { get; set; } = new List<Flower>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    [JsonIgnore]
    public ICollection<SellerFollow>? Following { get; set; }
    [JsonIgnore]
    public ICollection<SellerFollow>? Followers { get; set; }
}
