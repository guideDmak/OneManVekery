namespace OneManVekery.Models;

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

public sealed record CartSessionItem
{
    public string ProductId { get; init; } = string.Empty;

    public int Quantity { get; init; }
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
