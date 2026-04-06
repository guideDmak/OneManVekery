using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class LoyaltyWallet
{
    public int UserId { get; set; }

    public int CurrentPoints { get; set; }

    public int LifetimeEarned { get; set; }

    public int LifetimeRedeemed { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
