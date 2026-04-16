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
    private PricingSummary BuildPricingSummary(IReadOnlyList<CartLineRecord> cartItems, CartCheckoutViewModel checkout)
    {
        var subtotal = cartItems.Sum(item => item.LineTotal);
        var deliveryFee = CalculateDeliveryFee(cartItems);
        var itemCount = cartItems.Sum(item => item.Quantity);
        var currentPoints = GetCurrentPointsBalance();
        var storeNow = GetStoreNow();
        var autoPromotions = GetActiveAutomaticPromotions(storeNow);
        var loyaltyPromotion = autoPromotions
            .FirstOrDefault(promotion => promotion.PointsAwarded.HasValue || promotion.PointsCost.HasValue || promotion.RewardProductId.HasValue);
        var appliedBenefits = new List<PricingBenefit>();
        var remainingSubtotal = subtotal;
        var remainingShipping = deliveryFee;
        decimal discountAmount = 0;
        decimal shippingDiscountAmount = 0;
        var rewardPointCost = loyaltyPromotion?.PointsCost ?? 0;
        var rewardQty = loyaltyPromotion?.RewardQty ?? 1;
        var rewardProductName = loyaltyPromotion?.RewardProduct?.Name ?? "ของรางวัล";
        var canRedeemFreeItem = loyaltyPromotion is not null
            && loyaltyPromotion.RewardProduct is not null
            && rewardPointCost > 0
            && currentPoints >= rewardPointCost
            && cartItems.Count > 0;

        foreach (var promotion in autoPromotions)
        {
            if (ReferenceEquals(promotion, loyaltyPromotion))
            {
                continue;
            }

            if (!PromotionMeetsThresholds(promotion, subtotal, itemCount))
            {
                continue;
            }

            if (promotion.BuyQty is > 0 && promotion.GetQty is > 0 && remainingSubtotal > 0)
            {
                var (freeItemCount, appliedDiscount) = CalculateBuyGetPromotionDiscount(cartItems, promotion, remainingSubtotal);
                if (appliedDiscount > 0)
                {
                    discountAmount += appliedDiscount;
                    remainingSubtotal -= appliedDiscount;
                    appliedBenefits.Add(new PricingBenefit(
                        promotion.Title,
                        string.IsNullOrWhiteSpace(promotion.Description)
                            ? $"รับสินค้าฟรี {freeItemCount} ชิ้นจากรายการในตะกร้า"
                            : promotion.Description.Trim(),
                        appliedDiscount,
                        0,
                        0,
                        0,
                        "success",
                        promotion.BenefitType,
                        promotion.Id,
                        null,
                        null,
                        string.Empty,
                        null));
                }

                continue;
            }

            if (promotion.FreeShipping && remainingShipping > 0)
            {
                shippingDiscountAmount += remainingShipping;
                appliedBenefits.Add(new PricingBenefit(
                    promotion.Title,
                    string.IsNullOrWhiteSpace(promotion.Description)
                        ? $"ยอดสินค้าครบ {promotion.MinOrderAmount:0.##} ฿ แล้ว"
                        : promotion.Description.Trim(),
                    0,
                    remainingShipping,
                    0,
                    0,
                    "success",
                    promotion.BenefitType,
                    promotion.Id,
                    null,
                    null,
                    string.Empty,
                    null));
                remainingShipping = 0;
                continue;
            }

            if (remainingSubtotal > 0 && (promotion.DiscountPercent is > 0 || promotion.DiscountAmount is > 0))
            {
                var appliedDiscount = CalculatePromotionDiscountAmount(promotion, remainingSubtotal);
                if (appliedDiscount > 0)
                {
                    discountAmount += appliedDiscount;
                    remainingSubtotal -= appliedDiscount;
                    appliedBenefits.Add(new PricingBenefit(
                        promotion.Title,
                        string.IsNullOrWhiteSpace(promotion.Description)
                            ? BuildPromotionDescription(promotion, storeNow, itemCount)
                            : promotion.Description.Trim(),
                        appliedDiscount,
                        0,
                        0,
                        0,
                        "success",
                        promotion.BenefitType,
                        promotion.Id,
                        null,
                        null,
                        string.Empty,
                        null));
                }
            }
        }

        var promo = ResolvePromoCode(checkout.PromoCode, cartItems, subtotal, remainingSubtotal, remainingShipping);
        if (promo.IsApplied)
        {
            if (promo.DiscountAmount > 0)
            {
                discountAmount += promo.DiscountAmount;
                remainingSubtotal -= promo.DiscountAmount;
            }

            if (promo.ShippingDiscountAmount > 0)
            {
                shippingDiscountAmount += promo.ShippingDiscountAmount;
                remainingShipping -= promo.ShippingDiscountAmount;
            }

            appliedBenefits.Add(new PricingBenefit(
                promo.Title,
                string.IsNullOrWhiteSpace(promo.Description) ? $"ใช้โค้ด {promo.Code}" : promo.Description,
                promo.DiscountAmount,
                promo.ShippingDiscountAmount,
                0,
                0,
                "success",
                promo.BenefitType,
                promo.PromotionId,
                promo.PromoCodeId,
                promo.RewardProductId,
                promo.RewardProductName,
                promo.RewardQty));
        }

        var pointsRedeemed = 0;
        if (checkout.UsePointsReward && loyaltyPromotion is not null && canRedeemFreeItem)
        {
            var rewardDiscount = ResolveRewardPromotionDiscount(loyaltyPromotion, remainingSubtotal);
            if (rewardDiscount > 0)
            {
                pointsRedeemed = rewardPointCost;
                discountAmount += rewardDiscount;
                remainingSubtotal -= rewardDiscount;
                appliedBenefits.Add(new PricingBenefit(
                    loyaltyPromotion.Title,
                    string.IsNullOrWhiteSpace(loyaltyPromotion.Description)
                        ? $"ใช้ {rewardPointCost} พอยต์แลก {rewardProductName} ฟรี {rewardQty} ชิ้น"
                        : loyaltyPromotion.Description.Trim(),
                    rewardDiscount,
                    0,
                    0,
                    pointsRedeemed,
                    "success",
                    loyaltyPromotion.BenefitType,
                    loyaltyPromotion.Id,
                    null,
                    loyaltyPromotion.RewardProductId,
                    rewardProductName,
                    rewardQty));
            }
        }

        var maxPointDiscountRedeem = CalculateMaxPointDiscountRedeem(currentPoints - pointsRedeemed, remainingSubtotal);
        var (pointDiscountRedeemed, pointDiscountAmount) = ResolvePointDiscount(checkout.PointsToRedeem, maxPointDiscountRedeem);
        if (pointDiscountRedeemed > 0 && pointDiscountAmount > 0)
        {
            pointsRedeemed += pointDiscountRedeemed;
            discountAmount += pointDiscountAmount;
            remainingSubtotal -= pointDiscountAmount;
            appliedBenefits.Add(new PricingBenefit(
                "ใช้พอยต์ลดราคา",
                $"ใช้ {pointDiscountRedeemed} P ลดราคา {pointDiscountAmount:0.##} ฿",
                pointDiscountAmount,
                0,
                0,
                pointDiscountRedeemed,
                "success",
                "points_discount",
                loyaltyPromotion?.Id,
                null,
                null,
                string.Empty,
                null));
        }

        var pointsEarned = CalculateEarnedPoints(loyaltyPromotion, remainingSubtotal);
        if (pointsEarned > 0)
        {
            appliedBenefits.Add(new PricingBenefit(
                loyaltyPromotion?.Title ?? "รับคะแนนสะสม",
                BuildLoyaltyEarningDescription(loyaltyPromotion),
                0,
                0,
                pointsEarned,
                0,
                "neutral",
                loyaltyPromotion?.BenefitType ?? "points_reward",
                loyaltyPromotion?.Id,
                null,
                loyaltyPromotion?.RewardProductId,
                rewardProductName,
                rewardQty));
        }

        return new PricingSummary(
            subtotal,
            deliveryFee,
            discountAmount,
            shippingDiscountAmount,
            currentPoints,
            pointsEarned,
            pointsRedeemed,
            pointDiscountAmount,
            maxPointDiscountRedeem,
            currentPoints - pointsRedeemed + pointsEarned,
            rewardPointCost,
            rewardQty,
            rewardProductName,
            canRedeemFreeItem,
            loyaltyPromotion?.Id,
            appliedBenefits,
            promo);
    }

    private static int CalculateMaxPointDiscountRedeem(int availablePoints, decimal discountableSubtotal)
    {
        if (availablePoints <= 0 || discountableSubtotal <= 0)
        {
            return 0;
        }

        var pointsBySubtotal = (int)Math.Floor(discountableSubtotal / PointDiscountValuePerStep * PointDiscountPointStep);
        return Math.Min(availablePoints, Math.Max(0, pointsBySubtotal));
    }

    private static (int PointsRedeemed, decimal DiscountAmount) ResolvePointDiscount(int requestedPoints, int maxRedeemablePoints)
    {
        var pointsRedeemed = Math.Min(Math.Max(0, requestedPoints), Math.Max(0, maxRedeemablePoints));
        var discountAmount = Math.Round(pointsRedeemed / (decimal)PointDiscountPointStep * PointDiscountValuePerStep, 2, MidpointRounding.AwayFromZero);

        return (pointsRedeemed, discountAmount);
    }

    private static CheckoutBenefitViewModel MapBenefit(PricingBenefit benefit)
    {
        var valueParts = new List<string>();

        if (benefit.DiscountAmount > 0)
        {
            valueParts.Add($"-{benefit.DiscountAmount:0.##} ฿");
        }

        if (benefit.ShippingDiscountAmount > 0)
        {
            valueParts.Add($"ส่งฟรี {benefit.ShippingDiscountAmount:0.##} ฿");
        }

        if (benefit.PointsEarned > 0)
        {
            valueParts.Add($"+{benefit.PointsEarned} P");
        }

        if (benefit.PointsRedeemed > 0)
        {
            valueParts.Add($"-{benefit.PointsRedeemed} P");
        }

        return new CheckoutBenefitViewModel
        {
            Title = benefit.Title,
            Description = benefit.Description,
            ValueLabel = valueParts.Count == 0 ? string.Empty : string.Join(" • ", valueParts),
            Tone = benefit.Tone
        };
    }

    private static CheckoutBenefitViewModel MapBenefit(OrderPromotion benefit)
    {
        var valueParts = new List<string>();

        if (benefit.DiscountAmount > 0)
        {
            valueParts.Add($"-{benefit.DiscountAmount:0.##} ฿");
        }

        if (benefit.ShippingDiscountAmount > 0)
        {
            valueParts.Add($"ส่งฟรี {benefit.ShippingDiscountAmount:0.##} ฿");
        }

        if (benefit.PointsEarned > 0)
        {
            valueParts.Add($"+{benefit.PointsEarned} P");
        }

        if (benefit.PointsRedeemed > 0)
        {
            valueParts.Add($"-{benefit.PointsRedeemed} P");
        }

        var tone = benefit.PointsEarned > 0 && benefit.DiscountAmount <= 0 && benefit.ShippingDiscountAmount <= 0
            ? "neutral"
            : "success";

        return new CheckoutBenefitViewModel
        {
            Title = benefit.PromotionTitle,
            Description = benefit.Note ?? string.Empty,
            ValueLabel = valueParts.Count == 0 ? string.Empty : string.Join(" • ", valueParts),
            Tone = tone
        };
    }
}
