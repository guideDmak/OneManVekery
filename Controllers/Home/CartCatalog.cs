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
    private IReadOnlyList<ProductCardViewModel> GetProducts()
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

    private IReadOnlyList<ProductCardViewModel> GetNewArrivalProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.CreatedAt)
            .ThenByDescending(product => product.Id)
            .Take(3)
            .ToList()
            .Select(MapProduct)
            .ToList();
    }

    private IReadOnlyDictionary<string, ProductSalesSummary> BuildProductSalesLookup()
    {
        return _dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.ProductId.HasValue)
            .Select(item => new
            {
                ProductId = item.ProductId!.Value,
                item.Qty,
                item.LineTotal,
                item.Order.OrderStatus
            })
            .ToList()
            .Where(item => NormalizeOrderStatusKey(item.OrderStatus) is not ("refunded" or "cancelled"))
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key.ToString(CultureInfo.InvariantCulture),
                group => new ProductSalesSummary(
                    group.Sum(item => item.Qty),
                    group.Sum(item => item.LineTotal)),
                StringComparer.OrdinalIgnoreCase);
    }

    private ProductCardViewModel? GetProductById(string productId)
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

    private IReadOnlyList<CartLineRecord> GetCartItems()
    {
        var catalog = GetProducts().ToDictionary(product => product.ProductId, StringComparer.OrdinalIgnoreCase);

        return ReadCartItems()
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

    private bool AddItemToCart(string productId, int quantity = 1)
    {
        var product = GetActiveProductEntity(productId);
        if (product is null || product.StockQty <= 0 || quantity <= 0)
        {
            return false;
        }

        var items = ReadCartItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        var currentQuantity = index >= 0 ? items[index].Quantity : 0;
        var nextQuantity = Math.Min(currentQuantity + quantity, product.StockQty);

        if (nextQuantity <= currentQuantity)
        {
            return false;
        }

        if (index >= 0)
        {
            items[index] = items[index] with { Quantity = Math.Min(nextQuantity, 99) };
        }
        else
        {
            items.Add(new CartSessionItem
            {
                ProductId = productId,
                Quantity = Math.Min(nextQuantity, 99)
            });
        }

        WriteCartItems(items);
        return true;
    }

    private bool ChangeCartItemQuantity(string productId, int delta)
    {
        var currentItem = ReadCartItems().FirstOrDefault(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (currentItem is null)
        {
            return false;
        }

        return UpdateCartItemQuantity(productId, currentItem.Quantity + delta);
    }

    private bool RemoveCartItem(string productId)
    {
        var items = ReadCartItems();
        var removedCount = items.RemoveAll(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (removedCount == 0)
        {
            return false;
        }

        WriteCartItems(items);
        return true;
    }

    private void ClearCart()
    {
        HttpContext.Session.Remove(CartSessionKey);
        HttpContext.Session.Remove(PromoCodeSessionKey);
    }

    private bool TryCreateOrder(
        int? userId,
        CartCheckoutSnapshot checkout,
        IReadOnlyList<CartLineRecord> items,
        PricingSummary pricing,
        PromoResolution promo,
        out Order? order,
        out string errorMessage)
    {
        order = null;
        errorMessage = string.Empty;

        var productIds = items
            .Select(item => int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                ? productIdValue
                : 0)
            .Where(productIdValue => productIdValue > 0)
            .Distinct()
            .ToArray();

        using var transaction = _dbContext.Database.BeginTransaction();

        var products = _dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id);

        foreach (var item in items)
        {
            if (!int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                || !products.TryGetValue(productIdValue, out var product))
            {
                errorMessage = "มีสินค้าบางรายการไม่พร้อมจำหน่ายแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง";
                return false;
            }

            if (!product.IsActive || product.StockQty <= 0)
            {
                errorMessage = $"{product.Name} หมดแล้ว กรุณานำออกจากตะกร้าก่อนสั่งซื้อ";
                return false;
            }

            if (item.Quantity > product.StockQty)
            {
                errorMessage = $"{product.Name} มีคงเหลือเพียง {product.StockQty} ชิ้น กรุณาปรับจำนวนก่อนสั่งซื้อ";
                return false;
            }
        }

        PromoCode? promoCode = null;
        if (promo.IsApplied && promo.PromoCodeId is int promoCodeIdValue)
        {
            promoCode = _dbContext.PromoCodes
                .Include(item => item.Promotion)
                .FirstOrDefault(item => item.Id == promoCodeIdValue);

            if (promoCode is null)
            {
                errorMessage = "ไม่พบโค้ดส่วนลดที่เลือกแล้ว กรุณาลองใหม่อีกครั้ง";
                return false;
            }

            if (!IsRecordActive(promoCode.Status) || !IsWithinUsageWindow(promoCode.StartsAt, promoCode.ExpiresAt))
            {
                errorMessage = "โค้ดส่วนลดนี้หมดอายุหรือยังไม่พร้อมใช้งานแล้ว";
                return false;
            }

            if (promoCode.UsageLimit is int usageLimit && usageLimit > 0 && promoCode.UsedCount >= usageLimit)
            {
                errorMessage = "โค้ดส่วนลดนี้ถูกใช้ครบสิทธิ์แล้ว";
                return false;
            }
        }

        var createdAt = DateTime.UtcNow;
        order = new Order
        {
            OrderNo = GenerateStorefrontOrderNumber(),
            UserId = userId,
            CustomerName = checkout.CustomerName.Trim(),
            Phone = checkout.PhoneNumber.Trim(),
            Address = checkout.DeliveryAddress.Trim(),
            PaymentMethod = checkout.PaymentMethodCode,
            PaymentStatus = "paid",
            OrderStatus = "paid",
            Subtotal = pricing.Subtotal,
            DeliveryFee = pricing.DeliveryFee,
            TotalAmount = Math.Max(0, pricing.Subtotal + pricing.DeliveryFee - pricing.DiscountAmount - pricing.ShippingDiscountAmount),
            Note = string.IsNullOrWhiteSpace(checkout.Notes) ? null : checkout.Notes.Trim(),
            CreatedAt = createdAt,
            PromoCodeId = promo.PromoCodeId,
            DiscountCode = promo.IsApplied ? promo.Code : null,
            DiscountAmount = pricing.DiscountAmount,
            ShippingDiscountAmount = pricing.ShippingDiscountAmount,
            PointsEarned = pricing.PointsEarned,
            PointsRedeemed = pricing.PointsRedeemed
        };

        foreach (var item in items)
        {
            var productIdValue = int.Parse(item.ProductId, CultureInfo.InvariantCulture);
            var product = products[productIdValue];
            product.StockQty -= item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Qty = item.Quantity,
                LineTotal = product.Price * item.Quantity
            });
        }

        foreach (var benefit in pricing.AppliedBenefits)
        {
            order.OrderPromotions.Add(new OrderPromotion
            {
                PromotionId = benefit.PromotionId,
                PromoCodeId = benefit.PromoCodeId,
                PromotionTitle = benefit.Title,
                BenefitType = benefit.BenefitType,
                DiscountAmount = benefit.DiscountAmount,
                ShippingDiscountAmount = benefit.ShippingDiscountAmount,
                PointsEarned = benefit.PointsEarned,
                PointsRedeemed = benefit.PointsRedeemed,
                RewardProductId = benefit.RewardProductId,
                RewardProductName = string.IsNullOrWhiteSpace(benefit.RewardProductName) ? null : benefit.RewardProductName,
                RewardQty = benefit.RewardQty,
                Note = string.IsNullOrWhiteSpace(benefit.Description) ? null : benefit.Description,
                CreatedAt = createdAt
            });
        }

        if (promoCode is not null)
        {
            promoCode.UsedCount += 1;
        }

        _dbContext.Orders.Add(order);

        if (userId is int userIdValue)
        {
            ApplyCheckoutLoyalty(userIdValue, order, pricing, createdAt);
        }

        _dbContext.SaveChanges();
        transaction.Commit();
        return true;
    }

    private Order? GetOrderForCurrentUser(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return null;
        }

        var normalizedOrderNumber = orderNumber.Trim();

        return _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.PromoCode)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product!.Category)
            .Include(order => order.OrderPromotions)
            .Include(order => order.LoyaltyPointsLedgers)
            .FirstOrDefault(order => order.OrderNo == normalizedOrderNumber && order.UserId == accountId.Value);
    }

    private bool IsStorefrontUserSignedIn()
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);

        return int.TryParse(accountId, out var accountIdValue)
               && accountIdValue > 0
               && string.Equals(roleKey, "user", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetSignedInStorefrontUserId()
    {
        if (!IsStorefrontUserSignedIn())
        {
            return null;
        }

        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);
        return int.TryParse(accountId, out var accountIdValue) && accountIdValue > 0
            ? accountIdValue
            : null;
    }

    private List<CartSessionItem> ReadCartItems()
    {
        var raw = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartSessionItem>>(raw) ?? [];
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(CartSessionKey);
            return [];
        }
    }

    private void WriteCartItems(List<CartSessionItem> items)
    {
        if (items.Count == 0)
        {
            HttpContext.Session.Remove(CartSessionKey);
            return;
        }

        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(items));
    }

    private bool UpdateCartItemQuantity(string productId, int quantity)
    {
        var items = ReadCartItems();
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
            var product = GetActiveProductEntity(productId);
            if (product is null || product.StockQty <= 0)
            {
                items.RemoveAt(index);
            }
            else
            {
                items[index] = items[index] with { Quantity = Math.Min(Math.Min(quantity, product.StockQty), 99) };
            }
        }

        WriteCartItems(items);
        return true;
    }

    private Product? GetActiveProductEntity(string productId)
    {
        if (!int.TryParse(productId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue))
        {
            return null;
        }

        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(item => item.Id == productIdValue && item.IsActive);
    }

    private CartCheckoutViewModel ApplySignedInCheckoutDefaults(CartCheckoutViewModel checkout, bool includePersistedPromoCode = false)
    {
        checkout.PaymentMethod = string.IsNullOrWhiteSpace(checkout.PaymentMethod)
            ? "promptpay"
            : checkout.PaymentMethod;

        if (includePersistedPromoCode && string.IsNullOrWhiteSpace(checkout.PromoCode))
        {
            checkout.PromoCode = ReadPersistedPromoCode();
        }

        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return checkout;
        }

        var account = _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == accountId.Value)
            .Select(user => new
            {
                user.FullName,
                user.Phone
            })
            .FirstOrDefault();

        var address = _dbContext.UserAddresses
            .AsNoTracking()
            .Where(item => item.UserId == accountId.Value)
            .OrderByDescending(item => item.IsDefault)
            .ThenByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.RecipientName,
                item.Phone,
                item.AddressLine,
                item.PostalCode
            })
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(checkout.CustomerName))
        {
            checkout.CustomerName = address?.RecipientName
                ?? account?.FullName
                ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(checkout.PhoneNumber))
        {
            checkout.PhoneNumber = address?.Phone
                ?? account?.Phone
                ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(checkout.DeliveryAddress) && address is not null)
        {
            checkout.DeliveryAddress = FormatDeliveryAddress(address.AddressLine, address.PostalCode);
        }

        return checkout;
    }

    private static string FormatDeliveryAddress(string addressLine, string? postalCode)
    {
        return string.IsNullOrWhiteSpace(postalCode)
            ? addressLine
            : $"{addressLine} {postalCode}".Trim();
    }
}
