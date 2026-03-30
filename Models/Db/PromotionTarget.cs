using System;
using System.Collections.Generic;

namespace OneManVekery.Models.Db;

public partial class PromotionTarget
{
    public int Id { get; set; }

    public int PromotionId { get; set; }

    public string TargetType { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? ProductId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
