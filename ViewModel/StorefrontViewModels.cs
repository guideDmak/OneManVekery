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

    public IReadOnlyList<ServiceFeatureViewModel> Features { get; init; } = [];
}

public class AboutPageViewModel
{
    public IReadOnlyList<AboutStatViewModel> Stats { get; init; } = [];

    public IReadOnlyList<ServiceFeatureViewModel> Values { get; init; } = [];

    public IReadOnlyList<ProcessStepViewModel> Steps { get; init; } = [];
}

public class ContactPageViewModel
{
    public ContactFormViewModel Form { get; init; } = new();

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
