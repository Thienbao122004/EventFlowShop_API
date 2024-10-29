using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace WebAPI_FlowerShopSWP.Models
{
    public partial class User
    {
        public User()
        {
            Deliveries = new HashSet<Delivery>();

            Flowers = new HashSet<Flower>();

            Messages = new HashSet<Message>();

            Notifications = new HashSet<Notification>();

            Orders = new HashSet<Order>();

            Reviews = new HashSet<Review>();

            Following = new HashSet<SellerFollow>();

            Followers = new HashSet<SellerFollow>();

            SellerConversations = new HashSet<Conversation>();

<<<<<<< HEAD
            BuyerConversations = new HashSet<Conversation>();
=======
    [NotMapped]
    public IFormFile? ProfileImageFile { get; set; }

    public DateTime? RegistrationDate { get; set; }
>>>>>>> 4aaea305f05818d11577be37aeff61a05b19b3cf

        }

        [Key]
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? UserType { get; set; }
        public string? Address { get; set; }
        public string? WardCode { get; set; }
        public int? DistrictId { get; set; }
        public string? Phone { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime? RegistrationDate { get; set; }


        [NotMapped]
        public string? Cart { get; set; }

        [NotMapped]
        public IFormFile? ProfileImageFile { get; set; }

        // Navigation properties
        public virtual ICollection<Delivery> Deliveries { get; set; }
        public virtual ICollection<Flower> Flowers { get; set; }
        public virtual ICollection<Message> Messages { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }

        [JsonIgnore]
        public virtual ICollection<SellerFollow> Following { get; set; }

        [JsonIgnore]
        public virtual ICollection<SellerFollow> Followers { get; set; }

        // Chat-related navigation properties
        public virtual ICollection<Conversation> SellerConversations { get; set; }
        public virtual ICollection<Conversation> BuyerConversations { get; set; }
    }
}