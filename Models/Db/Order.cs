using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class Order
{
    public int Id { get; set; }

    public string OrderNo { get; set; } = null!;

    public int? UserId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? User { get; set; }
}
