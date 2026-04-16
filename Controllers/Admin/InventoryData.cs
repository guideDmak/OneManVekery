using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using OneManVekery.Models.Db;
using OneManVekery.Models;
using OneManVekery.ViewModel;
using System.Globalization;

namespace OneManVekery.Controllers;

public partial class AdminController
{
    private IReadOnlyList<InventoryItemRecord> GetAllInventoryItems()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .ToList()
            .Select(MapInventoryProduct)
            .ToList();
    }

    private InventoryItemRecord? GetInventoryItem(int itemId)
    {
        if (itemId <= 0)
        {
            return null;
        }

        var product = _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == itemId);

        return product is null ? null : MapInventoryProduct(product);
    }

    private bool SkuExists(string sku, int? excludingItemId = null)
    {
        var normalizedSku = NormalizeInventorySku(sku);

        return _dbContext.Products
            .AsNoTracking()
            .Any(item => item.Sku.ToUpper() == normalizedSku && item.Id != excludingItemId);
    }

    private InventoryItemRecord AddInventoryItem(InventoryItemInput input)
    {
        var category = ResolveOrCreateCategory(input.Category);
        var product = new Product
        {
            CategoryId = category.Id,
            Sku = NormalizeInventorySku(input.Sku),
            Name = NormalizeInventoryText(input.Name),
            Description = SerializeInventoryMeta(input),
            Price = NormalizeInventoryPrice(input.Price),
            StockQty = Math.Max(0, input.StockQuantity),
            ImageUrl = NormalizeInventoryImagePath(input.ImagePath),
            IsActive = input.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.SaveChanges();
        product.Category = category;

        return MapInventoryProduct(product);
    }

    private bool UpdateInventoryItem(int itemId, InventoryItemInput input)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        var category = ResolveOrCreateCategory(input.Category);
        product.CategoryId = category.Id;
        product.Category = category;
        product.Sku = NormalizeInventorySku(input.Sku);
        product.Name = NormalizeInventoryText(input.Name);
        product.Description = SerializeInventoryMeta(input);
        product.Price = NormalizeInventoryPrice(input.Price);
        product.StockQty = Math.Max(0, input.StockQuantity);
        product.ImageUrl = NormalizeInventoryImagePath(input.ImagePath);
        product.IsActive = input.IsPublished;

        _dbContext.SaveChanges();
        return true;
    }

    private bool SetPublishedState(int itemId, bool isPublished, string? visibilityNote = null)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        product.IsActive = isPublished;

        if (visibilityNote is not null)
        {
            var meta = ParseInventoryMeta(product.Description);
            product.Description = SerializeInventoryMeta(new InventoryMeta(meta.Tagline, NormalizeInventoryText(visibilityNote), meta.ReorderLevel));
        }

        _dbContext.SaveChanges();
        return true;
    }

    private bool AdjustInventoryStock(int itemId, int quantityDelta)
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
            : NormalizeInventoryText(categoryName);
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

    private static InventoryItemRecord MapInventoryProduct(Product product)
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
            ImagePath = NormalizeInventoryImagePath(product.ImageUrl),
            IsPublished = product.IsActive,
            UpdatedAt = product.CreatedAt
        };
    }

    private static string SerializeInventoryMeta(InventoryItemInput input)
    {
        var payload = new InventoryMetaStorage
        {
            Tagline = NormalizeInventoryText(input.Tagline),
            Notes = NormalizeInventoryText(input.Notes),
            ReorderLevel = Math.Max(0, input.ReorderLevel)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeInventoryMeta(InventoryMeta input)
    {
        var payload = new InventoryMetaStorage
        {
            Tagline = NormalizeInventoryText(input.Tagline),
            Notes = NormalizeInventoryText(input.Notes),
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
                    NormalizeInventoryText(payload.Tagline),
                    NormalizeInventoryText(payload.Notes),
                    payload.ReorderLevel < 0 ? DefaultReorderLevel : payload.ReorderLevel);
            }
        }
        catch (JsonException)
        {
        }

        return new InventoryMeta(NormalizeInventoryText(description), string.Empty, DefaultReorderLevel);
    }

    private static decimal NormalizeInventoryPrice(decimal price)
    {
        return Math.Round(Math.Max(0, price), 2);
    }

    private static string NormalizeInventoryText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeInventorySku(string value)
    {
        return NormalizeInventoryText(value).ToUpperInvariant();
    }

    private static string NormalizeInventoryImagePath(string? value)
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

    private static InventoryItemInput CreateInventoryInput(AdminItemEditorViewModel form)
    {
        return new InventoryItemInput
        {
            Name = form.Name,
            Category = form.Category,
            Sku = form.Sku,
            Price = form.Price,
            StockQuantity = form.StockQuantity,
            ReorderLevel = form.ReorderLevel,
            Tagline = form.Tagline,
            Notes = form.Notes,
            ImagePath = form.ImagePath,
            IsPublished = form.IsPublished
        };
    }
}
