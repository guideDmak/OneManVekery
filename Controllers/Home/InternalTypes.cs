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
    private sealed record PricingSummary(
        decimal Subtotal,
        decimal DeliveryFee,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        int CurrentPoints,
        int PointsEarned,
        int PointsRedeemed,
        decimal PointsDiscountAmount,
        int MaxPointDiscountRedeem,
        int ProjectedPointsBalance,
        int RewardPointCost,
        int RewardQty,
        string RewardProductName,
        bool CanRedeemFreeItem,
        int? LoyaltyPromotionId,
        IReadOnlyList<PricingBenefit> AppliedBenefits,
        PromoResolution Promo);

    private sealed record PricingBenefit(
        string Title,
        string Description,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        int PointsEarned,
        int PointsRedeemed,
        string Tone,
        string BenefitType,
        int? PromotionId,
        int? PromoCodeId,
        int? RewardProductId,
        string RewardProductName,
        int? RewardQty);

    private sealed record PromoResolution(
        bool IsValid,
        bool IsApplied,
        string Code,
        string Title,
        string Description,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        string Message,
        string MessageState,
        int? PromoCodeId,
        int? PromotionId,
        string BenefitType,
        int? RewardProductId,
        string RewardProductName,
        int? RewardQty)
    {
        public static PromoResolution Empty { get; } = new(
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            string.Empty,
            string.Empty,
            null,
            null,
            string.Empty,
            null,
            string.Empty,
            null);

        public static PromoResolution Invalid(string code, string message) => new(
            false,
            false,
            code,
            string.Empty,
            string.Empty,
            0,
            0,
            message,
            "warning",
            null,
            null,
            string.Empty,
            null,
            string.Empty,
            null);

        public static PromoResolution Applied(
            string code,
            string title,
            string description,
            decimal discountAmount,
            decimal shippingDiscountAmount,
            string message,
            int? promoCodeId,
            int? promotionId,
            string benefitType,
            int? rewardProductId,
            string rewardProductName,
            int? rewardQty) => new(
            true,
            true,
            code,
            title,
            description,
            discountAmount,
            shippingDiscountAmount,
            message,
            "success",
            promoCodeId,
            promotionId,
            benefitType,
            rewardProductId,
            rewardProductName,
            rewardQty);
    }
}
