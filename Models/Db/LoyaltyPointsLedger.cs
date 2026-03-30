using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class LoyaltyPointsLedger
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? OrderId { get; set; }

    public int? PromotionId { get; set; }

    public string EntryType { get; set; } = null!;

    public int PointsDelta { get; set; }

    public int BalanceAfter { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Promotion? Promotion { get; set; }

    public virtual User User { get; set; } = null!;
}
