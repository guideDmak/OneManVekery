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
    public IActionResult Products()
    {
        return View(BuildProductsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetProductVisibility(int productId, string visibilityAction, string? visibilityNote)
    {
        var product = GetInventoryItem(productId);
        if (product is null)
        {
            if (IsAjaxRequest())
            {
                return NotFound(new { success = false, message = "ไม่พบสินค้าที่ต้องการอัปเดต" });
            }

            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการอัปเดต";
            return RedirectToAction(nameof(Products));
        }

        var normalizedAction = (visibilityAction ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedAction is not ("publish" or "hide"))
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new { success = false, message = "รูปแบบคำสั่งเปลี่ยนสถานะสินค้าไม่ถูกต้อง" });
            }

            TempData["SiteNotice"] = "รูปแบบคำสั่งเปลี่ยนสถานะสินค้าไม่ถูกต้อง";
            return RedirectToAction(nameof(Products));
        }

        var isPublished = normalizedAction == "publish";
        var normalizedNote = string.IsNullOrWhiteSpace(visibilityNote) ? string.Empty : visibilityNote.Trim();

        if (!isPublished && string.IsNullOrWhiteSpace(normalizedNote))
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new { success = false, message = "กรุณาระบุเหตุผลก่อนซ่อนสินค้าจากหน้าร้าน" });
            }

            TempData["SiteNotice"] = "กรุณาระบุเหตุผลก่อนซ่อนสินค้าจากหน้าร้าน";
            return RedirectToAction(nameof(Products));
        }

        if (!SetPublishedState(productId, isPublished, isPublished ? null : normalizedNote))
        {
            if (IsAjaxRequest())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "อัปเดตสถานะสินค้าไม่สำเร็จ" });
            }

            TempData["SiteNotice"] = "อัปเดตสถานะสินค้าไม่สำเร็จ";
            return RedirectToAction(nameof(Products));
        }

        var updatedProduct = GetInventoryItem(productId) ?? product;
        var message = isPublished
            ? $"เปิดขายสินค้า {product.Name} บนหน้าร้านแล้ว"
            : $"ซ่อนสินค้า {product.Name} ออกจากหน้าร้านแล้ว";

        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                message,
                product = BuildProductVisibilityPayload(updatedProduct)
            });
        }

        TempData["SiteNotice"] = message;
        return RedirectToAction(nameof(Products));
    }

    private AdminProductsViewModel BuildProductsModel()
    {
        var items = GetAllInventoryItems();
        var salesLookup = _dbContext.OrderItems
            .AsNoTracking()
            .Where(orderItem => orderItem.ProductId.HasValue)
            .GroupBy(orderItem => orderItem.ProductId!.Value)
            .Select(group => new
            {
                ProductId = group.Key,
                UnitsSold = group.Sum(orderItem => orderItem.Qty),
                RevenueAmount = group.Sum(orderItem => orderItem.LineTotal)
            })
            .ToDictionary(
                item => item.ProductId,
                item => (item.UnitsSold, item.RevenueAmount));

        var products = items
            .Select(item => MapProductShowcase(item, salesLookup))
            .OrderByDescending(item => item.IsPublished)
            .ThenByDescending(item => item.RevenueAmount)
            .ThenBy(item => item.Name)
            .ToList();

        return new AdminProductsViewModel
        {
            DateRangeLabel = $"Storefront sync {DateTime.Now:dd MMM yyyy}",
            Metrics = BuildProductMetrics(products),
            SummaryItems = BuildProductSummary(products),
            Products = products
        };
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildProductMetrics(IReadOnlyList<AdminProductShowcaseViewModel> products)
    {
        var publishedCount = products.Count(product => product.IsPublished);
        var hiddenCount = products.Count - publishedCount;
        var totalUnitsSold = products.Sum(product => product.UnitsSold);
        var totalRevenue = products.Sum(product => product.RevenueAmount);

        return
        [
            new AdminMetricCardViewModel { Label = "Live Products", Value = publishedCount.ToString(), Delta = $"{hiddenCount} hidden", PositiveTrend = publishedCount > 0, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Units Sold", Value = totalUnitsSold.ToString("N0"), Delta = "Across all orders", PositiveTrend = totalUnitsSold > 0, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Store Revenue", Value = $"{totalRevenue:0.##} ฿", Delta = "Product sales total", PositiveTrend = totalRevenue > 0, AccentKey = "gold" },
            new AdminMetricCardViewModel { Label = "Draft / Hidden", Value = hiddenCount.ToString(), Delta = "Not visible in storefront", PositiveTrend = hiddenCount == 0, AccentKey = hiddenCount == 0 ? "blue" : "red" }
        ];
    }

    private static IReadOnlyList<AdminInfoItemViewModel> BuildProductSummary(IReadOnlyList<AdminProductShowcaseViewModel> products)
    {
        var categoryCount = products
            .Select(product => product.Category)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var lowStockCount = products.Count(product => product.StockQuantity <= product.ReorderLevel && product.StockQuantity > 0);
        var soldOutCount = products.Count(product => product.StockQuantity == 0);
        var averagePrice = products.Count == 0
            ? 0
            : products
                .Select(product => decimal.TryParse(product.PriceLabel.Replace(" ฿", string.Empty), out var price) ? price : 0)
                .Average();

        return
        [
            new AdminInfoItemViewModel { Label = "Categories", Value = categoryCount.ToString(), Detail = "Active bakery groups", AccentKey = "blue" },
            new AdminInfoItemViewModel { Label = "Low Stock", Value = lowStockCount.ToString(), Detail = "สินค้าใกล้ถึงจุดเตือน", AccentKey = lowStockCount > 0 ? "gold" : "green" },
            new AdminInfoItemViewModel { Label = "Sold Out", Value = soldOutCount.ToString(), Detail = "ควรเติมสต็อกก่อนเปิดขาย", AccentKey = soldOutCount > 0 ? "red" : "green" },
            new AdminInfoItemViewModel { Label = "Average Price", Value = $"{averagePrice:0.##} ฿", Detail = "Average sell price", AccentKey = "orange" }
        ];
    }

    private static AdminProductShowcaseViewModel MapProductShowcase(
        InventoryItemRecord item,
        IReadOnlyDictionary<int, (int UnitsSold, decimal RevenueAmount)> salesLookup)
    {
        salesLookup.TryGetValue(item.ItemId, out var sales);

        return new AdminProductShowcaseViewModel
        {
            ProductId = item.ItemId,
            ProductCode = item.ItemCode,
            Name = item.Name,
            Category = item.Category,
            Tagline = item.Tagline,
            Notes = item.Notes,
            ImagePath = item.ImagePath,
            PriceLabel = $"{item.Price:0.##} ฿",
            StockLabel = $"Stock {item.StockQuantity:N0}",
            StockQuantity = item.StockQuantity,
            ReorderLevel = item.ReorderLevel,
            SalesLabel = $"{sales.UnitsSold:N0} sold",
            RevenueLabel = $"{sales.RevenueAmount:0.##} ฿",
            RevenueAmount = sales.RevenueAmount,
            UnitsSold = sales.UnitsSold,
            VisibilityLabel = item.IsPublished ? "Live" : "Hidden",
            VisibilityKey = item.IsPublished ? "green" : "gold",
            PublishedCopy = item.IsPublished ? "แสดงบนหน้าร้านและเลือกขายได้" : "ซ่อนจากหน้าร้านชั่วคราว",
            IsPublished = item.IsPublished
        };
    }

    private static object BuildProductVisibilityPayload(InventoryItemRecord item)
    {
        var isPublished = item.IsPublished;

        return new
        {
            productId = item.ItemId,
            isPublished,
            visibilityLabel = isPublished ? "Live" : "Hidden",
            visibilityKey = isPublished ? "green" : "gold",
            publishedCopy = isPublished ? "แสดงบนหน้าร้านและเลือกขายได้" : "ซ่อนจากหน้าร้านชั่วคราว",
            notes = item.Notes,
            buttonLabel = isPublished ? "Hide From Store" : "Publish To Store",
            nextAction = isPublished ? "hide" : "publish"
        };
    }
}
