using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OneManVekery.ViewModel;

public class AdminDashboardViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public string TrendLabel { get; init; } = string.Empty;

    public string TrendValue { get; init; } = string.Empty;

    public string TrendDelta { get; init; } = string.Empty;

    public IReadOnlyList<AdminChartPointViewModel> TrendChart { get; init; } = [];

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public IReadOnlyList<AdminDashboardTopProductViewModel> TopProducts { get; init; } = [];

    public IReadOnlyList<AdminOrderRecordViewModel> LatestOrders { get; init; } = [];
}

public class AdminOrdersViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public string UpdateLabel { get; init; } = string.Empty;

    public string UpdateValue { get; init; } = string.Empty;

    public string UpdateDelta { get; init; } = string.Empty;

    public IReadOnlyList<AdminChartPointViewModel> UpdateChart { get; init; } = [];

    public IReadOnlyList<AdminInfoItemViewModel> FulfillmentSummary { get; init; } = [];

    public IReadOnlyList<AdminOrderRecordViewModel> Orders { get; init; } = [];

    public IReadOnlyList<string> OrderStatusOptions { get; init; } = [];

    public IReadOnlyList<string> PaymentStatusOptions { get; init; } = [];

    public IReadOnlyList<string> PaymentMethodOptions { get; init; } = [];

    public IReadOnlyList<AdminSelectOptionViewModel> CustomerOptions { get; init; } = [];

    public IReadOnlyList<AdminSelectOptionViewModel> ProductOptions { get; init; } = [];

    public AdminOrderCreateViewModel AddForm { get; init; } = new();

    public AdminOrderEditorViewModel EditForm { get; init; } = new();

    public string ActiveModal { get; init; } = string.Empty;
}

public class AdminStaffViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public IReadOnlyList<AdminStaffRecordViewModel> StaffMembers { get; init; } = [];
}

public class AdminProductsViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public IReadOnlyList<AdminProductShowcaseViewModel> Products { get; init; } = [];
}

public class AdminCodesViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminMetricCardViewModel> Metrics { get; init; } = [];

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public IReadOnlyList<AdminPromotionRecordViewModel> Promotions { get; init; } = [];

    public IReadOnlyList<AdminSelectOptionViewModel> PromotionOptions { get; init; } = [];

    public IReadOnlyList<AdminSelectOptionViewModel> DiscountTypeOptions { get; init; } = [];

    public IReadOnlyList<string> StatusOptions { get; init; } = [];

    public AdminPromoCodeEditorViewModel CreateForm { get; init; } = new();

    public string ActiveModal { get; init; } = string.Empty;
}

public class AdminPromotionRecordViewModel
{
    public int? PromoCodeId { get; init; }

    public bool IsPromoCode { get; init; }

