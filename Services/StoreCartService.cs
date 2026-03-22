using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OneManVekery.ViewModel;

namespace OneManVekery.Services;

public interface IStoreCartService
{
    IReadOnlyList<CartLineRecord> GetItems();

    int GetItemCount();

    bool AddItem(string productId, int quantity = 1);

    bool UpdateQuantity(string productId, int quantity);

    bool ChangeQuantity(string productId, int delta);

    bool RemoveItem(string productId);

    void Clear();
}

public sealed record CartLineRecord
{
    public string ProductId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public bool IsSoldOut { get; init; }

    public decimal LineTotal => UnitPrice * Quantity;
}

internal sealed record CartSessionItem
{
    public string ProductId { get; init; } = string.Empty;

    public int Quantity { get; init; }
}

public sealed class SessionStoreCartService : IStoreCartService
{
    private const string SessionKey = "one-man-vekery-cart";

    private readonly IStoreCatalogService _storeCatalogService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionStoreCartService(IStoreCatalogService storeCatalogService, IHttpContextAccessor httpContextAccessor)
    {
        _storeCatalogService = storeCatalogService;
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<CartLineRecord> GetItems()
    {
        var catalog = _storeCatalogService.GetProducts().ToDictionary(product => product.ProductId, StringComparer.OrdinalIgnoreCase);

        return ReadItems()
            .Where(item => catalog.ContainsKey(item.ProductId))
            .Select(item =>
            {
                var product = catalog[item.ProductId];

                return new CartLineRecord
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Category = product.Category,
                    Description = product.Description,
                    ImagePath = product.ImagePath,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity,
                    IsSoldOut = product.IsSoldOut
                };
            })
            .ToList();
    }

    public int GetItemCount()
    {
        return GetItems().Sum(item => item.Quantity);
    }

    public bool AddItem(string productId, int quantity = 1)
    {
        var product = _storeCatalogService.GetProductById(productId);
        if (product is null || product.IsSoldOut || quantity <= 0)
        {
            return false;
        }

        var items = ReadItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            items[index] = items[index] with { Quantity = Math.Min(items[index].Quantity + quantity, 99) };
        }
        else
        {
            items.Add(new CartSessionItem
            {
                ProductId = productId,
                Quantity = Math.Min(quantity, 99)
            });
        }

        WriteItems(items);
        return true;
    }

    public bool UpdateQuantity(string productId, int quantity)
    {
        var items = ReadItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        if (quantity <= 0)
        {
            items.RemoveAt(index);
        }
        else
        {
            items[index] = items[index] with { Quantity = Math.Min(quantity, 99) };
        }

        WriteItems(items);
        return true;
    }

    public bool ChangeQuantity(string productId, int delta)
    {
        var currentItem = ReadItems().FirstOrDefault(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (currentItem is null)
        {
            return false;
        }

        return UpdateQuantity(productId, currentItem.Quantity + delta);
    }

    public bool RemoveItem(string productId)
    {
        var items = ReadItems();
        var removedCount = items.RemoveAll(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (removedCount == 0)
        {
            return false;
        }

        WriteItems(items);
        return true;
    }

    public void Clear()
    {
        Session.Remove(SessionKey);
    }

    private ISession Session
    {
        get
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session is null)
            {
                throw new InvalidOperationException("Session is not available for the current request.");
            }

            return session;
        }
    }

    private List<CartSessionItem> ReadItems()
    {
        var raw = Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CartSessionItem>>(raw) ?? [];
    }

    private void WriteItems(List<CartSessionItem> items)
    {
        if (items.Count == 0)
        {
            Session.Remove(SessionKey);
            return;
        }

        Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }
}
