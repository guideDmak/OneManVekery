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
    private ContactPageViewModel BuildContactPageModel(ContactFormViewModel? form = null)
    {
        var contactContent = _storefrontContent.Contact ?? new StorefrontContactOptions();
        var contactForm = ApplySignedInContactDefaults(form ?? new ContactFormViewModel());

        return new ContactPageViewModel
        {
            Form = contactForm,
            HeadingTitle = contactContent.HeadingTitle,
            ContactCards = contactContent.Cards
                .Select(card => new ContactInfoCardViewModel
                {
                    IconText = card.IconText,
                    Title = card.Title,
                    LineOne = card.LineOne,
                    LineTwo = card.LineTwo
                })
                .ToList()
        };
    }

    private ContactFormViewModel ApplySignedInContactDefaults(ContactFormViewModel form)
    {
        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);
        if (!int.TryParse(accountId, out var accountIdValue) || accountIdValue <= 0)
        {
            return form;
        }

        var account = _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == accountIdValue)
            .Select(user => new
            {
                user.FullName,
                user.Email,
                user.Phone
            })
            .FirstOrDefault();

        if (account is null)
        {
            return form;
        }

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            form.Name = account.FullName ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(form.Email))
        {
            form.Email = account.Email ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(form.PhoneNumber))
        {
            form.PhoneNumber = account.Phone ?? string.Empty;
        }

        return form;
    }

    private CartPageViewModel BuildCartPageModel(CartCheckoutViewModel? checkout = null)
    {
        var cartItems = GetCartItems();
        var checkoutModel = ApplySignedInCheckoutDefaults(checkout ?? new CartCheckoutViewModel
        {
            PaymentMethod = "promptpay"
        }, includePersistedPromoCode: checkout is null);
        var pricing = BuildPricingSummary(cartItems, checkoutModel);

        return new CartPageViewModel
        {
            Items = cartItems.Select(item => new CartLineViewModel
            {
                ProductId = item.ProductId,
                Name = item.Name,
                Category = item.Category,
                Description = item.Description,
                ImagePath = item.ImagePath,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                IsSoldOut = item.IsSoldOut,
                UnitPriceLabel = $"{item.UnitPrice:0.##} ฿",
                LineTotalLabel = $"{item.LineTotal:0.##} ฿"
            }).ToList(),
            PaymentOptions = BuildPaymentOptions(),
            AppliedBenefits = pricing.AppliedBenefits
                .Select(MapBenefit)
                .ToList(),
            Checkout = checkoutModel,
            ItemCount = cartItems.Sum(item => item.Quantity),
            Subtotal = pricing.Subtotal,
            DeliveryFee = pricing.DeliveryFee,
            DiscountAmount = pricing.DiscountAmount,
            ShippingDiscountAmount = pricing.ShippingDiscountAmount,
            AppliedPromoCode = pricing.Promo.IsApplied ? pricing.Promo.Code : string.Empty,
            AppliedPromoTitle = pricing.Promo.IsApplied ? pricing.Promo.Title : string.Empty,
            AppliedPromoDescription = pricing.Promo.IsApplied ? pricing.Promo.Description : string.Empty,
            PromoMessage = pricing.Promo.Message,
            PromoMessageState = pricing.Promo.MessageState,
            CurrentPoints = pricing.CurrentPoints,
            PointsEarned = pricing.PointsEarned,
            PointsRedeemed = pricing.PointsRedeemed,
            PointsDiscountAmount = pricing.PointsDiscountAmount,
            MaxPointDiscountRedeem = pricing.MaxPointDiscountRedeem,
            PointDiscountRateLabel = $"{PointDiscountPointStep} P = {PointDiscountValuePerStep:0.##} ฿",
            ProjectedPointsBalance = pricing.ProjectedPointsBalance,
            PointsNeededForFreeItem = Math.Max(0, pricing.RewardPointCost - pricing.CurrentPoints),
            RewardPointCost = pricing.RewardPointCost,
            RewardQty = pricing.RewardQty,
            RewardProductName = pricing.RewardProductName,
            CanRedeemFreeItem = pricing.CanRedeemFreeItem
        };
    }

    private OrderStatusPageViewModel BuildOrderStatusPageModel(Order order)
    {
        var currentStatus = ResolveOrderStatus(order.OrderStatus);
        var latestPointsLedger = order.LoyaltyPointsLedgers
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .LastOrDefault();
        var promoBenefit = order.OrderPromotions
            .FirstOrDefault(item => item.PromoCodeId.HasValue)
            ?? order.OrderPromotions.FirstOrDefault(item => item.PromotionId == order.PromoCode?.PromotionId);

        return new OrderStatusPageViewModel
        {
            OrderNumber = order.OrderNo,
            CreatedAt = ConvertToStoreTime(order.CreatedAt),
            CustomerName = order.CustomerName,
            PhoneNumber = order.Phone,
            DeliveryAddress = order.Address,
            PaymentMethodLabel = ResolvePaymentLabel(order.PaymentMethod),
            Notes = order.Note ?? string.Empty,
            CurrentStatusLabel = currentStatus.Title,
            CurrentStatusDescription = currentStatus.Description,
            Items = order.OrderItems.Select(item => new OrderReceiptLineViewModel
            {
                Name = item.ProductName,
                Category = item.Product?.Category?.Name ?? string.Empty,
                Quantity = item.Qty,
                UnitPrice = item.Price,
                LineTotal = item.LineTotal
            }).ToList(),
            StatusSteps = BuildOrderProgressSteps(order.OrderStatus),
            AppliedBenefits = order.OrderPromotions
                .OrderBy(item => item.Id)
                .Select(MapBenefit)
                .ToList(),
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            DiscountAmount = order.DiscountAmount,
            ShippingDiscountAmount = order.ShippingDiscountAmount,
            AppliedPromoCode = order.DiscountCode ?? string.Empty,
            AppliedPromoTitle = promoBenefit?.PromotionTitle ?? order.PromoCode?.Title ?? string.Empty,
            PointsEarned = order.PointsEarned,
            PointsRedeemed = order.PointsRedeemed,
            PointsBalanceAfter = latestPointsLedger?.BalanceAfter ?? GetCurrentPointsBalance(order.UserId)
        };
    }

    private MyOrdersPageViewModel BuildMyOrdersPageModel()
    {
        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return new MyOrdersPageViewModel();
        }

        var orders = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Where(order => order.UserId == accountId.Value)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        return new MyOrdersPageViewModel
        {
            Orders = orders.Select(MapMyOrderCard).ToList(),
            OrderCount = orders.Count,
            TotalSpendLabel = $"{orders.Sum(order => order.TotalAmount):0.##} ฿",
            LastOrderLabel = orders.Count == 0
                ? "ยังไม่มีคำสั่งซื้อ"
                : ConvertToStoreTime(orders[0].CreatedAt).ToString("dd MMM yyyy, HH:mm")
        };
    }

    private MyOrderCardViewModel MapMyOrderCard(Order order)
    {
        var status = ResolveOrderStatus(order.OrderStatus);
        var firstItem = order.OrderItems
            .OrderBy(item => item.Id)
            .FirstOrDefault();
        var totalQuantity = order.OrderItems.Sum(item => item.Qty);
        var extraItems = Math.Max(0, order.OrderItems.Count - 1);
        var productSummary = firstItem is null
            ? "ไม่มีรายการสินค้า"
            : extraItems > 0
                ? $"{firstItem.ProductName} และอีก {extraItems} รายการ"
                : firstItem.ProductName;

        return new MyOrderCardViewModel
        {
            OrderNumber = order.OrderNo,
            CreatedAt = ConvertToStoreTime(order.CreatedAt),
            ProductSummary = productSummary,
            ItemCountLabel = totalQuantity <= 0 ? "0 ชิ้น" : $"{totalQuantity} ชิ้น",
            TotalAmountLabel = $"{order.TotalAmount:0.##} ฿",
            PaymentMethodLabel = ResolvePaymentLabel(order.PaymentMethod),
            StatusLabel = status.Title,
            StatusDescription = status.Description,
            StatusKey = NormalizeOrderStatusKey(order.OrderStatus),
            PromoLabel = string.IsNullOrWhiteSpace(order.DiscountCode)
                ? string.Empty
                : order.DiscountCode,
            PointsEarned = order.PointsEarned,
            PointsRedeemed = order.PointsRedeemed
        };
    }
}
