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
    [HttpGet]
    public IActionResult Codes()
    {
        return View(BuildCodesModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePromoCode([Bind(Prefix = "CreateForm")] AdminPromoCodeEditorViewModel form)
    {
        ValidatePromoCodeForm(form);

        if (!ModelState.IsValid)
        {
            return View("Codes", BuildCodesModel(form, activeModal: "code-create"));
        }

        var promoCode = CreatePromoCodeRecord(form);
        TempData["SiteNotice"] = $"สร้างโค้ด {promoCode.Code} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Codes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePromoCodeStatus(int promoCodeId, string targetStatus)
    {
        var normalizedStatus = NormalizePromotionStatusKey(targetStatus);
        if (!GetPromotionStatusValues().Contains(normalizedStatus, StringComparer.OrdinalIgnoreCase))
        {
            TempData["SiteNotice"] = "สถานะโค้ดไม่ถูกต้อง";
            return RedirectToAction(nameof(Codes));
        }

        var promoCode = _dbContext.PromoCodes.FirstOrDefault(code => code.Id == promoCodeId);
        if (promoCode is null)
        {
            TempData["SiteNotice"] = "ไม่พบโค้ดที่ต้องการอัปเดต";
            return RedirectToAction(nameof(Codes));
        }

        if (normalizedStatus == "active" && promoCode.ExpiresAt.HasValue && promoCode.ExpiresAt <= DateTime.UtcNow)
        {
            TempData["SiteNotice"] = $"โค้ด {promoCode.Code} หมดอายุแล้ว ไม่สามารถเปิดใช้งานได้";
            return RedirectToAction(nameof(Codes));
        }

        promoCode.Status = normalizedStatus;
        _dbContext.SaveChanges();

        TempData["SiteNotice"] = $"อัปเดตสถานะโค้ด {promoCode.Code} เป็น {FormatPromotionStatusLabel(normalizedStatus)} แล้ว";
        return RedirectToAction(nameof(Codes));
    }

    private AdminCodesViewModel BuildCodesModel(AdminPromoCodeEditorViewModel? createForm = null, string activeModal = "")
    {
        var promotions = _dbContext.Promotions
            .AsNoTracking()
            .Include(promotion => promotion.PromoCodes)
            .Include(promotion => promotion.OrderPromotions)
            .Include(promotion => promotion.PromotionTargets)
                .ThenInclude(target => target.Product)
            .Include(promotion => promotion.PromotionTargets)
                .ThenInclude(target => target.Category)
            .Include(promotion => promotion.RewardProduct)
            .OrderBy(promotion => promotion.Priority)
            .ThenBy(promotion => promotion.Title)
            .ToList();

        var standalonePromoCodes = _dbContext.PromoCodes
            .AsNoTracking()
            .Where(code => code.PromotionId == null)
            .OrderByDescending(code => code.CreatedAt)
            .ToList();

        var promotionRows = promotions
            .SelectMany(BuildPromotionRows)
            .Concat(standalonePromoCodes.Select(MapStandalonePromoCodeRecord))
            .OrderBy(record => record.StatusKey == "active" ? 0 : 1)
            .ThenByDescending(record => record.CreatedAtSort)
            .ThenBy(record => record.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activePromotions = promotions.Count(promotion => NormalizePromotionStatusKey(promotion.Status) == "active");
        var autoPromotions = promotions.Count(promotion => promotion.AutoApply && !promotion.RequiresCode);
        var codeBasedPromotions = promotions.Count(promotion => promotion.RequiresCode || promotion.PromoCodes.Count > 0) + standalonePromoCodes.Count;
        var loyaltyPromotions = promotions.Count(promotion => string.Equals(promotion.CampaignType, "loyalty", StringComparison.OrdinalIgnoreCase));
        var stackablePromotions = promotions.Count(promotion => promotion.CanStack);
        var freeShippingPromotions = promotions.Count(promotion => promotion.FreeShipping);
        var rewardPromotions = promotions.Count(promotion => (promotion.RewardQty ?? 0) > 0 || (promotion.PointsCost ?? 0) > 0);
        var totalCodeUses = promotions
            .SelectMany(promotion => promotion.PromoCodes)
            .Concat(standalonePromoCodes)
            .Sum(code => code.UsedCount);
        var totalAppliedOrders = promotions.Sum(promotion => promotion.OrderPromotions.Count);
        var loyaltyWalletCount = _dbContext.LoyaltyWallets.AsNoTracking().Count();

        return new AdminCodesViewModel
        {
            DateRangeLabel = $"Promotion sync {DateTime.Now:dd MMM yyyy}",
            Metrics =
            [
                new AdminMetricCardViewModel
                {
                    Label = "Promotions",
                    Value = promotions.Count.ToString(CultureInfo.InvariantCulture),
                    Delta = $"{activePromotions} active campaigns",
                    PositiveTrend = activePromotions > 0,
                    AccentKey = "gold"
                },
                new AdminMetricCardViewModel
                {
                    Label = "Auto Apply",
                    Value = autoPromotions.ToString(CultureInfo.InvariantCulture),
                    Delta = $"{codeBasedPromotions} ต้องกรอกโค้ด",
                    PositiveTrend = autoPromotions > 0,
                    AccentKey = "green"
                },
                new AdminMetricCardViewModel
                {
                    Label = "Promo Codes",
                    Value = (promotions.SelectMany(promotion => promotion.PromoCodes).Count() + standalonePromoCodes.Count).ToString(CultureInfo.InvariantCulture),
                    Delta = $"{totalCodeUses} ครั้งที่ถูกใช้",
                    PositiveTrend = totalCodeUses > 0,
                    AccentKey = "blue"
                },
                new AdminMetricCardViewModel
                {
                    Label = "Loyalty",
                    Value = loyaltyPromotions.ToString(CultureInfo.InvariantCulture),
                    Delta = $"{loyaltyWalletCount} wallets พร้อมใช้งาน",
                    PositiveTrend = loyaltyPromotions > 0,
                    AccentKey = "orange"
                }
            ],
            SummaryItems =
            [
                new AdminInfoItemViewModel
                {
                    Label = "Stackable",
                    Value = stackablePromotions.ToString(CultureInfo.InvariantCulture),
                    Detail = "โปรโมชั่นที่ใช้ร่วมกับสิทธิ์อื่นได้",
                    AccentKey = stackablePromotions > 0 ? "green" : "gold"
                },
                new AdminInfoItemViewModel
                {
                    Label = "Free Shipping",
                    Value = freeShippingPromotions.ToString(CultureInfo.InvariantCulture),
                    Detail = "กฎที่มีผลกับค่าจัดส่ง",
                    AccentKey = freeShippingPromotions > 0 ? "blue" : "gold"
                },
                new AdminInfoItemViewModel
                {
                    Label = "Rewards",
                    Value = rewardPromotions.ToString(CultureInfo.InvariantCulture),
                    Detail = "แคมเปญสะสมแต้มและของรางวัล",
                    AccentKey = rewardPromotions > 0 ? "orange" : "gold"
                },
                new AdminInfoItemViewModel
                {
                    Label = "Applied Orders",
                    Value = totalAppliedOrders.ToString(CultureInfo.InvariantCulture),
                    Detail = "ออเดอร์ที่มีการบันทึกโปรโมชันจริง",
                    AccentKey = totalAppliedOrders > 0 ? "green" : "gold"
                }
            ],
            Promotions = promotionRows,
            PromotionOptions = BuildPromotionSelectOptions(promotions),
            DiscountTypeOptions = BuildPromoCodeDiscountTypeOptions(),
            StatusOptions = GetPromotionStatusOptions(),
            CreateForm = createForm ?? new AdminPromoCodeEditorViewModel
            {
                DiscountType = "percent",
                DiscountValue = 10,
                Status = "Active"
            },
            ActiveModal = activeModal
        };
    }

    private void ValidatePromoCodeForm(AdminPromoCodeEditorViewModel form)
    {
        var normalizedCode = NormalizePromoCodeValue(form.Code);
        form.Code = normalizedCode;

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.Code)), "กรุณากรอกโค้ด");
        }
        else if (!IsPromoCodeFormatValid(normalizedCode))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.Code)), "โค้ดใช้ได้เฉพาะ A-Z, 0-9, - และ _");
        }
        else if (_dbContext.PromoCodes.Any(code => code.Code == normalizedCode))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.Code)), "โค้ดนี้มีอยู่ในระบบแล้ว");
        }

        if (form.PromotionId is int promotionId &&
            !_dbContext.Promotions.AsNoTracking().Any(promotion => promotion.Id == promotionId))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.PromotionId)), "ไม่พบ campaign ที่เลือก");
        }

        var discountType = NormalizeDiscountType(form.DiscountType);
        if (!GetPromoCodeDiscountTypes().Contains(discountType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.DiscountType)), "ประเภทส่วนลดไม่ถูกต้อง");
        }

        if (discountType == "percent" && (form.DiscountValue <= 0 || form.DiscountValue > 100))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.DiscountValue)), "ส่วนลดแบบเปอร์เซ็นต์ต้องอยู่ระหว่าง 1-100");
        }
        else if (discountType == "amount" && form.DiscountValue <= 0)
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.DiscountValue)), "ส่วนลดแบบจำนวนเงินต้องมากกว่า 0");
        }
        else if (discountType == "shipping" && form.DiscountValue < 0)
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.DiscountValue)), "ส่วนลดค่าส่งต้องไม่ติดลบ");
        }

        if (form.StartsAt.HasValue && form.ExpiresAt.HasValue && form.ExpiresAt <= form.StartsAt)
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.ExpiresAt)), "วันหมดอายุต้องอยู่หลังวันเริ่มใช้");
        }

        var status = NormalizePromotionStatusKey(form.Status);
        if (!GetPromotionStatusValues().Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.Status)), "สถานะโค้ดไม่ถูกต้อง");
        }
        else if (status == "active" && ConvertStoreLocalToUtc(form.ExpiresAt) is DateTime expiresAtUtc && expiresAtUtc <= DateTime.UtcNow)
        {
            ModelState.AddModelError(PromoCodeField(nameof(form.ExpiresAt)), "โค้ด Active ต้องมีวันหมดอายุที่ยังไม่ผ่าน");
        }
    }

    private PromoCode CreatePromoCodeRecord(AdminPromoCodeEditorViewModel form)
    {
        var discountType = NormalizeDiscountType(form.DiscountType);
        var promoCode = new PromoCode
        {
            PromotionId = form.PromotionId,
            Code = NormalizePromoCodeValue(form.Code),
            Title = NormalizeAccountText(form.Title),
            Description = NormalizeOptionalAccountText(form.Description),
            DiscountType = discountType,
            DiscountValue = discountType == "shipping" && form.DiscountValue <= 0 ? 0 : form.DiscountValue,
            MinOrderAmount = NormalizePositiveAmount(form.MinOrderAmount),
            MaxDiscountAmount = NormalizePositiveAmount(form.MaxDiscountAmount),
            UsageLimit = form.UsageLimit,
            UsedCount = 0,
            StartsAt = ConvertStoreLocalToUtc(form.StartsAt),
            ExpiresAt = ConvertStoreLocalToUtc(form.ExpiresAt),
            Status = NormalizePromotionStatusKey(form.Status),
            Note = NormalizeOptionalAccountText(form.Note),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PromoCodes.Add(promoCode);
        _dbContext.SaveChanges();

        return promoCode;
    }

    private static IReadOnlyList<AdminSelectOptionViewModel> BuildPromotionSelectOptions(IReadOnlyList<Promotion> promotions)
    {
        return promotions
            .OrderBy(promotion => promotion.Title, StringComparer.OrdinalIgnoreCase)
            .Select(promotion => new AdminSelectOptionViewModel
            {
                Value = promotion.Id.ToString(CultureInfo.InvariantCulture),
                Label = $"{promotion.PromotionKey} - {promotion.Title}",
                SecondaryLabel = $"{BuildPromotionDiscountLabel(promotion, null)} | {FormatPromotionStatusLabel(promotion.Status)}",
                DataValue = NormalizePromotionStatusKey(promotion.Status),
                DataExtra = BuildPromotionRuleLabel(promotion, null)
            })
            .ToList();
    }

    private static IReadOnlyList<AdminSelectOptionViewModel> BuildPromoCodeDiscountTypeOptions()
    {
        return
        [
            new AdminSelectOptionViewModel { Value = "percent", Label = "ลดเป็นเปอร์เซ็นต์", SecondaryLabel = "เช่น ลด 10%" },
            new AdminSelectOptionViewModel { Value = "amount", Label = "ลดเป็นจำนวนเงิน", SecondaryLabel = "เช่น ลด 50 ฿" },
            new AdminSelectOptionViewModel { Value = "shipping", Label = "ลดค่าส่ง / ส่งฟรี", SecondaryLabel = "ใส่ 0 เพื่อฟรีค่าส่งเต็มจำนวน" }
        ];
    }

    private static IReadOnlyList<string> GetPromoCodeDiscountTypes()
    {
        return ["percent", "amount", "shipping"];
    }

    private static IReadOnlyList<string> GetPromotionStatusOptions()
    {
        return ["Active", "Draft", "Paused"];
    }

    private static IReadOnlyList<string> GetPromotionStatusValues()
    {
        return ["active", "draft", "paused"];
    }

    private static string PromoCodeField(string propertyName)
    {
        return $"{nameof(AdminCodesViewModel.CreateForm)}.{propertyName}";
    }

    private static string NormalizePromoCodeValue(string? value)
    {
        return string.Concat((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(character => !char.IsWhiteSpace(character)));
    }

    private static bool IsPromoCodeFormatValid(string value)
    {
        return value.All(character =>
            (character >= 'A' && character <= 'Z') ||
            (character >= '0' && character <= '9') ||
            character is '-' or '_');
    }

    private static decimal? NormalizePositiveAmount(decimal? value)
    {
        return value.HasValue && value.Value > 0 ? value.Value : null;
    }

    private static DateTime? ConvertStoreLocalToUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var localTimestamp = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localTimestamp, GetStoreTimeZone());
    }

    private static TimeZoneInfo GetStoreTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
    }
}
