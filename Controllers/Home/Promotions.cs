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
    private int GetCurrentPointsBalance(int? userId = null)
    {
        var targetUserId = userId ?? GetSignedInStorefrontUserId();
        if (targetUserId is null)
        {
            return 0;
        }

        return _dbContext.LoyaltyWallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == targetUserId.Value)
            .Select(wallet => wallet.CurrentPoints)
            .FirstOrDefault();
    }

    private void ApplyCheckoutLoyalty(int userId, Order order, PricingSummary pricing, DateTime createdAt)
    {
        if (pricing.PointsEarned <= 0 && pricing.PointsRedeemed <= 0)
        {
            return;
        }

        var wallet = _dbContext.LoyaltyWallets
            .FirstOrDefault(item => item.UserId == userId);

        if (wallet is null)
        {
            wallet = new LoyaltyWallet
            {
                UserId = userId,
                CurrentPoints = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.LoyaltyWallets.Add(wallet);
        }
        if (pricing.PointsRedeemed > 0)
        {
            wallet.CurrentPoints = Math.Max(0, wallet.CurrentPoints - pricing.PointsRedeemed);
            wallet.LifetimeRedeemed += pricing.PointsRedeemed;

            _dbContext.LoyaltyPointsLedgers.Add(new LoyaltyPointsLedger
            {
                UserId = userId,
                Order = order,
                PromotionId = pricing.LoyaltyPromotionId,
                EntryType = "redeem",
                PointsDelta = -pricing.PointsRedeemed,
                BalanceAfter = wallet.CurrentPoints,
                Note = $"ใช้พอยต์กับออเดอร์ {order.OrderNo}",
                CreatedAt = createdAt
            });
        }

        if (pricing.PointsEarned > 0)
        {
            wallet.CurrentPoints += pricing.PointsEarned;
            wallet.LifetimeEarned += pricing.PointsEarned;

            _dbContext.LoyaltyPointsLedgers.Add(new LoyaltyPointsLedger
            {
                UserId = userId,
                Order = order,
                PromotionId = pricing.LoyaltyPromotionId,
                EntryType = "earn",
                PointsDelta = pricing.PointsEarned,
                BalanceAfter = wallet.CurrentPoints,
                Note = $"รับพอยต์จากออเดอร์ {order.OrderNo}",
                CreatedAt = createdAt
            });
        }

        wallet.UpdatedAt = createdAt;
    }

    private IReadOnlyList<Promotion> GetActiveAutomaticPromotions(DateTimeOffset storeNow)
    {
        return _dbContext.Promotions
            .AsNoTracking()
            .Include(promotion => promotion.RewardProduct)
            .Where(promotion => promotion.AutoApply && !promotion.RequiresCode)
            .ToList()
            .Where(promotion => IsRecordActive(promotion.Status) && IsPromotionActiveNow(promotion, storeNow))
            .OrderBy(promotion => promotion.Priority)
            .ThenBy(promotion => promotion.Id)
            .ToList();
    }

    private static bool IsPromotionActiveNow(Promotion promotion, DateTimeOffset storeNow)
    {
        if (!IsWithinUsageWindow(promotion.StartsAt, promotion.ExpiresAt))
        {
            return false;
        }

        if (promotion.WeekdayMask is int weekdayMask && weekdayMask > 0)
        {
            var currentDayMask = GetWeekdayMask(storeNow.DayOfWeek);
            if ((weekdayMask & currentDayMask) == 0)
            {
                return false;
            }
        }

        if (promotion.DailyStartTime.HasValue || promotion.DailyEndTime.HasValue)
        {
            var nowTime = TimeOnly.FromDateTime(storeNow.DateTime);
            var startTime = promotion.DailyStartTime ?? TimeOnly.MinValue;
            var endTime = promotion.DailyEndTime ?? TimeOnly.MaxValue;

            if (startTime <= endTime)
            {
                if (nowTime < startTime || nowTime >= endTime)
                {
                    return false;
                }
            }
            else if (nowTime < startTime && nowTime >= endTime)
            {
                return false;
            }
        }

        return true;
    }

    private static int GetWeekdayMask(DayOfWeek dayOfWeek)
    {
        return 1 << (int)dayOfWeek;
    }

    private static bool PromotionMeetsThresholds(Promotion promotion, decimal subtotal, int itemCount)
    {
        if (promotion.MinOrderAmount is decimal minOrderAmount && minOrderAmount > 0 && subtotal < minOrderAmount)
        {
            return false;
        }

        if (promotion.MinItemQty is int minItemQty && minItemQty > 0 && itemCount < minItemQty)
        {
            return false;
        }

        return true;
    }

    private static (int FreeItemCount, decimal DiscountAmount) CalculateBuyGetPromotionDiscount(
        IReadOnlyList<CartLineRecord> cartItems,
        Promotion promotion,
        decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0 || promotion.BuyQty is not > 0 || promotion.GetQty is not > 0)
        {
            return (0, 0);
        }

        var setSize = promotion.BuyQty.Value + promotion.GetQty.Value;
        var freeItemCount = 0;
        var discountAmount = 0m;

        foreach (var item in cartItems)
        {
            if (item.Quantity < setSize)
            {
                continue;
            }

            var eligibleFreeQty = (item.Quantity / setSize) * promotion.GetQty.Value;
            if (eligibleFreeQty <= 0)
            {
                continue;
            }

            freeItemCount += eligibleFreeQty;
            discountAmount += eligibleFreeQty * item.UnitPrice;
        }

        return (freeItemCount, Math.Min(discountAmount, remainingSubtotal));
    }

    private static decimal CalculatePromotionDiscountAmount(Promotion promotion, decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0)
        {
            return 0;
        }

        decimal discountAmount = 0;

        if (promotion.DiscountPercent is decimal discountPercent && discountPercent > 0)
        {
            discountAmount = Math.Round(remainingSubtotal * (discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else if (promotion.DiscountAmount is decimal fixedDiscountAmount && fixedDiscountAmount > 0)
        {
            discountAmount = fixedDiscountAmount;
        }

        if (promotion.MaxDiscountAmount is decimal maxDiscountAmount && maxDiscountAmount > 0)
        {
            discountAmount = Math.Min(discountAmount, maxDiscountAmount);
        }

        return Math.Min(discountAmount, remainingSubtotal);
    }

    private static decimal ResolveRewardPromotionDiscount(Promotion promotion, decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0 || promotion.RewardProduct is null)
        {
            return 0;
        }

        var rewardQty = promotion.RewardQty.GetValueOrDefault(1);
        if (rewardQty <= 0)
        {
            rewardQty = 1;
        }

        var rewardValue = promotion.RewardProduct.Price * rewardQty;
        return Math.Min(rewardValue, remainingSubtotal);
    }

    private static int CalculateEarnedPoints(Promotion? promotion, decimal remainingSubtotal)
    {
        if (promotion?.PointsAwarded is not > 0)
        {
            return 0;
        }

        if (promotion.SpendStepAmount is not decimal spendStepAmount || spendStepAmount <= 0 || remainingSubtotal < spendStepAmount)
        {
            return 0;
        }

        return (int)Math.Floor(remainingSubtotal / spendStepAmount) * promotion.PointsAwarded.Value;
    }

    private static string BuildPromotionDescription(Promotion promotion, DateTimeOffset storeNow, int itemCount)
    {
        if (promotion.MinItemQty is int minItemQty && minItemQty > 0)
        {
            return $"จำนวนสินค้าในตะกร้าครบ {itemCount} ชิ้นแล้ว";
        }

        if (promotion.FreeShipping && promotion.MinOrderAmount is decimal minOrderAmount && minOrderAmount > 0)
        {
            return $"ยอดสินค้าครบ {minOrderAmount:0.##} ฿ แล้ว";
        }

        if (promotion.DailyStartTime.HasValue || promotion.DailyEndTime.HasValue)
        {
            return $"สิทธิ์พิเศษตามช่วงเวลา {storeNow:HH:mm}";
        }

        return "สิทธิ์นี้ถูกใช้กับออเดอร์ปัจจุบันแล้ว";
    }

    private static string BuildLoyaltyEarningDescription(Promotion? promotion)
    {
        if (promotion?.SpendStepAmount is decimal spendStepAmount && promotion.PointsAwarded is int pointsAwarded)
        {
            return $"ทุกยอดซื้อครบ {spendStepAmount:0.##} ฿ รับ {pointsAwarded} พอยต์";
        }

        return "ได้รับคะแนนสะสมจากออเดอร์นี้";
    }

    private static DateTimeOffset GetStoreNow()
    {
        var utcNow = DateTimeOffset.UtcNow;

        try
        {
            return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return utcNow.ToLocalTime();
            }
        }
    }

    private PromoResolution ResolvePromoCode(
        string? rawCode,
        IReadOnlyList<CartLineRecord> cartItems,
        decimal eligibilitySubtotal,
        decimal discountBaseSubtotal,
        decimal deliveryFee)
    {
        var storeNow = GetStoreNow();
        var normalizedCode = rawCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return PromoResolution.Empty;
        }

        var promoCode = _dbContext.PromoCodes
            .AsNoTracking()
            .Include(item => item.Promotion)
                .ThenInclude(promotion => promotion!.RewardProduct)
            .FirstOrDefault(item => item.Code == normalizedCode);

        if (promoCode is null)
        {
            return PromoResolution.Invalid(normalizedCode, "ไม่พบโค้ดส่วนลดนี้");
        }

        if (!IsRecordActive(promoCode.Status))
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้ยังไม่พร้อมใช้งาน");
        }

        if (!IsWithinUsageWindow(promoCode.StartsAt, promoCode.ExpiresAt))
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้หมดอายุหรือยังไม่เริ่มใช้งาน");
        }

        if (promoCode.UsageLimit is int usageLimit && usageLimit > 0 && promoCode.UsedCount >= usageLimit)
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้ถูกใช้ครบสิทธิ์แล้ว");
        }

        var promotion = promoCode.Promotion;
        if (promotion is not null)
        {
            if (!IsRecordActive(promotion.Status))
            {
                return PromoResolution.Invalid(normalizedCode, "แคมเปญส่วนลดนี้ยังไม่พร้อมใช้งาน");
            }

            if (!IsPromotionActiveNow(promotion, storeNow))
            {
                return PromoResolution.Invalid(normalizedCode, "แคมเปญส่วนลดนี้หมดอายุหรือยังไม่เริ่มใช้งาน");
            }
        }

        var minOrderAmount = new[] { promoCode.MinOrderAmount, promotion?.MinOrderAmount }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (minOrderAmount > 0 && eligibilitySubtotal < minOrderAmount)
        {
            return PromoResolution.Invalid(normalizedCode, $"โค้ดนี้ใช้ได้เมื่อสั่งซื้อขั้นต่ำ {minOrderAmount:0.##} ฿");
        }

        var minItemQuantity = promotion?.MinItemQty ?? 0;
        var itemCount = cartItems.Sum(item => item.Quantity);
        if (minItemQuantity > 0 && itemCount < minItemQuantity)
        {
            return PromoResolution.Invalid(normalizedCode, $"โค้ดนี้ใช้ได้เมื่อซื้ออย่างน้อย {minItemQuantity} ชิ้น");
        }

        var discountAmount = CalculatePromoDiscountAmount(promoCode, promotion, discountBaseSubtotal);
        var shippingDiscountAmount = CalculateShippingDiscountAmount(promoCode, promotion, deliveryFee);
        var totalSavings = discountAmount + shippingDiscountAmount;

        if (totalSavings <= 0)
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดนี้ยังไม่ลดเพิ่มจากยอดสั่งซื้อปัจจุบัน");
        }

        var title = string.IsNullOrWhiteSpace(promoCode.Title)
            ? promotion?.Title ?? normalizedCode
            : promoCode.Title.Trim();
        var description = string.IsNullOrWhiteSpace(promoCode.Description)
            ? promotion?.Description?.Trim() ?? string.Empty
            : promoCode.Description.Trim();
        var savedLabel = totalSavings.ToString("0.##", CultureInfo.InvariantCulture);

        return PromoResolution.Applied(
            normalizedCode,
            title,
            description,
            discountAmount,
            shippingDiscountAmount,
            $"ใช้โค้ด {normalizedCode} แล้ว ประหยัด {savedLabel} ฿",
            promoCode.Id,
            promotion?.Id,
            promotion?.BenefitType ?? NormalizeDiscountType(promoCode.DiscountType),
            promotion?.RewardProductId,
            promotion?.RewardProduct?.Name ?? string.Empty,
            promotion?.RewardQty);
    }

    private static decimal CalculatePromoDiscountAmount(PromoCode promoCode, Promotion? promotion, decimal subtotal)
    {
        var discountType = NormalizeDiscountType(promoCode.DiscountType);
        var maxDiscount = promoCode.MaxDiscountAmount ?? promotion?.MaxDiscountAmount;
        decimal discountAmount;

        switch (discountType)
        {
            case "percent":
                discountAmount = subtotal * (promoCode.DiscountValue / 100m);
                break;
            case "amount":
                discountAmount = promoCode.DiscountValue;
                break;
            default:
                if (promotion?.DiscountPercent is decimal promotionPercent && promotionPercent > 0)
                {
                    discountAmount = subtotal * (promotionPercent / 100m);
                }
                else
                {
                    discountAmount = promotion?.DiscountAmount ?? 0;
                }

                break;
        }

        if (maxDiscount is decimal cap && cap > 0)
        {
            discountAmount = Math.Min(discountAmount, cap);
        }

        return Math.Min(Math.Max(discountAmount, 0), subtotal);
    }

    private static decimal CalculateShippingDiscountAmount(PromoCode promoCode, Promotion? promotion, decimal deliveryFee)
    {
        if (deliveryFee <= 0)
        {
            return 0;
        }

        var discountType = NormalizeDiscountType(promoCode.DiscountType);

        if (discountType == "shipping")
        {
            var shippingDiscount = promoCode.DiscountValue <= 0
                ? deliveryFee
                : promoCode.DiscountValue;

            return Math.Min(shippingDiscount, deliveryFee);
        }

        if (promotion?.FreeShipping == true)
        {
            return deliveryFee;
        }

        return 0;
    }

    private static string NormalizeDiscountType(string? discountType)
    {
        var normalized = discountType?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "percent" or "percentage" or "percent_discount" or "order_percent" => "percent",
            "amount" or "fixed" or "flat" or "fixed_amount" or "order_amount" => "amount",
            "shipping" or "shipping_discount" or "free_shipping" => "shipping",
            _ => normalized
        };
    }

    private static bool IsRecordActive(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "active" or "enabled" or "published" or "live";
    }

    private static bool IsWithinUsageWindow(DateTime? startsAt, DateTime? expiresAt)
    {
        var utcNow = DateTime.UtcNow;

        if (startsAt.HasValue && startsAt.Value > utcNow)
        {
            return false;
        }

        if (expiresAt.HasValue && expiresAt.Value < utcNow)
        {
            return false;
        }

        return true;
    }
}
