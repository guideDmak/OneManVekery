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
    public IActionResult Items()
    {
        return View(BuildItemsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddCategory([Bind(Prefix = "CategoryForm")] AdminCategoryEditorViewModel form)
    {
        form.Name = NormalizeCategoryName(form.Name);

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            ModelState.AddModelError("CategoryForm.Name", "กรุณากรอกชื่อหมวดสินค้า");
        }

        if (!ModelState.IsValid)
        {
            var validationMessage = GetFirstModelError() ?? "ไม่สามารถเพิ่มหมวดสินค้าได้";
            if (IsAjaxRequest())
            {
                return BadRequest(new { success = false, message = validationMessage });
            }

            return View("Items", BuildItemsModel(categoryForm: form, activeModal: "category"));
        }

        var normalizedName = form.Name.ToUpperInvariant();
        var existingCategory = _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefault(category => category.Name.ToUpper() == normalizedName);

        if (existingCategory is not null)
        {
            var existingMessage = $"หมวดสินค้า {existingCategory.Name} มีอยู่แล้ว";
            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = existingMessage,
                    categoryName = existingCategory.Name
                });
            }

            TempData["SiteNotice"] = existingMessage;
            return RedirectToAction(nameof(Items));
        }

        var category = new Category
        {
            Name = form.Name
        };

        _dbContext.Categories.Add(category);
        _dbContext.SaveChanges();

        var message = $"เพิ่มหมวดสินค้า {category.Name} เรียบร้อยแล้ว";
        if (IsAjaxRequest())
        {
            return Json(new
            {
                success = true,
                message,
                categoryName = category.Name
            });
        }

        TempData["SiteNotice"] = message;
        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddItem([Bind(Prefix = "AddForm")] AdminItemEditorViewModel form)
    {
        if (SkuExists(form.Sku))
        {
            ModelState.AddModelError("AddForm.Sku", "SKU นี้ถูกใช้งานแล้ว");
        }

        ValidateItemImageUpload(form.ImageFile, "AddForm.ImageFile");

        if (!ModelState.IsValid)
        {
            return View("Items", BuildItemsModel(addForm: form, activeModal: "add"));
        }

        if (!TryApplyUploadedItemImage(form, "AddForm.ImageFile"))
        {
            return View("Items", BuildItemsModel(addForm: form, activeModal: "add"));
        }

        var createdItem = AddInventoryItem(CreateInventoryInput(form));
        TempData["SiteNotice"] = $"เพิ่มสินค้า {createdItem.Name} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateItem([Bind(Prefix = "EditForm")] AdminItemEditorViewModel form)
    {
        if (form.ItemId <= 0 || GetInventoryItem(form.ItemId) is null)
        {
            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Items));
        }

        if (SkuExists(form.Sku, form.ItemId))
        {
            ModelState.AddModelError("EditForm.Sku", "SKU นี้ถูกใช้งานแล้ว");
        }

        ValidateItemImageUpload(form.ImageFile, "EditForm.ImageFile");

        if (!ModelState.IsValid)
        {
            return View("Items", BuildItemsModel(editForm: form, activeModal: "edit"));
        }

        if (!TryApplyUploadedItemImage(form, "EditForm.ImageFile"))
        {
            return View("Items", BuildItemsModel(editForm: form, activeModal: "edit"));
        }

        UpdateInventoryItem(form.ItemId, CreateInventoryInput(form));
        TempData["SiteNotice"] = $"อัปเดตสินค้า {form.Name} แล้ว";

        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AdjustItemStock(int itemId, int quantityAmount = 1, int quantityDirection = 1)
    {
        var existingItem = GetInventoryItem(itemId);
        if (itemId <= 0 || existingItem is null)
        {
            return HandleItemStockResult("ไม่พบสินค้าที่ต้องการปรับสต็อก", null, isSuccess: false, statusCode: StatusCodes.Status404NotFound);
        }

        if (quantityAmount <= 0)
        {
            return HandleItemStockResult("กรุณาระบุจำนวนสต็อกที่ต้องการเปลี่ยนอย่างน้อย 1", existingItem, isSuccess: false, statusCode: StatusCodes.Status400BadRequest);
        }

        var normalizedDirection = quantityDirection < 0 ? -1 : 1;
        var quantityDelta = quantityAmount * normalizedDirection;

        if (normalizedDirection < 0 && quantityAmount > existingItem.StockQuantity)
        {
            return HandleItemStockResult("จำนวนที่ลดต้องไม่เกินสต็อกคงเหลือ", existingItem, isSuccess: false, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!AdjustInventoryStock(itemId, quantityDelta))
        {
            return HandleItemStockResult($"สต็อกของ {existingItem.Name} ลดต่ำกว่า 0 ไม่ได้", existingItem, isSuccess: false, statusCode: StatusCodes.Status400BadRequest);
        }

        var updatedItem = GetInventoryItem(itemId) ?? existingItem;
        var actionLabel = normalizedDirection > 0
            ? $"เพิ่มสต็อก {quantityAmount}"
            : $"ลดสต็อก {quantityAmount}";
        var notice = $"{actionLabel} สำหรับ {updatedItem.Name} แล้ว";

        return HandleItemStockResult(notice, updatedItem, isSuccess: true, updatedAtOverride: DateTime.Now);
    }

    private AdminItemsPageViewModel BuildItemsModel(
        AdminCategoryEditorViewModel? categoryForm = null,
        AdminItemEditorViewModel? addForm = null,
        AdminItemEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var items = GetAllInventoryItems();
        var persistedCategories = _dbContext.Categories
            .AsNoTracking()
            .Select(category => category.Name)
            .ToArray();
        var categories = BuildCategories(
            items,
            persistedCategories
                .Concat(new[] { addForm?.Category, editForm?.Category, categoryForm?.Name })
                .ToArray());
        var imageOptions = BuildImageOptions(items, addForm?.ImagePath, editForm?.ImagePath);

        return new AdminItemsPageViewModel
        {
            DateRangeLabel = $"Inventory sync {DateTime.Now:dd MMM yyyy}",
            SummaryItems = BuildInventorySummary(items),
            Items = items.Select(MapInventoryItem).ToList(),
            Categories = categories,
            ImageOptions = imageOptions,
            CategoryForm = categoryForm ?? new AdminCategoryEditorViewModel(),
            AddForm = addForm ?? new AdminItemEditorViewModel
            {
                ReorderLevel = 10,
                ImagePath = "/images/theme-cake.svg",
                IsPublished = true
            },
            EditForm = editForm ?? new AdminItemEditorViewModel
            {
                ImagePath = "/images/theme-cake.svg",
                IsPublished = true
            },
            ActiveModal = activeModal
        };
    }

    private IActionResult HandleItemStockResult(
        string message,
        InventoryItemRecord? item,
        bool isSuccess,
        int statusCode = StatusCodes.Status200OK,
        DateTime? updatedAtOverride = null)
    {
        if (IsAjaxRequest())
        {
            var payload = new
            {
                success = isSuccess,
                message,
                item = item is null ? null : BuildStockPayload(item, updatedAtOverride)
            };

            return StatusCode(statusCode, payload);
        }

        TempData["SiteNotice"] = message;
        return RedirectToAction(nameof(Items));
    }

    private static IReadOnlyList<AdminInfoItemViewModel> BuildInventorySummary(IReadOnlyList<InventoryItemRecord> items)
    {
        var publishedCount = items.Count(item => item.IsPublished);
        var lowStockCount = items.Count(item => item.IsPublished && item.StockQuantity > 0 && item.StockQuantity <= item.ReorderLevel);
        var soldOutCount = items.Count(item => item.IsPublished && item.StockQuantity == 0);
        var totalUnits = items.Sum(item => item.StockQuantity);

        return
        [
            new AdminInfoItemViewModel { Label = "All Items", Value = items.Count.ToString(), Detail = "Tracked bakery products" },
            new AdminInfoItemViewModel { Label = "Published", Value = publishedCount.ToString(), Detail = "Visible in storefront", AccentKey = "green" },
            new AdminInfoItemViewModel { Label = "Low Stock", Value = lowStockCount.ToString(), Detail = "Need refill soon", AccentKey = "gold" },
            new AdminInfoItemViewModel { Label = "Units On Hand", Value = totalUnits.ToString("N0"), Detail = $"{soldOutCount} sold out", AccentKey = soldOutCount > 0 ? "red" : "blue" }
        ];
    }

    private static AdminInventoryItemViewModel MapInventoryItem(InventoryItemRecord item)
    {
        var status = GetInventoryStatus(item);

        return new AdminInventoryItemViewModel
        {
            ItemId = item.ItemId,
            ItemCode = item.ItemCode,
            Sku = item.Sku,
            Name = item.Name,
            Category = item.Category,
            Tagline = item.Tagline,
            Notes = item.Notes,
            ImagePath = item.ImagePath,
            PriceAmount = item.Price,
            PriceLabel = $"{item.Price:0.##} ฿",
            StockQuantity = item.StockQuantity,
            ReorderLevel = item.ReorderLevel,
            StatusLabel = status.Label,
            StatusKey = status.Key,
            UpdatedAtLabel = item.UpdatedAt.ToString("dd MMM yyyy, HH:mm"),
            UpdatedAtSort = item.UpdatedAt.Ticks,
            IsPublished = item.IsPublished
        };
    }

    private static object BuildStockPayload(InventoryItemRecord item, DateTime? updatedAtOverride = null)
    {
        var mappedItem = MapInventoryItem(item);
        var updatedAtLabel = (updatedAtOverride ?? item.UpdatedAt).ToString("dd MMM yyyy, HH:mm");

        return new
        {
            mappedItem.ItemId,
            mappedItem.StockQuantity,
            mappedItem.StatusLabel,
            mappedItem.StatusKey,
            UpdatedAtLabel = updatedAtLabel
        };
    }

    private static IReadOnlyList<string> BuildCategories(IReadOnlyList<InventoryItemRecord> items, params string?[] extraCategories)
    {
        return items
            .Select(item => item.Category)
            .Concat(extraCategories.Where(category => !string.IsNullOrWhiteSpace(category)).Select(category => category!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeCategoryName(string? categoryName)
    {
        return string.IsNullOrWhiteSpace(categoryName)
            ? string.Empty
            : string.Join(" ", categoryName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private string? GetFirstModelError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
    }

    private static (string Label, string Key) GetInventoryStatus(InventoryItemRecord item)
    {
        if (!item.IsPublished)
        {
            return ("Draft", "draft");
        }

        if (item.StockQuantity <= 0)
        {
            return ("Sold Out", "sold-out");
        }

        if (item.StockQuantity <= item.ReorderLevel)
        {
            return ("Low Stock", "low-stock");
        }

        return ("In Stock", "in-stock");
    }
}
