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

    public int? PromoCodeId { get; set; }

    public string? DiscountCode { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingDiscountAmount { get; set; }

    public int PointsEarned { get; set; }

    public int PointsRedeemed { get; set; }

    public virtual ICollection<LoyaltyPointsLedger> LoyaltyPointsLedgers { get; set; } = new List<LoyaltyPointsLedger>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();

    public virtual PromoCode? PromoCode { get; set; }

    public virtual User? User { get; set; }
}
