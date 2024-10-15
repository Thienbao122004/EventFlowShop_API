using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI_FlowerShopSWP.Models
{
    public class SellerRegistrationRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(255)]
        public string StoreName { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [StringLength(12)]
        [RegularExpression(@"^\d{9,12}$", ErrorMessage = "IdCard must be 9 to 12 digits.")]
        public string IdCard { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? ProcessedDate { get; set; }
    }
}