    public long CreatedAtSort { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string DiscountLabel { get; init; } = string.Empty;

    public string RuleLabel { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public string UsageLabel { get; init; } = string.Empty;

    public string ExpiryLabel { get; init; } = string.Empty;

    public string Note { get; init; } = string.Empty;
}

public class AdminPromoCodeEditorViewModel
{
    public int? PromotionId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกโค้ด")]
    [StringLength(40, ErrorMessage = "โค้ดต้องไม่เกิน 40 ตัวอักษร")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อโค้ด")]
    [StringLength(120, ErrorMessage = "ชื่อโค้ดต้องไม่เกิน 120 ตัวอักษร")]
    public string Title { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "รายละเอียดต้องไม่เกิน 255 ตัวอักษร")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกประเภทส่วนลด")]
    public string DiscountType { get; set; } = "percent";

    [Range(0, 100000, ErrorMessage = "มูลค่าส่วนลดต้องไม่ติดลบ")]
    public decimal DiscountValue { get; set; } = 10;

    [Range(0, 1000000, ErrorMessage = "ยอดขั้นต่ำต้องไม่ติดลบ")]
    public decimal? MinOrderAmount { get; set; }

    [Range(0, 1000000, ErrorMessage = "ส่วนลดสูงสุดต้องไม่ติดลบ")]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(1, 100000, ErrorMessage = "จำนวนครั้งที่ใช้ได้ต้องมากกว่า 0")]
    public int? UsageLimit { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    [Required(ErrorMessage = "กรุณาเลือกสถานะ")]
    public string Status { get; set; } = "Active";

    [StringLength(255, ErrorMessage = "หมายเหตุต้องไม่เกิน 255 ตัวอักษร")]
    public string Note { get; set; } = string.Empty;
}

public class AdminProductShowcaseViewModel
{
    public int ProductId { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Tagline { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public decimal PriceAmount { get; init; }

    public string PriceLabel { get; init; } = string.Empty;

    public string StockLabel { get; init; } = string.Empty;

    public int StockQuantity { get; init; }

    public int ReorderLevel { get; init; }

    public string SalesLabel { get; init; } = string.Empty;

    public string RevenueLabel { get; init; } = string.Empty;

    public string VisibilityLabel { get; init; } = string.Empty;

    public string VisibilityKey { get; init; } = string.Empty;

    public string PublishedCopy { get; init; } = string.Empty;

    public bool IsPublished { get; init; }

    public int UnitsSold { get; init; }

    public decimal RevenueAmount { get; init; }
}

public class AdminProfileViewModel
{
    public string AccountCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string LastActiveLabel { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];
}

public class AdminDashboardTopProductViewModel
{
    public string Name { get; init; } = string.Empty;

    public string UnitsSoldLabel { get; init; } = string.Empty;

    public string RevenueLabel { get; init; } = string.Empty;
}

public class AdminStaffRecordViewModel
{
    public int StaffId { get; init; }

    public string StaffCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string RoleLabel { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public string LastActiveLabel { get; init; } = string.Empty;

    public string CreatedLabel { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}

public class AdminAccountsViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public IReadOnlyList<AdminAccountRecordViewModel> Accounts { get; init; } = [];

    public IReadOnlyList<string> AddRoleOptions { get; init; } = [];

    public IReadOnlyList<string> EditRoleOptions { get; init; } = [];

    public IReadOnlyList<string> StatusOptions { get; init; } = [];

    public bool CanCreateStaffAccounts { get; init; }

    public bool CanChangeRoles { get; init; }

    public AdminAccountEditorViewModel AddForm { get; init; } = new();

    public AdminAccountEditorViewModel EditForm { get; init; } = new();

    public string ActiveModal { get; init; } = string.Empty;
}

public class AdminItemsPageViewModel
{
    public string DateRangeLabel { get; init; } = string.Empty;

    public IReadOnlyList<AdminInfoItemViewModel> SummaryItems { get; init; } = [];

    public IReadOnlyList<AdminInventoryItemViewModel> Items { get; init; } = [];

    public IReadOnlyList<string> Categories { get; init; } = [];

    public IReadOnlyList<string> ImageOptions { get; init; } = [];

    public AdminCategoryEditorViewModel CategoryForm { get; init; } = new();

    public AdminItemEditorViewModel AddForm { get; init; } = new();

    public AdminItemEditorViewModel EditForm { get; init; } = new();

    public string ActiveModal { get; init; } = string.Empty;
}

public class AdminCategoryEditorViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อหมวดสินค้า")]
    [StringLength(40, ErrorMessage = "หมวดสินค้าต้องไม่เกิน 40 ตัวอักษร")]
    public string Name { get; set; } = string.Empty;

    public string TargetFieldId { get; set; } = string.Empty;

    public string ReturnModalId { get; set; } = string.Empty;
}

public class AdminInventoryItemViewModel
{
    public int ItemId { get; init; }

    public string ItemCode { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Tagline { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string ImagePath { get; init; } = string.Empty;

    public decimal PriceAmount { get; init; }

    public string PriceLabel { get; init; } = string.Empty;

    public int StockQuantity { get; init; }

    public int ReorderLevel { get; init; }

    public string StatusLabel { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public string UpdatedAtLabel { get; init; } = string.Empty;

    public long UpdatedAtSort { get; init; }

    public bool IsPublished { get; init; }
}

public class AdminItemEditorViewModel
{
    public int ItemId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อสินค้า")]
    [StringLength(80, ErrorMessage = "ชื่อสินค้าต้องไม่เกิน 80 ตัวอักษร")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกหมวดสินค้า")]
    [StringLength(40, ErrorMessage = "หมวดสินค้าต้องไม่เกิน 40 ตัวอักษร")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอก SKU")]
    [StringLength(24, MinimumLength = 3, ErrorMessage = "SKU ต้องมี 3-24 ตัวอักษร")]
    public string Sku { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "99999", ErrorMessage = "ราคาต้องมากกว่า 0")]
    public decimal Price { get; set; }

    [Range(0, 5000, ErrorMessage = "จำนวนสต็อกต้องอยู่ระหว่าง 0-5000")]
    public int StockQuantity { get; set; }

    [Range(0, 1000, ErrorMessage = "จุดเตือนสต็อกต้องอยู่ระหว่าง 0-1000")]
    public int ReorderLevel { get; set; }

    [StringLength(120, ErrorMessage = "คำอธิบายสั้นต้องไม่เกิน 120 ตัวอักษร")]
    public string Tagline { get; set; } = string.Empty;

    [StringLength(240, ErrorMessage = "หมายเหตุต้องไม่เกิน 240 ตัวอักษร")]
    public string Notes { get; set; } = string.Empty;

    [StringLength(160, ErrorMessage = "path รูปสินค้าต้องไม่เกิน 160 ตัวอักษร")]
    public string ImagePath { get; set; } = "/images/theme-cake.svg";

    public IFormFile? ImageFile { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class AdminAccountRecordViewModel
{
    public int AccountId { get; init; }

    public string AccountCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string StatusKey { get; init; } = string.Empty;

    public bool IsProtectedAdminAccount { get; init; }

    public string LastActive { get; init; } = string.Empty;

    public long LastActiveSort { get; init; }

    public string Notes { get; init; } = string.Empty;
}

public class AdminAccountEditorViewModel
{
    public int AccountId { get; set; }

    public string AccountCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกชื่อบัญชี")]
    [StringLength(80, ErrorMessage = "ชื่อบัญชีต้องไม่เกิน 80 ตัวอักษร")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือก role")]
    public string Role { get; set; } = "Staff";

    [Required(ErrorMessage = "กรุณาเลือกสถานะ")]
    public string Status { get; set; } = "Active";

    [StringLength(160, ErrorMessage = "หมายเหตุต้องไม่เกิน 160 ตัวอักษร")]
    public string Notes { get; set; } = string.Empty;

    [RegularExpression(@"^$|^.{8,100}$", ErrorMessage = "รหัสผ่านต้องมี 8-100 ตัวอักษร")]
    public string? Password { get; set; }

    public string LastActiveDisplay { get; set; } = string.Empty;
}

public class AdminInfoItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminMetricCardViewModel
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Delta { get; init; } = string.Empty;

    public bool PositiveTrend { get; init; }

    public string AccentKey { get; init; } = string.Empty;
}

public class AdminChartPointViewModel
{
    public string Label { get; init; } = string.Empty;

    public int Value { get; init; }

    public bool IsHighlighted { get; init; }
}

public class AdminOrderRecordViewModel
{
    public int OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string ProductSummary { get; init; } = string.Empty;

    public string ItemCountLabel { get; init; } = string.Empty;

    public string CreatedAtLabel { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string TotalAmountLabel { get; init; } = string.Empty;

    public string SubtotalLabel { get; init; } = string.Empty;

    public string DeliveryFeeLabel { get; init; } = string.Empty;

    public string DiscountAmountLabel { get; init; } = string.Empty;

    public string ShippingDiscountAmountLabel { get; init; } = string.Empty;

    public string DiscountCode { get; init; } = string.Empty;

    public string PointsEarnedLabel { get; init; } = string.Empty;

    public string PointsRedeemedLabel { get; init; } = string.Empty;

    public string PaymentMethodLabel { get; init; } = string.Empty;

    public string PaymentStatus { get; init; } = string.Empty;

    public string PaymentStatusKey { get; init; } = string.Empty;

    public string OrderStatus { get; init; } = string.Empty;

    public string OrderStatusKey { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string Note { get; init; } = string.Empty;

    public IReadOnlyList<AdminOrderItemRecordViewModel> Items { get; init; } = [];

    public IReadOnlyList<AdminOrderBenefitRecordViewModel> Benefits { get; init; } = [];
}

public class AdminOrderItemRecordViewModel
{
    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public string UnitPriceLabel { get; init; } = string.Empty;

    public string LineTotalLabel { get; init; } = string.Empty;
}

public class AdminOrderBenefitRecordViewModel
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ValueLabel { get; init; } = string.Empty;
}

public class AdminOrderEditorViewModel
{
    public int OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CreatedAtLabel { get; set; } = string.Empty;

    public string ItemSummary { get; set; } = string.Empty;

    public string TotalAmountLabel { get; set; } = string.Empty;

    public string PaymentMethodLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกสถานะออเดอร์")]
    public string OrderStatus { get; set; } = "paid";

    [Required(ErrorMessage = "กรุณาเลือกสถานะการชำระเงิน")]
    public string PaymentStatus { get; set; } = "paid";

    public string Phone { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "หมายเหตุต้องไม่เกิน 200 ตัวอักษร")]
    public string Note { get; set; } = string.Empty;
}

public class AdminOrderCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกลูกค้า")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกที่อยู่จัดส่ง")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกวิธีชำระเงิน")]
    public string PaymentMethod { get; set; } = "card";

    [Required(ErrorMessage = "กรุณาเลือกสถานะการชำระเงิน")]
    public string PaymentStatus { get; set; } = "paid";

    [Required(ErrorMessage = "กรุณาเลือกสถานะออเดอร์")]
    public string OrderStatus { get; set; } = "paid";

    [Range(typeof(decimal), "0", "9999", ErrorMessage = "ค่าส่งต้องอยู่ระหว่าง 0-9999")]
    public decimal DeliveryFee { get; set; }

    [StringLength(200, ErrorMessage = "หมายเหตุต้องไม่เกิน 200 ตัวอักษร")]
    public string Note { get; set; } = string.Empty;

    public List<AdminOrderLineEditorViewModel> Items { get; set; } =
    [
        new()
    ];
}

public class AdminOrderLineEditorViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกสินค้า")]
    public int ProductId { get; set; }

    [Range(1, 500, ErrorMessage = "จำนวนสินค้าต้องอย่างน้อย 1")]
    public int Quantity { get; set; } = 1;
}

public class AdminSelectOptionViewModel
{
    public string Value { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string SecondaryLabel { get; init; } = string.Empty;

    public string DataValue { get; init; } = string.Empty;

    public string DataExtra { get; init; } = string.Empty;
}
