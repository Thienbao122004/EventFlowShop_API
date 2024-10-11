using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace WebAPI_FlowerShopSWP.Models;

public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NotificationId { get; set; }
    public string Message { get; set; }
    public DateTime NotificationDate { get; set; }
    public bool IsRead { get; set; } = false;
    public int SellerId { get; set; }

    public virtual User? Seller { get; set; } = null!;
}
