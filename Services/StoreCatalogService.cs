using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Services;

public interface IStoreCatalogService
{
    IReadOnlyList<ProductCardViewModel> GetProducts();

    ProductCardViewModel? GetProductById(string productId);
}

public sealed class DbStoreCatalogService : IStoreCatalogService
{
    private const int DefaultReorderLevel = 10;
    private readonly OneManVekeryDBContext _dbContext;

    public DbStoreCatalogService(OneManVekeryDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<ProductCardViewModel> GetProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToList()
            .Select(MapProduct)
            .ToList();
    }

    public ProductCardViewModel? GetProductById(string productId)
    {
        if (!int.TryParse(productId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue))
        {
            return null;
        }

        var product = _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == productIdValue && item.IsActive);

        return product is null ? null : MapProduct(product);
    }

    private static ProductCardViewModel MapProduct(Product product)
    {
        var meta = ParseProductMeta(product.Description);
        var category = string.IsNullOrWhiteSpace(product.Category?.Name) ? "Bakery" : product.Category!.Name;
        var themeKey = ResolveThemeKey(category, product.Name, product.ImageUrl);
        var isSoldOut = product.StockQty <= 0;

        return new ProductCardViewModel
        {
            ProductId = product.Id.ToString(CultureInfo.InvariantCulture),
            Name = product.Name,
            Category = category,
            Description = ResolveDescription(product.Description, meta),
            Price = product.Price,
            Badge = isSoldOut
                ? "หมดแล้ว"
                : product.StockQty <= meta.ReorderLevel
                    ? "ใกล้หมด"
                    : string.Empty,
            ThemeKey = themeKey,
            ImagePath = NormalizeImagePath(product.ImageUrl, themeKey),
            IsSoldOut = isSoldOut
        };
    }

    private static string ResolveDescription(string? description, ProductMeta meta)
    {
        if (!string.IsNullOrWhiteSpace(meta.Tagline))
        {
            return meta.Tagline;
        }

        if (!string.IsNullOrWhiteSpace(meta.Notes))
        {
            return meta.Notes;
        }

        return string.IsNullOrWhiteSpace(description) || LooksLikeJson(description)
            ? "สดใหม่จากครัวของร้านในทุกออเดอร์"
            : description.Trim();
    }

    private static ProductMeta ParseProductMeta(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new ProductMeta(string.Empty, string.Empty, DefaultReorderLevel);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ProductMetaStorage>(description);
            if (payload is not null)
            {
                return new ProductMeta(
                    payload.Tagline?.Trim() ?? string.Empty,
                    payload.Notes?.Trim() ?? string.Empty,
                    payload.ReorderLevel < 0 ? DefaultReorderLevel : payload.ReorderLevel);
            }
        }
        catch (JsonException)
        {
        }

        return new ProductMeta(string.Empty, string.Empty, DefaultReorderLevel);
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string ResolveThemeKey(string category, string name, string? imagePath)
    {
        var source = $"{category} {name} {imagePath}".ToLowerInvariant();

        if (source.Contains("macaron"))
        {
            return "macaron";
        }

        if (source.Contains("berry") || source.Contains("blue") || source.Contains("cherry"))
        {
            return "berry";
        }

        if (source.Contains("milk"))
        {
            return "milk";
        }

        if (source.Contains("cream") || source.Contains("choux") || source.Contains("eclair"))
        {
            return "cream";
        }

        if (source.Contains("cake") || source.Contains("cheese"))
        {
            return "cake";
        }

        return "gold";
    }

    private static string NormalizeImagePath(string? imagePath, string themeKey)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return themeKey switch
            {
                "macaron" => "/images/theme-macaron.svg",
                "berry" => "/images/theme-berry.svg",
                "milk" => "/images/theme-milk.svg",
                "cream" => "/images/theme-cream.svg",
                "cake" => "/images/theme-cake.svg",
                _ => "/images/theme-gold.svg"
            };
        }

        var normalized = imagePath.Trim();
        return normalized.StartsWith("~/", StringComparison.Ordinal)
            ? "/" + normalized[2..]
            : normalized;
    }

    private sealed class ProductMetaStorage
    {
        public string? Tagline { get; set; }

        public string? Notes { get; set; }

        public int ReorderLevel { get; set; } = DefaultReorderLevel;
    }

    private sealed record ProductMeta(string Tagline, string Notes, int ReorderLevel);
}
