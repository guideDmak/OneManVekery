namespace OneManVekery.Models;

public sealed class InventoryItemInput
{
    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public int ReorderLevel { get; init; }

    public string Tagline { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public bool IsPublished { get; init; } = true;
}

public sealed record InventoryItemRecord
{
    public int ItemId { get; init; }

    public string ItemCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public int StockQuantity { get; init; }

    public int ReorderLevel { get; init; }

    public string Tagline { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public DateTime UpdatedAt { get; init; }
}
