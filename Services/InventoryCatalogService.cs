namespace OneManVekery.Services;

public interface IInventoryCatalogService
{
    IReadOnlyList<InventoryItemRecord> GetAllItems();

    InventoryItemRecord? GetItem(Guid itemId);

    bool SkuExists(string sku, Guid? excludingItemId = null);

    InventoryItemRecord AddItem(InventoryItemInput input);

    bool UpdateItem(Guid itemId, InventoryItemInput input);

    bool AdjustStock(Guid itemId, int quantityDelta);
}

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
    public Guid ItemId { get; init; }

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

public sealed class InMemoryInventoryCatalogService : IInventoryCatalogService
{
    private readonly object _sync = new();
    private readonly List<InventoryItemRecord> _items;

    public InMemoryInventoryCatalogService()
    {
        _items = SeedItems();
    }

    public IReadOnlyList<InventoryItemRecord> GetAllItems()
    {
        lock (_sync)
        {
            return _items
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(Clone)
                .ToList();
        }
    }

    public InventoryItemRecord? GetItem(Guid itemId)
    {
        lock (_sync)
        {
            var item = _items.FirstOrDefault(entry => entry.ItemId == itemId);
            return item is null ? null : Clone(item);
        }
    }

    public bool SkuExists(string sku, Guid? excludingItemId = null)
    {
        var normalizedSku = NormalizeSku(sku);

        lock (_sync)
        {
            return _items.Any(item =>
                string.Equals(item.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase) &&
                item.ItemId != excludingItemId);
        }
    }

    public InventoryItemRecord AddItem(InventoryItemInput input)
    {
        lock (_sync)
        {
            var item = CreateRecord(Guid.NewGuid(), GenerateItemCode(), input, DateTime.Now);
            _items.Add(item);
            return Clone(item);
        }
    }

    public bool UpdateItem(Guid itemId, InventoryItemInput input)
    {
        lock (_sync)
        {
            var index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return false;
            }

            _items[index] = CreateRecord(itemId, _items[index].ItemCode, input, DateTime.Now);
            return true;
        }
    }

    public bool AdjustStock(Guid itemId, int quantityDelta)
    {
        lock (_sync)
        {
            var index = _items.FindIndex(item => item.ItemId == itemId);
            if (index < 0)
            {
                return false;
            }

            var item = _items[index];
            var nextStock = item.StockQuantity + quantityDelta;
            if (nextStock < 0)
            {
                return false;
            }

            _items[index] = item with
            {
                StockQuantity = nextStock,
                UpdatedAt = DateTime.Now
            };

            return true;
        }
    }

    private static InventoryItemRecord CreateRecord(Guid itemId, string itemCode, InventoryItemInput input, DateTime updatedAt)
    {
        return new InventoryItemRecord
        {
            ItemId = itemId,
            ItemCode = itemCode,
            Name = NormalizeText(input.Name),
            Category = NormalizeText(input.Category),
            Sku = NormalizeSku(input.Sku),
            Price = Math.Round(input.Price, 2),
            StockQuantity = input.StockQuantity,
            ReorderLevel = input.ReorderLevel,
            Tagline = NormalizeText(input.Tagline),
            Notes = NormalizeText(input.Notes),
            ImagePath = NormalizeImagePath(input.ImagePath),
            IsPublished = input.IsPublished,
            UpdatedAt = updatedAt
        };
    }

    private static InventoryItemRecord Clone(InventoryItemRecord item)
    {
        return item with { };
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }

    private static string NormalizeSku(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string NormalizeImagePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/images/theme-cake.svg";
        }

        var normalized = value.Trim();
        return normalized.StartsWith("~/", StringComparison.Ordinal)
            ? "/" + normalized[2..]
            : normalized;
    }

    private string GenerateItemCode()
    {
        var nextNumber = _items
            .Select(item => item.ItemCode)
            .Select(code => code.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
            .Select(value => int.TryParse(value, out var number) ? number : 999)
            .DefaultIfEmpty(1000)
            .Max() + 1;

        return $"ITM-{nextNumber:0000}";
    }

    private static List<InventoryItemRecord> SeedItems()
    {
        var seededAt = DateTime.Now;

        return
        [
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("2C628B3A-9A9E-4CE6-95C9-14D0C87DA001"),
                ItemCode = "ITM-1001",
                Name = "Rose Macaron Box",
                Category = "Macaron",
                Sku = "MC-ROSE-01",
                Price = 120,
                StockQuantity = 32,
                ReorderLevel = 12,
                Tagline = "Gift box best seller for weekend orders",
                Notes = "Keep chilled before delivery rush.",
                ImagePath = "/images/theme-macaron.svg",
                IsPublished = true,
                UpdatedAt = seededAt.AddHours(-6)
            },
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("0A31F6D5-2E8A-4D79-9B0A-14D0C87DA002"),
                ItemCode = "ITM-1002",
                Name = "Strawberry Shortcake",
                Category = "Cake",
                Sku = "CK-STRAW-02",
                Price = 145,
                StockQuantity = 9,
                ReorderLevel = 10,
                Tagline = "Fresh cream cake for birthdays and walk-ins",
                Notes = "Prioritize pre-orders before storefront display.",
                ImagePath = "/images/theme-cake.svg",
                IsPublished = true,
                UpdatedAt = seededAt.AddHours(-3)
            },
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("4AA2D594-0A4B-4D38-88F0-14D0C87DA003"),
                ItemCode = "ITM-1003",
                Name = "Vanilla Choux Cream",
                Category = "Choux Cream",
                Sku = "CH-VNLA-03",
                Price = 55,
                StockQuantity = 48,
                ReorderLevel = 18,
                Tagline = "Fast moving grab-and-go counter item",
                Notes = "Refill display every afternoon.",
                ImagePath = "/images/theme-cream.svg",
                IsPublished = true,
                UpdatedAt = seededAt.AddHours(-2)
            },
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("E9057993-A052-454B-B2AF-14D0C87DA004"),
                ItemCode = "ITM-1004",
                Name = "Butter Croissant",
                Category = "Bakery",
                Sku = "BK-CROIS-04",
                Price = 69,
                StockQuantity = 0,
                ReorderLevel = 15,
                Tagline = "Morning batch only",
                Notes = "Pause storefront listing until next bake finishes.",
                ImagePath = "/images/theme-gold.svg",
                IsPublished = true,
                UpdatedAt = seededAt.AddHours(-5)
            },
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("B456C8E7-2652-4C39-9F89-14D0C87DA005"),
                ItemCode = "ITM-1005",
                Name = "Blueberry Cheesecake",
                Category = "Cake",
                Sku = "CK-BLUE-05",
                Price = 159,
                StockQuantity = 14,
                ReorderLevel = 8,
                Tagline = "Premium slice for cafe pairings",
                Notes = "Highlight in weekend campaign.",
                ImagePath = "/images/theme-berry.svg",
                IsPublished = true,
                UpdatedAt = seededAt.AddHours(-1)
            },
            new InventoryItemRecord
            {
                ItemId = Guid.Parse("6D93A6E1-89FC-4E84-AF74-14D0C87DA006"),
                ItemCode = "ITM-1006",
                Name = "Milk Cloud Roll",
                Category = "Cake",
                Sku = "CK-MILK-06",
                Price = 135,
                StockQuantity = 6,
                ReorderLevel = 6,
                Tagline = "Soft roll cake for afternoon tea sets",
                Notes = "Draft flavor update waiting for final photo.",
                ImagePath = "/images/theme-milk.svg",
                IsPublished = false,
                UpdatedAt = seededAt.AddHours(-7)
            }
        ];
    }
}
