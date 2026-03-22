using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace OneManVekery.Services;

public interface IStoreOrderService
{
    OrderReceiptRecord CreateOrder(
        CartCheckoutSnapshot checkout,
        IReadOnlyList<CartLineRecord> items,
        decimal deliveryFee,
        string paymentMethodLabel);

    OrderReceiptRecord? GetOrder(string orderNumber);
}

public sealed record CartCheckoutSnapshot
{
    public string CustomerName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string DeliveryAddress { get; init; } = string.Empty;

    public string PaymentMethodCode { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}

public sealed record OrderReceiptLineRecord
{
    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}

public sealed record OrderReceiptRecord
{
    public string OrderNumber { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string DeliveryAddress { get; init; } = string.Empty;

    public string PaymentMethodCode { get; init; } = string.Empty;

    public string PaymentMethodLabel { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string CurrentStatusCode { get; init; } = "paid";

    public IReadOnlyList<OrderReceiptLineRecord> Items { get; init; } = [];

    public decimal Subtotal { get; init; }

    public decimal DeliveryFee { get; init; }

    public decimal Total => Subtotal + DeliveryFee;
}

public sealed class SessionStoreOrderService : IStoreOrderService
{
    private const string SessionKey = "one-man-vekery-orders";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionStoreOrderService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public OrderReceiptRecord CreateOrder(
        CartCheckoutSnapshot checkout,
        IReadOnlyList<CartLineRecord> items,
        decimal deliveryFee,
        string paymentMethodLabel)
    {
        var timestamp = DateTimeOffset.Now;
        var orders = ReadOrders();
        var subtotal = items.Sum(item => item.LineTotal);

        var order = new OrderReceiptRecord
        {
            OrderNumber = $"OVK-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{Random.Shared.Next(100, 999)}",
            CreatedAt = timestamp,
            CustomerName = checkout.CustomerName,
            PhoneNumber = checkout.PhoneNumber,
            DeliveryAddress = checkout.DeliveryAddress,
            PaymentMethodCode = checkout.PaymentMethodCode,
            PaymentMethodLabel = paymentMethodLabel,
            Notes = checkout.Notes,
            CurrentStatusCode = "paid",
            Items = items.Select(item => new OrderReceiptLineRecord
            {
                Name = item.Name,
                Category = item.Category,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList(),
            Subtotal = subtotal,
            DeliveryFee = deliveryFee
        };

        orders.Insert(0, order);
        WriteOrders(orders);

        return order;
    }

    public OrderReceiptRecord? GetOrder(string orderNumber)
    {
        return ReadOrders()
            .FirstOrDefault(order => string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
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

    private List<OrderReceiptRecord> ReadOrders()
    {
        var raw = Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<OrderReceiptRecord>>(raw) ?? [];
    }

    private void WriteOrders(List<OrderReceiptRecord> orders)
    {
        Session.SetString(SessionKey, JsonSerializer.Serialize(orders));
    }
}
