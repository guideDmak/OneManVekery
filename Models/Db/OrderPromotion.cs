using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class OrderPromotion
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? PromotionId { get; set; }

    public int? PromoCodeId { get; set; }

    public string PromotionTitle { get; set; } = null!;

    public string BenefitType { get; set; } = null!;

    public decimal DiscountAmount { get; set; }

    public decimal ShippingDiscountAmount { get; set; }

    public int PointsEarned { get; set; }

    public int PointsRedeemed { get; set; }

    public int? RewardProductId { get; set; }

    public string? RewardProductName { get; set; }

    public int? RewardQty { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PromoCode? PromoCode { get; set; }

    public virtual Promotion? Promotion { get; set; }

    public virtual Product? RewardProduct { get; set; }
}
