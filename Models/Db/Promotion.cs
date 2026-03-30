using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class Promotion
{
    public int Id { get; set; }

    public string PromotionKey { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string CampaignType { get; set; } = null!;

    public string RuleType { get; set; } = null!;

    public string BenefitType { get; set; } = null!;

    public string TargetScope { get; set; } = null!;

    public string? RewardScope { get; set; }

    public int Priority { get; set; }

    public bool CanStack { get; set; }

    public bool AutoApply { get; set; }

    public bool RequiresCode { get; set; }

    public decimal? MinOrderAmount { get; set; }

    public int? MinItemQty { get; set; }

    public decimal? SpendStepAmount { get; set; }

    public int? BuyQty { get; set; }

    public int? GetQty { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public bool FreeShipping { get; set; }

    public int? PointsAwarded { get; set; }

    public int? PointsCost { get; set; }

    public int? RewardQty { get; set; }

    public int? RewardProductId { get; set; }

    public int? RewardCategoryId { get; set; }

    public int? WeekdayMask { get; set; }

    public TimeOnly? DailyStartTime { get; set; }

    public TimeOnly? DailyEndTime { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<LoyaltyPointsLedger> LoyaltyPointsLedgers { get; set; } = new List<LoyaltyPointsLedger>();

    public virtual ICollection<OrderPromotion> OrderPromotions { get; set; } = new List<OrderPromotion>();

    public virtual ICollection<PromoCode> PromoCodes { get; set; } = new List<PromoCode>();

    public virtual ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();

    public virtual Category? RewardCategory { get; set; }

    public virtual Product? RewardProduct { get; set; }
}
