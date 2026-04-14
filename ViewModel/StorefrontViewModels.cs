using System.ComponentModel.DataAnnotations;

namespace OneManVekery.ViewModel;

public class HomeIndexViewModel
{
    public IReadOnlyList<CategoryCardViewModel> Categories { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> Products { get; init; } = [];

    public IReadOnlyList<ProductCardViewModel> NewArrivals { get; init; } = [];
}

public class ShopPageViewModel
{
    public IReadOnlyList<ProductCardViewModel> Products { get; init; } = [];

    public IReadOnlyList<string> Categories { get; init; } = [];
}

public class CartPageViewModel
{
    public IReadOnlyList<CartLineViewModel> Items { get; init; } = [];

    public IReadOnlyList<PaymentOptionViewModel> PaymentOptions { get; init; } = [];

    public IReadOnlyList<CheckoutBenefitViewModel> AppliedBenefits { get; init; } = [];

    public CartCheckoutViewModel Checkout { get; init; } = new();

    public int ItemCount { get; init; }

    public decimal Subtotal { get; init; }

    public decimal DeliveryFee { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal ShippingDiscountAmount { get; init; }

    public string AppliedPromoCode { get; init; } = string.Empty;

    public string AppliedPromoTitle { get; init; } = string.Empty;

    public string AppliedPromoDescription { get; init; } = string.Empty;

    public string PromoMessage { get; init; } = string.Empty;

    public string PromoMessageState { get; init; } = string.Empty;

    public int CurrentPoints { get; init; }

    public int PointsEarned { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal PointsDiscountAmount { get; init; }

    public int MaxPointDiscountRedeem { get; init; }

    public string PointDiscountRateLabel { get; init; } = string.Empty;

    public int ProjectedPointsBalance { get; init; }

    public int PointsNeededForFreeItem { get; init; }

    public int RewardPointCost { get; init; }

    public int RewardQty { get; init; } = 1;

    public string RewardProductName { get; init; } = string.Empty;

    public bool CanRedeemFreeItem { get; init; }

    public bool WillRedeemFreeItem => Checkout.UsePointsReward && CanRedeemFreeItem;

    public decimal TotalSavings => DiscountAmount + ShippingDiscountAmount;

    public decimal Total => Math.Max(0, Subtotal + DeliveryFee - TotalSavings);

    public bool HasItems => Items.Count > 0;

    public bool HasAppliedPromotion => !string.IsNullOrWhiteSpace(AppliedPromoCode) && TotalSavings > 0;

    public bool HasPromoMessage => !string.IsNullOrWhiteSpace(PromoMessage);
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

    public IReadOnlyList<CheckoutBenefitViewModel> AppliedBenefits { get; init; } = [];

    public decimal Subtotal { get; init; }

    public decimal DeliveryFee { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal ShippingDiscountAmount { get; init; }

    public string AppliedPromoCode { get; init; } = string.Empty;

    public string AppliedPromoTitle { get; init; } = string.Empty;

    public int PointsEarned { get; init; }

    public int PointsRedeemed { get; init; }

    public int PointsBalanceAfter { get; init; }

    public decimal TotalSavings => DiscountAmount + ShippingDiscountAmount;

    public decimal Total => Math.Max(0, Subtotal + DeliveryFee - TotalSavings);

    public bool HasAppliedPromotion => !string.IsNullOrWhiteSpace(AppliedPromoCode) && TotalSavings > 0;
}

public class MyOrdersPageViewModel
{
    public IReadOnlyList<MyOrderCardViewModel> Orders { get; init; } = [];

    public int OrderCount { get; init; }

    public string TotalSpendLabel { get; init; } = string.Empty;

    public string LastOrderLabel { get; init; } = string.Empty;

    public bool HasOrders => Orders.Count > 0;
}

public class MyOrderCardViewModel
{
    public string OrderNumber { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }

    public string ProductSummary { get; init; } = string.Empty;

    public string ItemCountLabel { get; init; } = string.Empty;

    public string TotalAmountLabel { get; init; } = string.Empty;

    public string PaymentMethodLabel { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusDescription { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public string PromoLabel { get; init; } = string.Empty;

    public int PointsEarned { get; init; }

    public int PointsRedeemed { get; init; }
}

public class AboutPageViewModel
{
    public string StoryTitle { get; init; } = string.Empty;

    public IReadOnlyList<string> StoryParagraphs { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Values { get; init; } = [];
}

public class ContactPageViewModel
{
    public ContactFormViewModel Form { get; init; } = new();

    public string HeadingTitle { get; init; } = string.Empty;

    public IReadOnlyList<ContactInfoCardViewModel> ContactCards { get; init; } = [];
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

public class CheckoutBenefitViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ValueLabel { get; init; } = string.Empty;

    public string Tone { get; init; } = string.Empty;
}

public class ServiceFeatureViewModel
{
    public string IconText { get; init; } = string.Empty;

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
    [Display(Name = "โค้ดส่วนลด")]
    public string? PromoCode { get; set; }

    [Display(Name = "ใช้พอยต์แลกขนมฟรี")]
    public bool UsePointsReward { get; set; }

    [Display(Name = "ใช้พอยต์ลดราคา")]
    [Range(0, int.MaxValue, ErrorMessage = "จำนวนพอยต์ต้องไม่ติดลบ")]
    public int PointsToRedeem { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้รับ")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกที่อยู่จัดส่ง")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวิธีชำระเงิน")]
    public string PaymentMethod { get; set; } = "promptpay";

    public string? Notes { get; set; }
}
