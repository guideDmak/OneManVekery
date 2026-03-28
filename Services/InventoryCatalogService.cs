using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneManVekery.Models.Db;

namespace OneManVekery.Services;

public interface IInventoryCatalogService
{
    IReadOnlyList<InventoryItemRecord> GetAllItems();

    InventoryItemRecord? GetItem(int itemId);

    bool SkuExists(string sku, int? excludingItemId = null);

    InventoryItemRecord AddItem(InventoryItemInput input);

    bool UpdateItem(int itemId, InventoryItemInput input);

    bool SetPublishedState(int itemId, bool isPublished);

    bool AdjustStock(int itemId, int quantityDelta);
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

public sealed class DbInventoryCatalogService : IInventoryCatalogService
{
    private const int DefaultReorderLevel = 10;

    private readonly OneManVekeryDBContext _dbContext;

    public DbInventoryCatalogService(OneManVekeryDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<InventoryItemRecord> GetAllItems()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .ToList()
            .Select(MapProduct)
            .ToList();
    }

    public InventoryItemRecord? GetItem(int itemId)
    {
        if (itemId <= 0)
        {
            return null;
        }

        var product = _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == itemId);

        return product is null ? null : MapProduct(product);
    }

    public bool SkuExists(string sku, int? excludingItemId = null)
    {
        var normalizedSku = NormalizeSku(sku);

        return _dbContext.Products
            .AsNoTracking()
            .Any(item => item.Sku.ToUpper() == normalizedSku && item.Id != excludingItemId);
    }

    public InventoryItemRecord AddItem(InventoryItemInput input)
    {
        var category = ResolveOrCreateCategory(input.Category);
        var product = new Product
        {
            CategoryId = category.Id,
            Sku = NormalizeSku(input.Sku),
            Name = NormalizeText(input.Name),
            Description = SerializeInventoryMeta(input),
            Price = NormalizePrice(input.Price),
            StockQty = Math.Max(0, input.StockQuantity),
            ImageUrl = NormalizeImagePath(input.ImagePath),
            IsActive = input.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.SaveChanges();
        product.Category = category;

        return MapProduct(product);
    }

    public bool UpdateItem(int itemId, InventoryItemInput input)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        var category = ResolveOrCreateCategory(input.Category);
        product.CategoryId = category.Id;
        product.Category = category;
        product.Sku = NormalizeSku(input.Sku);
        product.Name = NormalizeText(input.Name);
        product.Description = SerializeInventoryMeta(input);
        product.Price = NormalizePrice(input.Price);
        product.StockQty = Math.Max(0, input.StockQuantity);
        product.ImageUrl = NormalizeImagePath(input.ImagePath);
        product.IsActive = input.IsPublished;

        _dbContext.SaveChanges();
        return true;
    }

    public bool SetPublishedState(int itemId, bool isPublished)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        product.IsActive = isPublished;
        _dbContext.SaveChanges();
        return true;
    }

    public bool AdjustStock(int itemId, int quantityDelta)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        var nextStock = product.StockQty + quantityDelta;
        if (nextStock < 0)
        {
            return false;
        }

        product.StockQty = nextStock;
        _dbContext.SaveChanges();
        return true;
    }

    private Category ResolveOrCreateCategory(string categoryName)
    {
        var normalizedCategoryName = string.IsNullOrWhiteSpace(categoryName)
            ? "Bakery"
            : NormalizeText(categoryName);
        var comparisonName = normalizedCategoryName.ToUpperInvariant();

        var existingCategory = _dbContext.Categories
            .FirstOrDefault(category => category.Name.ToUpper() == comparisonName);

        if (existingCategory is not null)
        {
            return existingCategory;
        }

        var newCategory = new Category
        {
            Name = normalizedCategoryName
        };

        _dbContext.Categories.Add(newCategory);
        _dbContext.SaveChanges();
        return newCategory;
    }

    private static InventoryItemRecord MapProduct(Product product)
    {
        var meta = ParseInventoryMeta(product.Description);

        return new InventoryItemRecord
        {
            ItemId = product.Id,
            ItemCode = $"ITM-{product.Id:0000}",
            Name = product.Name,
            Category = product.Category?.Name ?? "Bakery",
            Sku = product.Sku,
            Price = product.Price,
            StockQuantity = product.StockQty,
            ReorderLevel = meta.ReorderLevel,
            Tagline = meta.Tagline,
            Notes = meta.Notes,
            ImagePath = NormalizeImagePath(product.ImageUrl),
            IsPublished = product.IsActive,
            UpdatedAt = product.CreatedAt
        };
    }

    private static string SerializeInventoryMeta(InventoryItemInput input)
    {
        var payload = new InventoryMetaStorage
        {
            Tagline = NormalizeText(input.Tagline),
            Notes = NormalizeText(input.Notes),
            ReorderLevel = Math.Max(0, input.ReorderLevel)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static InventoryMeta ParseInventoryMeta(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new InventoryMeta(string.Empty, string.Empty, DefaultReorderLevel);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<InventoryMetaStorage>(description);
            if (payload is not null)
            {
                return new InventoryMeta(
                    NormalizeText(payload.Tagline),
                    NormalizeText(payload.Notes),
                    payload.ReorderLevel < 0 ? DefaultReorderLevel : payload.ReorderLevel);
            }
        }
        catch (JsonException)
        {
        }

        return new InventoryMeta(NormalizeText(description), string.Empty, DefaultReorderLevel);
    }

    private static decimal NormalizePrice(decimal price)
    {
        return Math.Round(Math.Max(0, price), 2);
    }

    private static string NormalizeText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeSku(string value)
    {
        return NormalizeText(value).ToUpperInvariant();
    }

    private static string NormalizeImagePath(string? value)
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

    private sealed class InventoryMetaStorage
    {
        public string Tagline { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public int ReorderLevel { get; set; } = DefaultReorderLevel;
    }

    private sealed record InventoryMeta(string Tagline, string Notes, int ReorderLevel);
}
