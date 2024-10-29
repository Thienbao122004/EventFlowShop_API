using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

    namespace WebAPI_FlowerShopSWP.Models
    {
        public class Notification
        {
            [Key]
            public int NotificationId { get; set; }

            [Required]
            public int UserId { get; set; }

            [Required]
            [StringLength(255)]
            public string Title { get; set; }

            [Required]
            [StringLength(1000)]
            public string Content { get; set; }

            [Required]
            [StringLength(50)]
            public string Type { get; set; }

            public bool IsRead { get; set; } = false;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public int? RelatedId { get; set; }

            [StringLength(50)]
            public string RelatedType { get; set; }

            public bool IsActive { get; set; } = true;

            // Navigation property
            [ForeignKey("UserId")]
            public virtual User User { get; set; }
        }
}