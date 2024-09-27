using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI_FlowerShopSWP.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string PaymentStatus { get; set; } = null!;

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }
}
