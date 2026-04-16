using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public partial class HomeController
{
    private string GenerateStorefrontOrderNumber()
    {
        string orderNumber;
        do
        {
            var timestamp = GetStoreNow();
            orderNumber = $"OVK-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{Random.Shared.Next(100, 999)}";
        }
        while (_dbContext.Orders.AsNoTracking().Any(order => order.OrderNo == orderNumber));

        return orderNumber;
    }

    private static DateTimeOffset ConvertToStoreTime(DateTime utcDateTime)
    {
        var utcOffset = new DateTimeOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));

        try
        {
            return TimeZoneInfo.ConvertTime(utcOffset, TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utcOffset, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return utcOffset.ToLocalTime();
            }
        }
    }

    private bool TryValidateCartInventory(IReadOnlyList<CartLineRecord> cartItems, out string message)
    {
        message = string.Empty;

        var productIds = cartItems
            .Select(item => int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                ? productIdValue
                : 0)
            .Where(productIdValue => productIdValue > 0)
            .Distinct()
            .ToArray();

        var inventory = _dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.StockQty,
                product.IsActive
            })
            .ToDictionary(product => product.Id);

        foreach (var item in cartItems)
        {
            if (!int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                || !inventory.TryGetValue(productIdValue, out var product))
            {
                message = "มีสินค้าบางรายการไม่พร้อมจำหน่ายแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง";
                return false;
            }

            if (!product.IsActive || product.StockQty <= 0)
            {
                message = $"{product.Name} หมดแล้ว กรุณานำออกจากตะกร้าก่อนสั่งซื้อ";
                return false;
            }

            if (item.Quantity > product.StockQty)
            {
                message = $"{product.Name} มีคงเหลือเพียง {product.StockQty} ชิ้น กรุณาปรับจำนวนก่อนสั่งซื้อ";
                return false;
            }
        }

        return true;
    }

    private void ClearCheckoutValidationForPromoPreview()
    {
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.CustomerName));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PhoneNumber));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.DeliveryAddress));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PaymentMethod));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.UsePointsReward));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PointsToRedeem));
    }

    private void RemoveCheckoutFieldErrors(string fieldName)
    {
        foreach (var key in ModelState.Keys.ToList())
        {
            if (string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase)
                || key.EndsWith($".{fieldName}", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.Remove(key);
            }
        }
    }

    private void AddCheckoutFieldError(string fieldName, string errorMessage)
    {
        var key = $"Checkout.{fieldName}";
        ModelState.Remove(key);
        ModelState.AddModelError(key, errorMessage);
    }

    private string ReadPersistedPromoCode()
    {
        return HttpContext.Session.GetString(PromoCodeSessionKey)?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private void PersistPromoCode(string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
        {
            HttpContext.Session.Remove(PromoCodeSessionKey);
            return;
        }

        HttpContext.Session.SetString(PromoCodeSessionKey, promoCode.Trim().ToUpperInvariant());
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
            ImagePath = NormalizeProductImagePath(product.ImageUrl, themeKey),
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

    private static string NormalizeProductImagePath(string? imagePath, string themeKey)
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

    private IReadOnlyList<PaymentOptionViewModel> BuildPaymentOptions()
    {
        return _storefrontContent.PaymentOptions
            .Select(option => new PaymentOptionViewModel
            {
                Code = option.Code,
                Label = option.Label,
                Description = option.Description
            })
            .ToList();
    }

    private string ResolvePaymentLabel(string paymentMethod)
    {
        return BuildPaymentOptions()
            .FirstOrDefault(option => option.Code == paymentMethod)?.Label
            ?? "วิธีที่เลือก";
    }

    private static decimal CalculateDeliveryFee(IReadOnlyList<CartLineRecord> cartItems)
    {
        return cartItems.Count == 0 ? 0 : DeliveryFeeAmount;
    }

    private IReadOnlyList<OrderProgressStepViewModel> BuildOrderProgressSteps(string currentStatusCode)
    {
        var normalizedStatus = NormalizeOrderStatusKey(currentStatusCode);
        if (normalizedStatus is "refunded" or "cancelled")
        {
            var specialTitle = normalizedStatus == "refunded" ? "คืนเงินแล้ว" : "ยกเลิกออเดอร์";
            var specialDescription = normalizedStatus == "refunded"
                ? "ร้านดำเนินการคืนเงินสำหรับคำสั่งซื้อนี้แล้ว"
                : "คำสั่งซื้อนี้ถูกยกเลิกและจะไม่เข้าสู่ขั้นตอนจัดส่ง";
            var specialMarker = normalizedStatus == "refunded" ? "RF" : "CN";

            return
            [
                new OrderProgressStepViewModel
                {
                    Title = "รับคำสั่งซื้อ",
                    Description = "คำสั่งซื้อถูกบันทึกในระบบเรียบร้อยแล้ว",
                    Marker = "01",
                    State = "complete"
                },
                new OrderProgressStepViewModel
                {
                    Title = specialTitle,
                    Description = specialDescription,
                    Marker = specialMarker,
                    State = normalizedStatus
                }
            ];
        }

        var steps = _storefrontContent.OrderStatusSteps
            .Select(step => (step.Code, step.Title, step.Description, step.Marker))
            .ToArray();

        var currentIndex = Array.FindIndex(steps, step => string.Equals(step.Item1, currentStatusCode, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        return steps.Select((step, index) => new OrderProgressStepViewModel
        {
            Title = step.Item2,
            Description = step.Item3,
            Marker = step.Item4,
            State = index < currentIndex
                ? "complete"
                : index == currentIndex
                    ? "current"
                    : "pending"
        }).ToList();
    }

    private (string Title, string Description) ResolveOrderStatus(string currentStatusCode)
    {
        var normalizedStatus = NormalizeOrderStatusKey(currentStatusCode);
        if (normalizedStatus == "refunded")
        {
            return ("คืนเงินแล้ว", "ร้านดำเนินการคืนเงินให้คำสั่งซื้อนี้แล้ว หากมีคำถามเพิ่มเติมสามารถติดต่อร้านได้");
        }

        if (normalizedStatus == "cancelled")
        {
            return ("ยกเลิกออเดอร์", "คำสั่งซื้อนี้ถูกยกเลิกแล้ว และจะไม่เข้าสู่ขั้นตอนจัดส่ง");
        }

        var status = _storefrontContent.OrderStatusSteps
            .FirstOrDefault(step => string.Equals(step.Code, currentStatusCode, StringComparison.OrdinalIgnoreCase))
            ?? _storefrontContent.OrderStatusSteps.FirstOrDefault();

        return status is null
            ? ("สถานะคำสั่งซื้อ", "ระบบกำลังอัปเดตสถานะล่าสุดของออเดอร์นี้")
            : (status.Title, status.CurrentDescription);
    }

    private static string NormalizeOrderStatusKey(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized switch
        {
            "paid" or "pending" => "paid",
            "shipping" or "shipped" => "shipping",
            "delivered" or "complete" or "completed" => "delivered",
            "refunded" => "refunded",
            "cancelled" or "canceled" => "cancelled",
            _ => "paid"
        };
    }

    private static IReadOnlyList<CategoryCardViewModel> BuildCategoryCards(
        IReadOnlyList<ProductCardViewModel> products,
        IReadOnlyDictionary<string, ProductSalesSummary> salesLookup)
    {
        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Category))
            .GroupBy(product => product.Category, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var rankedProducts = group
                    .Select(product =>
                    {
                        var sales = salesLookup.TryGetValue(product.ProductId, out var summary)
                            ? summary
                            : new ProductSalesSummary(0, 0);

                        return new
                        {
                            Product = product,
                            Sales = sales
                        };
                    })
                    .OrderByDescending(item => item.Sales.UnitsSold)
                    .ThenByDescending(item => item.Sales.Revenue)
                    .ThenBy(item => item.Product.IsSoldOut)
                    .ThenBy(item => item.Product.Name)
                    .ToList();
                var featured = rankedProducts.First();
                var itemCount = group.Count();

                return new CategoryCardViewModel
                {
                    Title = group.First().Category,
                    Subtitle = $"{itemCount} เมนูในหมวดนี้",
                    ThemeKey = featured.Product.ThemeKey,
                    ImagePath = featured.Product.ImagePath,
                    ItemCount = itemCount,
                    FeaturedProductName = featured.Product.Name,
                    FeaturedProductDescription = featured.Product.Description,
                    FeaturedProductPriceLabel = $"{featured.Product.Price:0.##} ฿",
                    FeaturedProductSalesLabel = featured.Sales.UnitsSold > 0
                        ? $"ขายแล้ว {featured.Sales.UnitsSold:N0} ชิ้น"
                        : "เมนูเด่นของหมวดนี้",
                    FeaturedProductBadge = featured.Sales.UnitsSold > 0 ? "ขายดี" : "แนะนำ"
                };
            })
            .ToList();
    }

    private static IReadOnlyList<ProductCardViewModel> BuildBestSellingProducts(
        IReadOnlyList<ProductCardViewModel> products,
        IReadOnlyDictionary<string, ProductSalesSummary> salesLookup,
        int take)
    {
        return products
            .Select(product =>
            {
                var sales = salesLookup.TryGetValue(product.ProductId, out var summary)
                    ? summary
                    : new ProductSalesSummary(0, 0);

                return new
                {
                    Product = product,
                    Sales = sales
                };
            })
            .OrderByDescending(item => item.Sales.UnitsSold)
            .ThenByDescending(item => item.Sales.Revenue)
            .ThenBy(item => item.Product.IsSoldOut)
            .ThenBy(item => item.Product.Name)
            .Take(take)
            .Select(item => item.Product)
            .ToList();
    }

    private sealed record ProductSalesSummary(int UnitsSold, decimal Revenue);
}
