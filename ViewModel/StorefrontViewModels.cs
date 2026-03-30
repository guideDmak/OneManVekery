using System.ComponentModel.DataAnnotations;

namespace OneManVekery.ViewModel;

public class HomeIndexViewModel
{
    public IReadOnlyList<CategoryCardViewModel> Categories { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> Products { get; init; } = [];

    public IReadOnlyList<InspirationCardViewModel> Inspirations { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Features { get; init; } = [];
}

public class ShopPageViewModel
{
    public IReadOnlyList<ProductCardViewModel> Products { get; init; } = [];

    public IReadOnlyList<string> Categories { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Features { get; init; } = [];
}

public class CartPageViewModel
{
    public IReadOnlyList<CartLineViewModel> Items { get; init; } = [];

    public IReadOnlyList<PaymentOptionViewModel> PaymentOptions { get; init; } = [];

    public CartCheckoutViewModel Checkout { get; init; } = new();

    public int ItemCount { get; init; }

    public decimal Subtotal { get; init; }

    public decimal DeliveryFee { get; init; }

    public decimal Total => Subtotal + DeliveryFee;

    public bool HasItems => Items.Count > 0;
}

public class OrderStatusPageViewModel
{
    public string OrderNumber { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string DeliveryAddress { get; init; } = string.Empty;

    public string PaymentMethodLabel { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string CurrentStatusLabel { get; init; } = string.Empty;

    public string CurrentStatusDescription { get; init; } = string.Empty;

    public IReadOnlyList<OrderReceiptLineViewModel> Items { get; init; } = [];

    public IReadOnlyList<OrderProgressStepViewModel> StatusSteps { get; init; } = [];

    public decimal Subtotal { get; init; }

    public decimal DeliveryFee { get; init; }

    public decimal Total => Subtotal + DeliveryFee;
}

public class AboutPageViewModel
{
    public string StoryTitle { get; init; } = string.Empty;

    public IReadOnlyList<string> StoryParagraphs { get; init; } = [];

    public string Quote { get; init; } = string.Empty;

    public string QuoteCaption { get; init; } = string.Empty;

    public IReadOnlyList<AboutStatViewModel> Stats { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Values { get; init; } = [];

    public IReadOnlyList<ProcessStepViewModel> Steps { get; init; } = [];
}

public class ContactPageViewModel
{
    public ContactFormViewModel Form { get; init; } = new();

    public string HeadingTitle { get; init; } = string.Empty;

    public string HeadingDescription { get; init; } = string.Empty;

    public IReadOnlyList<ContactInfoCardViewModel> ContactCards { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Features { get; init; } = [];
}

public class CategoryCardViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string ThemeKey { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;
}

public class ProductCardViewModel
{
    public string ProductId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public decimal? OriginalPrice { get; init; }

    public string Badge { get; init; } = string.Empty;

    public string ThemeKey { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public bool IsSoldOut { get; init; }
}

public class CartLineViewModel
{
    public string ProductId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public bool IsSoldOut { get; init; }

    public string UnitPriceLabel { get; init; } = string.Empty;

    public string LineTotalLabel { get; init; } = string.Empty;
}

public class OrderReceiptLineViewModel
{
    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}

public class OrderProgressStepViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Marker { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;
}

public class PaymentOptionViewModel
{
    public string Code { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public class InspirationCardViewModel
{
    public string Number { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string ThemeKey { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;
}

public class ServiceFeatureViewModel
{
    public string IconText { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public class AboutStatViewModel
{
    public string Value { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;
}

public class ProcessStepViewModel
{
    public string Number { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public class ContactInfoCardViewModel
{
    public string IconText { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string LineOne { get; init; } = string.Empty;

    public string LineTwo { get; init; } = string.Empty;
}

public class ContactFormViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกข้อความ")]
    public string Message { get; set; } = string.Empty;
}

public class CartCheckoutViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้รับ")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกที่อยู่จัดส่ง")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวิธีชำระเงิน")]
    public string PaymentMethod { get; set; } = "promptpay";

    public string Notes { get; set; } = string.Empty;
}
