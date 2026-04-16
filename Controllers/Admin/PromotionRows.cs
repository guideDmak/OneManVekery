using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using OneManVekery.Models.Db;
using OneManVekery.Models;
using OneManVekery.ViewModel;
using System.Globalization;

namespace OneManVekery.Controllers;

public partial class AdminController
{
    private static IReadOnlyList<AdminPromotionRecordViewModel> BuildPromotionRows(Promotion promotion)
    {
        var promoCodes = promotion.PromoCodes
            .OrderBy(code => code.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (promoCodes.Count == 0)
        {
            return [MapPromotionRecord(promotion, null)];
        }

        return promoCodes
            .Select(code => MapPromotionRecord(promotion, code))
            .ToList();
    }

    private static AdminPromotionRecordViewModel MapPromotionRecord(Promotion promotion, PromoCode? promoCode)
    {
        var statusSource = promoCode?.Status ?? promotion.Status;

        return new AdminPromotionRecordViewModel
        {
            PromoCodeId = promoCode?.Id,
            IsPromoCode = promoCode is not null,
            CreatedAtSort = (promoCode?.CreatedAt ?? promotion.CreatedAt).Ticks,
            Code = BuildPromotionCodeLabel(promotion, promoCode),
            Title = string.IsNullOrWhiteSpace(promoCode?.Title) ? promotion.Title : promoCode!.Title.Trim(),
            DiscountLabel = BuildPromotionDiscountLabel(promotion, promoCode),
            RuleLabel = BuildPromotionRuleLabel(promotion, promoCode),
            Status = FormatPromotionStatusLabel(statusSource),
            StatusKey = NormalizePromotionStatusKey(statusSource),
            UsageLabel = BuildPromotionUsageLabel(promotion, promoCode),
            ExpiryLabel = BuildPromotionExpiryLabel(promotion, promoCode),
            Note = BuildPromotionNote(promotion, promoCode)
        };
    }

    private static AdminPromotionRecordViewModel MapStandalonePromoCodeRecord(PromoCode promoCode)
    {
        return new AdminPromotionRecordViewModel
        {
            PromoCodeId = promoCode.Id,
            IsPromoCode = true,
            CreatedAtSort = promoCode.CreatedAt.Ticks,
            Code = promoCode.Code,
            Title = promoCode.Title,
            DiscountLabel = BuildStandalonePromoCodeDiscountLabel(promoCode),
            RuleLabel = BuildStandalonePromoCodeRuleLabel(promoCode),
            Status = FormatPromotionStatusLabel(promoCode.Status),
            StatusKey = NormalizePromotionStatusKey(promoCode.Status),
            UsageLabel = BuildStandalonePromoCodeUsageLabel(promoCode),
            ExpiryLabel = BuildStandalonePromoCodeExpiryLabel(promoCode),
            Note = string.IsNullOrWhiteSpace(promoCode.Note) ? "Standalone code" : promoCode.Note.Trim()
        };
    }

    private static string BuildPromotionCodeLabel(Promotion promotion, PromoCode? promoCode)
    {
        if (!string.IsNullOrWhiteSpace(promoCode?.Code))
        {
            return promoCode.Code;
        }

        if (string.Equals(promotion.CampaignType, "loyalty", StringComparison.OrdinalIgnoreCase))
        {
            return "LOYALTY";
        }

        return promotion.AutoApply ? "AUTO" : promotion.PromotionKey.ToUpperInvariant();
    }

    private static string BuildPromotionDiscountLabel(Promotion promotion, PromoCode? promoCode)
    {
        if (promoCode is not null)
        {
            return NormalizeDiscountType(promoCode.DiscountType) switch
            {
                "percent" => promoCode.MaxDiscountAmount is decimal maxDiscount
                    ? $"ลด {promoCode.DiscountValue:0.##}% สูงสุด {maxDiscount:0.##} ฿"
                    : $"ลด {promoCode.DiscountValue:0.##}%",
                "amount" => $"ลด {promoCode.DiscountValue:0.##} ฿",
                "shipping" => "ส่งฟรี",
                _ => promotion.Title
            };
        }

        var parts = new List<string>();

        if (promotion.BuyQty is > 0 && promotion.GetQty is > 0)
        {
            parts.Add($"ซื้อ {promotion.BuyQty:0} แถม {promotion.GetQty:0}");
        }

        if (promotion.DiscountPercent is decimal discountPercent)
        {
            parts.Add($"ลด {discountPercent:0.##}%");
        }

        if (promotion.DiscountAmount is decimal discountAmount)
        {
            parts.Add($"ลด {discountAmount:0.##} ฿");
        }

        if (promotion.FreeShipping)
        {
            parts.Add("ส่งฟรี");
        }

        if (promotion.PointsAwarded is > 0 && promotion.SpendStepAmount is decimal spendStepAmount)
        {
            parts.Add($"ทุก {spendStepAmount:0.##} ฿ ได้ {promotion.PointsAwarded:0} พอยต์");
        }

        if (promotion.PointsCost is > 0)
        {
            var rewardLabel = promotion.RewardProduct?.Name ?? "ของรางวัล";
            var rewardQty = promotion.RewardQty.GetValueOrDefault(1);
            parts.Add($"แลก {promotion.PointsCost:0} พอยต์ รับ {rewardLabel} x{rewardQty:0}");
        }

        return parts.Count == 0
            ? promotion.BenefitType
            : string.Join(" / ", parts);
    }

    private static string BuildStandalonePromoCodeDiscountLabel(PromoCode promoCode)
    {
        return NormalizeDiscountType(promoCode.DiscountType) switch
        {
            "percent" => promoCode.MaxDiscountAmount is decimal maxDiscount
                ? $"ลด {promoCode.DiscountValue:0.##}% สูงสุด {maxDiscount:0.##} ฿"
                : $"ลด {promoCode.DiscountValue:0.##}%",
            "amount" => $"ลด {promoCode.DiscountValue:0.##} ฿",
            "shipping" => promoCode.DiscountValue <= 0
                ? "ส่งฟรี"
                : $"ลดค่าส่ง {promoCode.DiscountValue:0.##} ฿",
            _ => promoCode.Title
        };
    }

    private static string BuildPromotionRuleLabel(Promotion promotion, PromoCode? promoCode)
    {
        var parts = new List<string>();

        if (promoCode is not null)
        {
            parts.Add("กรอกโค้ด");
        }
        else if (promotion.AutoApply)
        {
            parts.Add("อัตโนมัติ");
        }

        if ((promoCode?.MinOrderAmount ?? promotion.MinOrderAmount) is decimal minOrderAmount)
        {
            parts.Add($"ขั้นต่ำ {minOrderAmount:0.##} ฿");
        }

        if (promotion.MinItemQty is > 0)
        {
            parts.Add($"ครบ {promotion.MinItemQty:0} ชิ้น");
        }

        if (promotion.BuyQty is > 0)
        {
            parts.Add($"ซื้ออย่างน้อย {promotion.BuyQty:0} ชิ้น");
        }

        var targetLabel = BuildPromotionTargetLabel(promotion);
        if (!string.IsNullOrWhiteSpace(targetLabel))
        {
            parts.Add(targetLabel);
        }

        var weekdayLabel = BuildWeekdayLabel(promotion.WeekdayMask);
        if (!string.IsNullOrWhiteSpace(weekdayLabel))
        {
            parts.Add(weekdayLabel);
        }

        if (promotion.DailyStartTime is TimeOnly startTime && promotion.DailyEndTime is TimeOnly endTime)
        {
            parts.Add($"{startTime:HH\\:mm}-{endTime:HH\\:mm}");
        }

        return parts.Count == 0
            ? "ไม่มีเงื่อนไขพิเศษ"
            : string.Join(" | ", parts);
    }

    private static string BuildStandalonePromoCodeRuleLabel(PromoCode promoCode)
    {
        var parts = new List<string> { "กรอกโค้ด" };

        if (promoCode.MinOrderAmount is decimal minOrderAmount)
        {
            parts.Add($"ขั้นต่ำ {minOrderAmount:0.##} ฿");
        }

        return string.Join(" | ", parts);
    }

    private static string BuildPromotionUsageLabel(Promotion promotion, PromoCode? promoCode)
    {
        if (promoCode is not null)
        {
            return promoCode.UsageLimit is int usageLimit
                ? $"{promoCode.UsedCount:0}/{usageLimit:0} ครั้ง"
                : $"{promoCode.UsedCount:0} ครั้ง";
        }

        return $"{promotion.OrderPromotions.Count:0} ออเดอร์";
    }

    private static string BuildStandalonePromoCodeUsageLabel(PromoCode promoCode)
    {
        return promoCode.UsageLimit is int usageLimit
            ? $"{promoCode.UsedCount:0}/{usageLimit:0} ครั้ง"
            : $"{promoCode.UsedCount:0} ครั้ง";
    }

    private static string BuildPromotionExpiryLabel(Promotion promotion, PromoCode? promoCode)
    {
        var startsAt = promoCode?.StartsAt ?? promotion.StartsAt;
        var expiresAt = promoCode?.ExpiresAt ?? promotion.ExpiresAt;

        if (startsAt.HasValue || expiresAt.HasValue)
        {
            var startLabel = startsAt?.ToString("dd MMM yyyy") ?? "-";
            var endLabel = expiresAt?.ToString("dd MMM yyyy") ?? "ไม่กำหนด";
            return $"{startLabel} -> {endLabel}";
        }

        if (promotion.DailyStartTime is TimeOnly startTime && promotion.DailyEndTime is TimeOnly endTime)
        {
            return $"ทุกวัน {startTime:HH\\:mm}-{endTime:HH\\:mm}";
        }

        var weekdayLabel = BuildWeekdayLabel(promotion.WeekdayMask);
        if (!string.IsNullOrWhiteSpace(weekdayLabel))
        {
            return weekdayLabel;
        }

        return "ไม่กำหนดวันหมดอายุ";
    }

    private static string BuildStandalonePromoCodeExpiryLabel(PromoCode promoCode)
    {
        if (promoCode.StartsAt.HasValue || promoCode.ExpiresAt.HasValue)
        {
            var startLabel = promoCode.StartsAt?.ToString("dd MMM yyyy") ?? "-";
            var endLabel = promoCode.ExpiresAt?.ToString("dd MMM yyyy") ?? "ไม่กำหนด";
            return $"{startLabel} -> {endLabel}";
        }

        return "ไม่กำหนดวันหมดอายุ";
    }

    private static string BuildPromotionNote(Promotion promotion, PromoCode? promoCode)
    {
        var noteParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(promotion.Note))
        {
            noteParts.Add(promotion.Note);
        }

        if (!string.IsNullOrWhiteSpace(promoCode?.Note))
        {
            noteParts.Add(promoCode.Note);
        }

        if (!promotion.CanStack)
        {
            noteParts.Add("ไม่ stack กับโปรอื่น");
        }

        return noteParts.Count == 0
            ? "-"
            : string.Join(" | ", noteParts.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildPromotionTargetLabel(Promotion promotion)
    {
        var targets = promotion.PromotionTargets
            .Select(target => target.Product?.Name ?? target.Category?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(2)
            .ToList();

        if (targets.Count > 0)
        {
            return $"เป้าหมาย {string.Join(", ", targets)}";
        }

        return (promotion.TargetScope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "order" => "ทั้งออเดอร์",
            "store" => "ทั้งร้าน",
            "product" => "ตามสินค้า",
            "category" => "ตามหมวด",
            _ => string.Empty
        };
    }

    private static string BuildWeekdayLabel(int? weekdayMask)
    {
        if (!weekdayMask.HasValue || weekdayMask.Value <= 0)
        {
            return string.Empty;
        }

        var days = new List<string>();
        var dayMap = new (int Mask, string Label)[]
        {
            (1, "อาทิตย์"),
            (2, "จันทร์"),
            (4, "อังคาร"),
            (8, "พุธ"),
            (16, "พฤหัส"),
            (32, "ศุกร์"),
            (64, "เสาร์")
        };

        foreach (var day in dayMap)
        {
            if ((weekdayMask.Value & day.Mask) == day.Mask)
            {
                days.Add(day.Label);
            }
        }

        return days.Count == 0 ? string.Empty : $"วัน{string.Join(", ", days)}";
    }

    private static string NormalizeDiscountType(string? discountType)
    {
        return (discountType ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizePromotionStatusKey(string? status)
    {
        return (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "draft" => "draft",
            "paused" => "paused",
            "expired" => "expired",
            "inactive" => "paused",
            _ => "active"
        };
    }

    private static string FormatPromotionStatusLabel(string? status)
    {
        return NormalizePromotionStatusKey(status) switch
        {
            "draft" => "Draft",
            "paused" => "Paused",
            "expired" => "Expired",
            _ => "Active"
        };
    }
}
