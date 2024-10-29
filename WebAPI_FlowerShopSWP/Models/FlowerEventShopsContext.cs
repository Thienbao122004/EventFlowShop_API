using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Controllers;

    namespace WebAPI_FlowerShopSWP.Models;

    public partial class FlowerEventShopsContext : DbContext
    {
        public FlowerEventShopsContext()
        {
        }

        public FlowerEventShopsContext(DbContextOptions<FlowerEventShopsContext> options)
            : base(options)
        {
        }
        public virtual DbSet<Category> Categories { get; set; }

        public virtual DbSet<Conversation> Conversations { get; set; }
        public virtual DbSet<Message> Messages { get; set; }

        public virtual DbSet<Delivery> Deliveries { get; set; }

        public virtual DbSet<Flower> Flowers { get; set; }

        public virtual DbSet<Notification> Notifications { get; set; }

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<OrderItem> OrderItems { get; set; }

        public virtual DbSet<Payment> Payments { get; set; }

        public virtual DbSet<Review> Reviews { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public DbSet<SellerRegistrationRequest> SellerRegistrationRequests { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
    public virtual DbSet<SellerFollow> SellerFollows { get; set; }

        public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

        public virtual DbSet<OrderReport> OrderReports { get; set; }



    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
 => optionsBuilder.UseSqlServer(GetConnectionString());
    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder().
            SetBasePath(Directory.GetCurrentDirectory()).
            AddJsonFile("appsettings.json", true, true)
            .Build();
        string connectionString = config["ConnectionStrings:ConnectDB"];
        return connectionString;
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId).HasName("PK__Categori__23CAF1D89BCF6E4B");

                entity.Property(e => e.CategoryId).HasColumnName("categoryId");
                entity.Property(e => e.CategoryName)
                    .HasMaxLength(100)
                    .HasColumnName("categoryName");
                entity.Property(e => e.Description)
                    .HasMaxLength(255)
                    .HasColumnName("description");

            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.CartId);

                entity.Property(e => e.CartId)
                    .HasColumnName("cartId");

                entity.Property(e => e.UserId)
                    .HasColumnName("userId");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("createdAt");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("updatedAt");

                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("Active")
                    .HasColumnName("status");

                // Relationship
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Carts_Users");
            });


        modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.CartItemId);

                entity.Property(e => e.CartItemId)
                    .HasColumnName("cartItemId");

                entity.Property(e => e.CartId)
                    .HasColumnName("cartId");

                entity.Property(e => e.FlowerId)
                    .HasColumnName("flowerId");

                entity.Property(e => e.Quantity)
                    .HasColumnName("quantity");

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("price");

                entity.Property(e => e.IsCustomOrder)
                    .HasDefaultValue(false)
                    .HasColumnName("isCustomOrder");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("createdAt");

                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnName("updatedAt");

                // Relationships
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_CartItems_Carts");

                entity.HasOne(ci => ci.Flower)
                    .WithMany()
                    .HasForeignKey(ci => ci.FlowerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CartItems_Flowers");
            });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);

            entity.Property(e => e.ConversationId)
                .HasColumnName("conversationId");

            entity.Property(e => e.SellerId)
                .HasColumnName("sellerId");

            entity.Property(e => e.BuyerId)
                .HasColumnName("buyerId");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updatedAt")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.LastMessageId)
                .HasColumnName("lastMessageId");

            entity.Property(e => e.IsActive)
                .HasColumnName("isActive")
                .HasDefaultValue(true);

            // Navigation properties
            entity.HasOne(d => d.Seller)
                .WithMany(p => p.SellerConversations)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversations_Seller");

            entity.HasOne(d => d.Buyer)
                .WithMany(p => p.BuyerConversations)
                .HasForeignKey(d => d.BuyerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversations_Buyer");

            entity.HasOne(d => d.LastMessage)
                .WithMany()
                .HasForeignKey(d => d.LastMessageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Conversations_LastMessage");
        });



        modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__CDC3A0B274700229");

                entity.Property(e => e.DeliveryId).HasColumnName("deliveryId");
                entity.Property(e => e.DeliveryAddress)
                    .HasMaxLength(255)
                    .HasColumnName("deliveryAddress");
                entity.Property(e => e.DeliveryDate)
                    .HasColumnType("datetime")
                    .HasColumnName("deliveryDate");
                entity.Property(e => e.DeliveryPersonnelId).HasColumnName("deliveryPersonnelId");
                entity.Property(e => e.DeliveryStatus)
                    .HasMaxLength(20)
                    .HasColumnName("deliveryStatus");
                entity.Property(e => e.OrderId).HasColumnName("orderId");
                entity.Property(e => e.PickupLocation)
                    .HasMaxLength(255)
                    .HasColumnName("pickupLocation");

                entity.HasOne(d => d.DeliveryPersonnel).WithMany(p => p.Deliveries)
                    .HasForeignKey(d => d.DeliveryPersonnelId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Deliverie__deliv__5FB337D6");

                entity.HasOne(d => d.Order).WithMany(p => p.Deliveries)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Deliverie__order__5EBF139D");
            });

            modelBuilder.Entity<Flower>(entity =>
            {
                entity.HasKey(e => e.FlowerId).HasName("PK__Flowers__8A622B3ECA974058");

                entity.Property(e => e.FlowerId).HasColumnName("flowerId");
                entity.Property(e => e.CategoryId).HasColumnName("categoryId");
                entity.Property(e => e.Condition)
                    .HasMaxLength(50)
                    .HasColumnName("condition");
                entity.Property(e => e.FlowerName)
                    .HasMaxLength(100)
                    .HasColumnName("flowerName");
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .HasColumnName("imageUrl");
                entity.Property(e => e.ListingDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime")
                    .HasColumnName("listingDate");
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("price");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.UserId).HasColumnName("userId");
                entity.Property(e => e.Status)
                    .HasMaxLength(20)
                    .HasColumnName("status");
                entity.HasOne(d => d.Category).WithMany(p => p.Flowers)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Flowers__categor__5165187F");
                entity.HasOne(d => d.Seller).WithMany(p => p.Flowers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Flowers__sellerI__5070F446");
            });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.MessageId)
                .HasColumnName("messageId");

            entity.Property(e => e.ConversationId)
                .HasColumnName("conversationId");

            entity.Property(e => e.SenderId)
                .HasColumnName("senderId");

            entity.Property(e => e.MessageContent)
                .HasColumnName("messageContent");

            entity.Property(e => e.ImageUrl)
            .HasMaxLength(1000)
            .IsUnicode(false);

            entity.Property(e => e.SendTime)
                .HasColumnType("datetime")
                .HasColumnName("sendTime")
                .HasDefaultValueSql("(getdate())");

            entity.Property(e => e.IsRead)
                .HasColumnName("isRead")
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeleted)
                .HasColumnName("isDeleted")
                .HasDefaultValue(false);

            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime")
                .HasColumnName("deletedAt")
                .IsRequired(false);

            // Navigation properties
            entity.HasOne(d => d.Conversation)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Conversations");

            entity.HasOne(d => d.Sender)
                .WithMany(p => p.Messages)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Messages_Users");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifications__NotificationId");

            entity.Property(e => e.NotificationId)
                .HasColumnName("notificationId")
                .UseIdentityColumn();

            entity.Property(e => e.UserId)
                .HasColumnName("userId");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.Property(e => e.Content)
                .HasMaxLength(1000)
                .HasColumnName("content");

            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("isRead");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");

            entity.Property(e => e.RelatedId)
                .HasColumnName("relatedId");

            entity.Property(e => e.RelatedType)
                .HasMaxLength(50)
                .HasColumnName("relatedType");

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");

            entity.HasOne(d => d.User)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__0809335D68AED2CC");

            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.DeliveryAddress)
                .HasMaxLength(255)
                .HasColumnName("deliveryAddress");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("orderDate");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasColumnName("orderStatus");

            entity.Property(e => e.OrderDelivery)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("orderDelivery");
            entity.Property(e => e.ToDistrictId).HasColumnName("toDistrictId");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("totalAmount")
                .HasDefaultValue(0.00m);

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__userId__5629CD9C");
        });

        modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId).HasName("PK__Order_It__3724BD5298E0FED9");

                entity.ToTable("Order_Items");

                entity.Property(e => e.OrderItemId).HasColumnName("orderItemId");
                entity.Property(e => e.FlowerId).HasColumnName("flowerId");
                entity.Property(e => e.OrderId).HasColumnName("orderId");
                entity.Property(e => e.Price)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("price");
                entity.Property(e => e.Quantity).HasColumnName("quantity");
                entity.Property(e => e.FlowerName)
                    .HasColumnName("flowerName")
                    .HasMaxLength(255); 
                entity.HasOne(d => d.Flower).WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.FlowerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Order_Ite__flowe__5BE2A6F2");

                entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Order_Ite__order__5AEE82B9");
            });

        modelBuilder.Entity<OrderReport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId).HasName("PK__Payments__A0D9EFC69AF0913E");

                entity.Property(e => e.PaymentId).HasColumnName("paymentId");
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("amount");
                entity.Property(e => e.OrderId).HasColumnName("orderId");
                entity.Property(e => e.PaymentDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime")
                    .HasColumnName("paymentDate");
                entity.Property(e => e.PaymentStatus)
                    .HasMaxLength(20)
                    .HasColumnName("paymentStatus");

                entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Payments__orderI__787EE5A0");
            });

        modelBuilder.Entity<SellerFollow>(entity =>
        {
            entity.HasKey(e => e.FollowId).HasName("PK__SellerFollow__FollowId");

            entity.Property(e => e.FollowId).HasColumnName("followId");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.SellerId).HasColumnName("sellerId");
            entity.Property(e => e.FollowDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("followDate");

            // Thiết lập quan hệ với bảng Users (người theo dõi - buyer)
            entity.HasOne(d => d.User)
                .WithMany(p => p.Following)  // Quan hệ 1-n (1 người có thể theo dõi nhiều seller)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)  // Không tự động xóa các bản ghi liên quan
                .HasConstraintName("FK_SellerFollow_User");

            // Thiết lập quan hệ với bảng Users (người được theo dõi - seller)
            entity.HasOne(d => d.Seller)
                .WithMany(p => p.Followers)  // Quan hệ 1-n (1 seller có nhiều người theo dõi)
                .HasForeignKey(d => d.SellerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerFollow_Seller");
        });

        modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__2ECD6E04CFF84127");

                entity.Property(e => e.ReviewId).HasColumnName("reviewId");
                entity.Property(e => e.FlowerId).HasColumnName("flowerId");
                entity.Property(e => e.Rating).HasColumnName("rating");
                entity.Property(e => e.ReviewComment)
                    .HasMaxLength(255)
                    .HasColumnName("reviewComment");
                entity.Property(e => e.ReviewDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime")
                    .HasColumnName("reviewDate");
                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Flower).WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.FlowerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Reviews__flowerI__6EF57B66");

                entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Reviews__userId__6E01572D");
            });

            modelBuilder.Entity<SellerRegistrationRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.Property(e => e.RequestId).HasColumnName("requestId");
                entity.Property(e => e.UserId).HasColumnName("userId");
                entity.Property(e => e.StoreName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20).IsRequired();
                entity.Property(e => e.IdCard).HasMaxLength(12).IsRequired();
                entity.Property(e => e.RequestDate).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.ProcessedDate).HasColumnType("datetime");

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_SellerRegistrationRequests_Users");
            });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__CB9A1CFF4D60B102");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164A4964FD0").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("fullName");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RegistrationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("registrationDate");
            entity.Property(e => e.UserType)
                .HasMaxLength(20)
                .HasColumnName("userType");
            entity.Property(e => e.ProfileImageUrl)
              .HasMaxLength(20)
              .HasColumnName("profileImageUrl");
    });

        modelBuilder.Entity<WithdrawalRequest>()
        .HasKey(w => w.RequestId); // Đảm bảo rằng RequestId được chỉ định là khóa chính

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__23CAF1D89BCF6E4B");

            entity.Property(e => e.CategoryId).HasColumnName("categoryId");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("categoryName");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}