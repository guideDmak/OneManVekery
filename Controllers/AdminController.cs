using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OneManVekery.Models.Db;
using OneManVekery.Models;
using OneManVekery.ViewModel;
using System.Globalization;

namespace OneManVekery.Controllers;

public class AdminController : Controller
{
    private const int DefaultReorderLevel = 10;
    private readonly OneManVekeryDBContext _dbContext;

    public AdminController(OneManVekeryDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        if (!AdminPortalAuth.CanAccessAdmin(roleKey))
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบด้วยบัญชี Staff, Admin หรือ Owner ก่อนเข้าหน้าหลังบ้าน";
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        ViewData["AdminSignedInName"] = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountNameKey) ?? "Bakery Team";
        ViewData["AdminSignedInRole"] = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleLabelKey) ?? "Staff";

        base.OnActionExecuting(context);
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(BuildDashboardModel());
    }

    [HttpGet]
    public IActionResult Orders()
    {
        return View(BuildOrdersModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddOrder([Bind(Prefix = "AddForm")] AdminOrderCreateViewModel form)
    {
        form.Items ??= [];

        var customer = form.UserId <= 0
            ? null
            : _dbContext.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .FirstOrDefault(user => user.Id == form.UserId);

        if (customer is null)
        {
            ModelState.AddModelError("AddForm.UserId", "ไม่พบลูกค้าที่เลือก");
        }

        var requestedItems = form.Items
            .Where(item => item.ProductId > 0 && item.Quantity > 0)
            .GroupBy(item => item.ProductId)
            .Select(group => new AdminOrderLineEditorViewModel
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (requestedItems.Count == 0)
        {
            ModelState.AddModelError("AddForm.Items", "กรุณาเลือกสินค้าอย่างน้อย 1 รายการ");
        }

        var requestedProductIds = requestedItems
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var products = _dbContext.Products
            .Where(product => requestedProductIds.Contains(product.Id))
            .OrderBy(product => product.Name)
            .ToList();

        foreach (var requestedItem in requestedItems)
        {
            var product = products.FirstOrDefault(item => item.Id == requestedItem.ProductId);
            if (product is null || !product.IsActive)
            {
                ModelState.AddModelError("AddForm.Items", "มีสินค้าที่เลือกไม่พร้อมใช้งาน");
                continue;
            }

            if (requestedItem.Quantity > product.StockQty)
            {
                ModelState.AddModelError("AddForm.Items", $"สินค้า {product.Name} มีสต็อกไม่พอ");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Orders", BuildOrdersModel(addForm: EnsureOrderFormItems(form), activeModal: "order-add"));
        }

        var subtotal = requestedItems.Sum(requestedItem =>
        {
            var product = products.First(item => item.Id == requestedItem.ProductId);
            return product.Price * requestedItem.Quantity;
        });

        using var transaction = _dbContext.Database.BeginTransaction();

        var order = new Order
        {
            OrderNo = GenerateOrderNumber(),
            UserId = customer!.Id,
            CustomerName = customer.FullName,
            Phone = string.IsNullOrWhiteSpace(form.Phone) ? customer.Phone ?? string.Empty : form.Phone.Trim(),
            Address = form.Address.Trim(),
            PaymentMethod = NormalizePaymentMethod(form.PaymentMethod),
            PaymentStatus = NormalizePaymentStatus(form.PaymentStatus),
            OrderStatus = NormalizeOrderStatus(form.OrderStatus),
            Subtotal = subtotal,
            DeliveryFee = Math.Round(Math.Max(0, form.DeliveryFee), 2),
            TotalAmount = subtotal + Math.Round(Math.Max(0, form.DeliveryFee), 2),
            Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);
        _dbContext.SaveChanges();

        foreach (var requestedItem in requestedItems)
        {
            var product = products.First(item => item.Id == requestedItem.ProductId);
            var lineTotal = product.Price * requestedItem.Quantity;

            _dbContext.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Qty = requestedItem.Quantity,
                LineTotal = lineTotal
            });

            product.StockQty -= requestedItem.Quantity;
        }

        _dbContext.SaveChanges();
        transaction.Commit();

        TempData["SiteNotice"] = $"สร้างออเดอร์ {order.OrderNo} เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateOrderStatus([Bind(Prefix = "EditForm")] AdminOrderEditorViewModel form)
    {
        var order = form.OrderId <= 0
            ? null
            : _dbContext.Orders.FirstOrDefault(item => item.Id == form.OrderId);

        if (order is null)
        {
            TempData["SiteNotice"] = "ไม่พบออเดอร์ที่ต้องการอัปเดต";
            return RedirectToAction(nameof(Orders));
        }

        var normalizedOrderStatus = NormalizeOrderStatus(form.OrderStatus);
        var normalizedPaymentStatus = NormalizePaymentStatus(form.PaymentStatus);

        order.OrderStatus = normalizedOrderStatus;
        order.PaymentStatus = normalizedPaymentStatus;

        _dbContext.SaveChanges();

        TempData["SiteNotice"] = $"อัปเดตสถานะออเดอร์ {order.OrderNo} แล้ว";
        return RedirectToAction(nameof(Orders));
    }

    [HttpGet]
    public IActionResult Customers()
    {
        return View(BuildCustomersModel());
    }

    [HttpGet]
    public IActionResult Products()
    {
        return View(BuildProductsModel());
    }

    [HttpGet]
    public IActionResult Codes()
    {
        return View(BuildCodesModel());
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

        if (!ModelState.IsValid)
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

        if (!ModelState.IsValid)
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

    [HttpGet]
    public IActionResult Accounts()
    {
        return View(BuildAccountsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddAccount([Bind(Prefix = "AddForm")] AdminAccountEditorViewModel form)
    {
        var currentRoleKey = GetCurrentAdminRoleKey();

        if (string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError("AddForm.Password", "กรุณากำหนดรหัสผ่านสำหรับบัญชีนี้");
        }

        if (!CanAssignRole(currentRoleKey, form.Role))
        {
            ModelState.AddModelError("AddForm.Role", AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey)
                ? "บัญชีที่สร้างจากหน้านี้เลือกได้เฉพาะ User หรือ Staff"
                : "บัญชี Staff สร้างบัญชีใหม่ได้เฉพาะ User เท่านั้น");
        }

        if (EmailExists(form.Email))
        {
            ModelState.AddModelError("AddForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(addForm: form, activeModal: "account-add"));
        }

        var createdAccount = AddAccount(CreateAccountInput(form));
        TempData["SiteNotice"] = $"เพิ่มบัญชี {createdAccount.FullName} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAccount([Bind(Prefix = "EditForm")] AdminAccountEditorViewModel form)
    {
        var currentRoleKey = GetCurrentAdminRoleKey();
        var existingAccount = form.AccountId <= 0 ? null : GetAccount(form.AccountId);

        if (existingAccount is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Accounts));
        }

        if (!AdminPortalAuth.CanChangeAccountRoles(currentRoleKey))
        {
            form.Role = existingAccount.Role;
        }
        else if (!CanKeepOrAssignRole(currentRoleKey, form.Role, existingAccount.Role))
        {
            ModelState.AddModelError("EditForm.Role", "บัญชีนี้เปลี่ยน role ได้เฉพาะ User หรือ Staff");
        }

        if (EmailExists(form.Email, form.AccountId))
        {
            ModelState.AddModelError("EditForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(editForm: form, activeModal: "account-edit"));
        }

        UpdateAccount(form.AccountId, CreateAccountInput(form));
        TempData["SiteNotice"] = $"อัปเดตบัญชี {form.FullName} แล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CloseAccount(int accountId)
    {
        var existingAccount = GetAccount(accountId);
        if (accountId <= 0 || existingAccount is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการปิด";
            return RedirectToAction(nameof(Accounts));
        }

        if (!CloseAccountRecord(accountId))
        {
            TempData["SiteNotice"] = "ไม่สามารถปิดบัญชีนี้ได้";
            return RedirectToAction(nameof(Accounts));
        }

        TempData["SiteNotice"] = $"ปิดบัญชี {existingAccount.FullName} แล้ว";
        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public IActionResult Profile()
    {
        return View(BuildProfileModel());
    }

    private AdminDashboardViewModel BuildDashboardModel()
    {
        var orders = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();
        var products = _dbContext.Products
            .AsNoTracking()
            .ToList();
        var customers = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role.RoleKey == "user")
            .ToList();
        var sevenDayRevenue = orders
            .Where(order => order.CreatedAt.Date >= DateTime.Today.AddDays(-6))
            .Sum(order => order.TotalAmount);

        return new AdminDashboardViewModel
        {
            DateRangeLabel = $"Dashboard sync {DateTime.Now:dd MMM yyyy}",
            Metrics = BuildDashboardMetrics(orders, products, customers),
            TrendLabel = "Revenue (7 days)",
            TrendValue = $"{sevenDayRevenue:0.##} ฿",
            TrendDelta = $"{orders.Count(order => order.CreatedAt.Date >= DateTime.Today.AddDays(-6))} orders",
            TrendChart = BuildDashboardTrendChart(orders),
            SummaryItems = BuildOrderFulfillmentSummary(orders),
            TopProducts = BuildDashboardTopProducts(orders),
            LatestOrders = orders.Take(8).Select(MapOrder).ToList()
        };
    }

    private AdminOrdersViewModel BuildOrdersModel(
        AdminOrderCreateViewModel? addForm = null,
        AdminOrderEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var orders = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        var totalRevenue = orders.Sum(order => order.TotalAmount);
        var deliveredCount = orders.Count(order => string.Equals(NormalizeOrderStatus(order.OrderStatus), "delivered", StringComparison.OrdinalIgnoreCase));

        return new AdminOrdersViewModel
        {
            DateRangeLabel = $"Order sync {DateTime.Now:dd MMM yyyy}",
            Metrics = BuildOrderMetrics(orders),
            UpdateLabel = "Order Revenue",
            UpdateValue = $"{totalRevenue:0.##} ฿",
            UpdateDelta = $"{deliveredCount} delivered",
            UpdateChart = BuildOrderTrendChart(orders),
            FulfillmentSummary = BuildOrderFulfillmentSummary(orders),
            Orders = orders.Select(MapOrder).ToList(),
            OrderStatusOptions = BuildOrderStatusOptions(),
            PaymentStatusOptions = BuildPaymentStatusOptions(),
            PaymentMethodOptions = BuildPaymentMethodOptions(),
            CustomerOptions = BuildOrderCustomerOptions(),
            ProductOptions = BuildOrderProductOptions(),
            AddForm = EnsureOrderFormItems(addForm ?? new AdminOrderCreateViewModel
            {
                PaymentMethod = "card",
                PaymentStatus = "paid",
                OrderStatus = "paid"
            }),
            EditForm = editForm ?? new AdminOrderEditorViewModel
            {
                OrderStatus = "paid",
                PaymentStatus = "paid"
            },
            ActiveModal = activeModal
        };
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildOrderMetrics(IReadOnlyList<Order> orders)
    {
        var totalRevenue = orders.Sum(order => order.TotalAmount);
        var averageOrderValue = orders.Count == 0 ? 0 : orders.Average(order => order.TotalAmount);
        var totalItems = orders.Sum(order => order.OrderItems.Sum(item => item.Qty));
        var attentionCount = orders.Count(order =>
            string.Equals(NormalizeOrderStatus(order.OrderStatus), "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePaymentStatus(order.PaymentStatus), "failed", StringComparison.OrdinalIgnoreCase));

        return
        [
            new AdminMetricCardViewModel { Label = "Total Orders", Value = orders.Count.ToString(), Delta = $"{orders.Count(order => NormalizePaymentStatus(order.PaymentStatus) == "paid")} paid", PositiveTrend = true, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Revenue", Value = $"{totalRevenue:0.##} ฿", Delta = "Gross sales", PositiveTrend = true, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Avg Order", Value = $"{averageOrderValue:0.##} ฿", Delta = $"{totalItems} items total", PositiveTrend = true, AccentKey = "gold" },
            new AdminMetricCardViewModel { Label = "Need Action", Value = attentionCount.ToString(), Delta = "Paid / failed payment", PositiveTrend = attentionCount == 0, AccentKey = attentionCount > 0 ? "red" : "green" }
        ];
    }

    private static IReadOnlyList<AdminChartPointViewModel> BuildOrderTrendChart(IReadOnlyList<Order> orders)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => DateTime.Today.AddDays(offset - 6))
            .ToList();
        var counts = days
            .Select(day => orders.Count(order => order.CreatedAt.Date == day.Date))
            .ToList();
        var maxCount = counts.DefaultIfEmpty(0).Max();

        return days
            .Select((day, index) => new AdminChartPointViewModel
            {
                Label = day.ToString("dd"),
                Value = maxCount == 0 ? 12 : Math.Max(12, (int)Math.Round((counts[index] / (double)maxCount) * 100)),
                IsHighlighted = day.Date == DateTime.Today
            })
            .ToList();
    }

    private static IReadOnlyList<AdminInfoItemViewModel> BuildOrderFulfillmentSummary(IReadOnlyList<Order> orders)
    {
        var paidCount = orders.Count(order => string.Equals(NormalizeOrderStatus(order.OrderStatus), "paid", StringComparison.OrdinalIgnoreCase));
        var shippingCount = orders.Count(order => string.Equals(NormalizeOrderStatus(order.OrderStatus), "shipping", StringComparison.OrdinalIgnoreCase));
        var deliveredCount = orders.Count(order => string.Equals(NormalizeOrderStatus(order.OrderStatus), "delivered", StringComparison.OrdinalIgnoreCase));
        var refundedCount = orders.Count(order =>
            string.Equals(NormalizeOrderStatus(order.OrderStatus), "refunded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePaymentStatus(order.PaymentStatus), "refunded", StringComparison.OrdinalIgnoreCase));

        return
        [
            new AdminInfoItemViewModel { Label = "Paid", Value = paidCount.ToString(), Detail = "Ready to ship", AccentKey = "gold" },
            new AdminInfoItemViewModel { Label = "Shipping", Value = shippingCount.ToString(), Detail = "In transit", AccentKey = "blue" },
            new AdminInfoItemViewModel { Label = "Delivered", Value = deliveredCount.ToString(), Detail = "Completed orders", AccentKey = "green" },
            new AdminInfoItemViewModel { Label = "Refunded", Value = refundedCount.ToString(), Detail = "Need follow-up", AccentKey = "red" }
        ];
    }

    private static IReadOnlyList<string> BuildOrderStatusOptions()
    {
        return ["paid", "shipping", "delivered", "refunded", "cancelled"];
    }

    private static IReadOnlyList<string> BuildPaymentStatusOptions()
    {
        return ["pending", "paid", "failed", "refunded"];
    }

    private static IReadOnlyList<string> BuildPaymentMethodOptions()
    {
        return ["card", "bank-transfer", "cash"];
    }

    private IReadOnlyList<AdminSelectOptionViewModel> BuildOrderCustomerOptions()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user =>
                user.Status == "Active" &&
                user.Role.RoleKey == "user")
            .OrderBy(user => user.FullName)
            .Select(user => new AdminSelectOptionViewModel
            {
                Value = user.Id.ToString(),
                Label = user.FullName,
                SecondaryLabel = user.Email,
                DataValue = user.Phone ?? string.Empty
            })
            .ToList();
    }

    private IReadOnlyList<AdminSelectOptionViewModel> BuildOrderProductOptions()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.StockQty > 0)
            .OrderBy(product => product.Name)
            .Select(product => new AdminSelectOptionViewModel
            {
                Value = product.Id.ToString(),
                Label = product.Name,
                SecondaryLabel = $"{product.Sku} • {product.Price:0.##} ฿ / stock {product.StockQty}",
                DataValue = product.StockQty.ToString(),
                DataExtra = product.Price.ToString("0.##", CultureInfo.InvariantCulture)
            })
            .ToList();
    }

    private IReadOnlyList<InventoryItemRecord> GetAllInventoryItems()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .ToList()
            .Select(MapInventoryProduct)
            .ToList();
    }

    private InventoryItemRecord? GetInventoryItem(int itemId)
    {
        if (itemId <= 0)
        {
            return null;
        }

        var product = _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == itemId);

        return product is null ? null : MapInventoryProduct(product);
    }

    private bool SkuExists(string sku, int? excludingItemId = null)
    {
        var normalizedSku = NormalizeInventorySku(sku);

        return _dbContext.Products
            .AsNoTracking()
            .Any(item => item.Sku.ToUpper() == normalizedSku && item.Id != excludingItemId);
    }

    private InventoryItemRecord AddInventoryItem(InventoryItemInput input)
    {
        var category = ResolveOrCreateCategory(input.Category);
        var product = new Product
        {
            CategoryId = category.Id,
            Sku = NormalizeInventorySku(input.Sku),
            Name = NormalizeInventoryText(input.Name),
            Description = SerializeInventoryMeta(input),
            Price = NormalizeInventoryPrice(input.Price),
            StockQty = Math.Max(0, input.StockQuantity),
            ImageUrl = NormalizeInventoryImagePath(input.ImagePath),
            IsActive = input.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.SaveChanges();
        product.Category = category;

        return MapInventoryProduct(product);
    }

    private bool UpdateInventoryItem(int itemId, InventoryItemInput input)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        var category = ResolveOrCreateCategory(input.Category);
        product.CategoryId = category.Id;
        product.Category = category;
        product.Sku = NormalizeInventorySku(input.Sku);
        product.Name = NormalizeInventoryText(input.Name);
        product.Description = SerializeInventoryMeta(input);
        product.Price = NormalizeInventoryPrice(input.Price);
        product.StockQty = Math.Max(0, input.StockQuantity);
        product.ImageUrl = NormalizeInventoryImagePath(input.ImagePath);
        product.IsActive = input.IsPublished;

        _dbContext.SaveChanges();
        return true;
    }

    private bool SetPublishedState(int itemId, bool isPublished, string? visibilityNote = null)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        product.IsActive = isPublished;

        if (visibilityNote is not null)
        {
            var meta = ParseInventoryMeta(product.Description);
            product.Description = SerializeInventoryMeta(new InventoryMeta(meta.Tagline, NormalizeInventoryText(visibilityNote), meta.ReorderLevel));
        }

        _dbContext.SaveChanges();
        return true;
    }

    private bool AdjustInventoryStock(int itemId, int quantityDelta)
    {
        var product = _dbContext.Products.FirstOrDefault(item => item.Id == itemId);
        if (product is null)
        {
            return false;
        }

        var nextStock = product.StockQty + quantityDelta;
        if (nextStock < 0)
        {
            return false;
        }

        product.StockQty = nextStock;
        _dbContext.SaveChanges();
        return true;
    }

    private Category ResolveOrCreateCategory(string categoryName)
    {
        var normalizedCategoryName = string.IsNullOrWhiteSpace(categoryName)
            ? "Bakery"
            : NormalizeInventoryText(categoryName);
        var comparisonName = normalizedCategoryName.ToUpperInvariant();

        var existingCategory = _dbContext.Categories
            .FirstOrDefault(category => category.Name.ToUpper() == comparisonName);

        if (existingCategory is not null)
        {
            return existingCategory;
        }

        var newCategory = new Category
        {
            Name = normalizedCategoryName
        };

        _dbContext.Categories.Add(newCategory);
        _dbContext.SaveChanges();
        return newCategory;
    }

    private static InventoryItemRecord MapInventoryProduct(Product product)
    {
        var meta = ParseInventoryMeta(product.Description);

        return new InventoryItemRecord
        {
            ItemId = product.Id,
            ItemCode = $"ITM-{product.Id:0000}",
            Name = product.Name,
            Category = product.Category?.Name ?? "Bakery",
            Sku = product.Sku,
            Price = product.Price,
            StockQuantity = product.StockQty,
            ReorderLevel = meta.ReorderLevel,
            Tagline = meta.Tagline,
            Notes = meta.Notes,
            ImagePath = NormalizeInventoryImagePath(product.ImageUrl),
            IsPublished = product.IsActive,
            UpdatedAt = product.CreatedAt
        };
    }

    private static string SerializeInventoryMeta(InventoryItemInput input)
    {
        var payload = new InventoryMetaStorage
        {
            Tagline = NormalizeInventoryText(input.Tagline),
            Notes = NormalizeInventoryText(input.Notes),
            ReorderLevel = Math.Max(0, input.ReorderLevel)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string SerializeInventoryMeta(InventoryMeta input)
    {
        var payload = new InventoryMetaStorage
        {
            Tagline = NormalizeInventoryText(input.Tagline),
            Notes = NormalizeInventoryText(input.Notes),
            ReorderLevel = Math.Max(0, input.ReorderLevel)
        };

        return JsonSerializer.Serialize(payload);
    }

    private static InventoryMeta ParseInventoryMeta(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new InventoryMeta(string.Empty, string.Empty, DefaultReorderLevel);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<InventoryMetaStorage>(description);
            if (payload is not null)
            {
                return new InventoryMeta(
                    NormalizeInventoryText(payload.Tagline),
                    NormalizeInventoryText(payload.Notes),
                    payload.ReorderLevel < 0 ? DefaultReorderLevel : payload.ReorderLevel);
            }
        }
        catch (JsonException)
        {
        }

        return new InventoryMeta(NormalizeInventoryText(description), string.Empty, DefaultReorderLevel);
    }

    private static decimal NormalizeInventoryPrice(decimal price)
    {
        return Math.Round(Math.Max(0, price), 2);
    }

    private static string NormalizeInventoryText(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private static string NormalizeInventorySku(string value)
    {
        return NormalizeInventoryText(value).ToUpperInvariant();
    }

    private static string NormalizeInventoryImagePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/images/theme-cake.svg";
        }

        var normalized = value.Trim();
        return normalized.StartsWith("~/", StringComparison.Ordinal)
            ? "/" + normalized[2..]
            : normalized;
    }

    private IReadOnlyList<AccountRecord> GetAllAccounts()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.Status)
            .ThenBy(user => user.FullName)
            .Select(MapAccountRecord)
            .ToList();
    }

    private AccountRecord? GetAccount(int accountId)
    {
        var user = _dbContext.Users
            .AsNoTracking()
            .Include(entry => entry.Role)
            .FirstOrDefault(entry => entry.Id == accountId);

        return user is null ? null : MapAccountRecord(user);
    }

    private static IReadOnlyList<string> GetAccountStatuses()
    {
        return ["Active", "Suspended", "Closed"];
    }

    private bool EmailExists(string email, int? excludingAccountId = null)
    {
        var normalizedEmail = NormalizeAccountEmail(email);

        return _dbContext.Users.Any(user =>
            user.Email == normalizedEmail &&
            user.Id != excludingAccountId);
    }

    private AccountRecord AddAccount(AccountInput input)
    {
        var role = ResolveRole(input.Role);
        var user = new User
        {
            FullName = NormalizeAccountText(input.FullName),
            Email = NormalizeAccountEmail(input.Email),
            PasswordHash = NormalizeAccountPassword(input.Password),
            Phone = NormalizeOptionalAccountText(input.PhoneNumber),
            RoleId = role.Id,
            Status = NormalizeAccountStatusValue(input.Status),
            Notes = NormalizeOptionalAccountText(input.Notes),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _dbContext.Entry(user).Reference(entry => entry.Role).Load();

        return MapAccountRecord(user);
    }

    private bool UpdateAccount(int accountId, AccountInput input)
    {
        var user = _dbContext.Users.FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
        {
            return false;
        }

        var role = ResolveRole(input.Role);

        user.FullName = NormalizeAccountText(input.FullName);
        user.Email = NormalizeAccountEmail(input.Email);
        user.Phone = NormalizeOptionalAccountText(input.PhoneNumber);
        user.RoleId = role.Id;
        user.Status = NormalizeAccountStatusValue(input.Status);
        user.Notes = NormalizeOptionalAccountText(input.Notes);

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            user.PasswordHash = NormalizeAccountPassword(input.Password);
        }

        _dbContext.SaveChanges();
        return true;
    }

    private bool CloseAccountRecord(int accountId)
    {
        var user = _dbContext.Users.FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
        {
            return false;
        }

        user.Status = "closed";
        _dbContext.SaveChanges();
        return true;
    }

    private Role ResolveRole(string roleValue)
    {
        var normalized = roleValue.Trim();
        var role = _dbContext.Roles
            .AsNoTracking()
            .ToList()
            .FirstOrDefault(entry =>
                string.Equals(entry.RoleName, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.RoleKey, normalized, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            throw new InvalidOperationException($"Role '{roleValue}' was not found in the database.");
        }

        return role;
    }

    private static AccountRecord MapAccountRecord(User user)
    {
        var roleKey = user.Role?.RoleKey?.Trim().ToLowerInvariant() ?? "user";
        var roleLabel = NormalizeAccountRoleLabel(user.Role?.RoleName, roleKey);

        return new AccountRecord
        {
            AccountId = user.Id,
            AccountCode = $"ACC-{user.Id:0000}",
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.Phone ?? string.Empty,
            Role = roleLabel,
            RoleKey = roleKey,
            Status = NormalizeAccountStatusLabel(user.Status),
            LastActiveAt = user.LastActiveAt ?? user.CreatedAt,
            Notes = user.Notes ?? string.Empty
        };
    }

    private static string NormalizeAccountEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeAccountText(string value)
    {
        return value.Trim();
    }

    private static string NormalizeAccountPassword(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalAccountText(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeAccountStatusValue(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "suspended" => "suspended",
            "closed" => "closed",
            _ => "active"
        };
    }

    private static string NormalizeAccountStatusLabel(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => "Active"
        };
    }

    private static string NormalizeAccountRoleLabel(string? roleName, string? roleKey)
    {
        var source = string.IsNullOrWhiteSpace(roleName) ? roleKey : roleName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "User";
        }

        return source.Trim().ToLowerInvariant() switch
        {
            "owner" => "Owner",
            "admin" => "Admin",
            "manager" => "Manager",
            "staff" => "Staff",
            "support" => "Support",
            _ => "User"
        };
    }

    private sealed class InventoryMetaStorage
    {
        public string Tagline { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public int ReorderLevel { get; set; } = DefaultReorderLevel;
    }

    private sealed record InventoryMeta(string Tagline, string Notes, int ReorderLevel);

    private static AdminOrderRecordViewModel MapOrder(Order order)
    {
        var firstItem = order.OrderItems
            .OrderBy(item => item.Id)
            .FirstOrDefault();
        var totalQuantity = order.OrderItems.Sum(item => item.Qty);
        var orderStatus = GetOrderStatusPresentation(order.OrderStatus);
        var paymentStatus = GetPaymentStatusPresentation(order.PaymentStatus);

        return new AdminOrderRecordViewModel
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNo,
            ProductSummary = firstItem?.ProductName ?? "No items",
            ItemCountLabel = totalQuantity <= 0 ? "0 items" : $"{totalQuantity} items",
            CreatedAtLabel = order.CreatedAt.ToString("dd MMM yyyy, HH:mm"),
            CustomerName = order.CustomerName,
            TotalAmountLabel = $"{order.TotalAmount:0.##} ฿",
            PaymentMethodLabel = $"{FormatPaymentMethod(order.PaymentMethod)} / {paymentStatus.Label}",
            PaymentStatus = paymentStatus.Label,
            PaymentStatusKey = paymentStatus.Key,
            OrderStatus = orderStatus.Label,
            OrderStatusKey = orderStatus.Key,
            Phone = order.Phone,
            Address = order.Address,
            Note = order.Note ?? string.Empty
        };
    }

    private static string NormalizeOrderStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "delivered" => "delivered",
            "shipping" => "shipping",
            "refunded" => "refunded",
            "refund" => "refunded",
            "cancelled" => "cancelled",
            _ => "paid"
        };
    }

    private static string NormalizePaymentStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "pending" => "pending",
            "failed" => "failed",
            "refunded" => "refunded",
            _ => "paid"
        };
    }

    private static string NormalizePaymentMethod(string? paymentMethod)
    {
        return (paymentMethod ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "bank-transfer" => "bank-transfer",
            "cash" => "cash",
            _ => "card"
        };
    }

    private static (string Label, string Key) GetOrderStatusPresentation(string? status)
    {
        return NormalizeOrderStatus(status) switch
        {
            "shipping" => ("Shipping", "shipping"),
            "delivered" => ("Delivered", "completed"),
            "refunded" => ("Refunded", "refund"),
            "cancelled" => ("Cancelled", "refund"),
            _ => ("Paid", "pending")
        };
    }

    private static (string Label, string Key) GetPaymentStatusPresentation(string? status)
    {
        return NormalizePaymentStatus(status) switch
        {
            "pending" => ("Pending", "pending"),
            "failed" => ("Failed", "refund"),
            "refunded" => ("Refunded", "refund"),
            _ => ("Paid", "completed")
        };
    }

    private static string FormatPaymentMethod(string? paymentMethod)
    {
        return (paymentMethod ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "bank-transfer" => "Bank Transfer",
            "card" => "Card",
            "cash" => "Cash",
            _ => "Payment"
        };
    }

    private AdminOrderCreateViewModel EnsureOrderFormItems(AdminOrderCreateViewModel form)
    {
        form.Items = form.Items
            .Where(item => item.ProductId > 0 || item.Quantity > 0)
            .ToList();

        if (form.Items.Count == 0)
        {
            form.Items.Add(new AdminOrderLineEditorViewModel());
        }

        return form;
    }

    private string GenerateOrderNumber()
    {
        string orderNumber;

        do
        {
            orderNumber = $"OVK-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        }
        while (_dbContext.Orders.Any(order => order.OrderNo == orderNumber));

        return orderNumber;
    }

    private AdminCustomersViewModel BuildCustomersModel()
    {
        var customers = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.Orders)
            .Where(user => user.Role.RoleKey == "user")
            .OrderByDescending(user => user.LastActiveAt ?? user.CreatedAt)
            .ThenBy(user => user.FullName)
            .ToList();

        return new AdminCustomersViewModel
        {
            DateRangeLabel = $"Customer sync {DateTime.Now:dd MMM yyyy}",
            Metrics = BuildCustomerMetrics(customers),
            Customers = customers.Select(MapCustomer).ToList()
        };
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

    private AdminAccountsViewModel BuildAccountsModel(
        AdminAccountEditorViewModel? addForm = null,
        AdminAccountEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var currentRoleKey = GetCurrentAdminRoleKey();
        var accounts = GetAllAccounts();
        var statusOptions = GetAccountStatuses();
        var addRoleOptions = BuildAddRoleOptions(currentRoleKey);
        var editRoleOptions = addRoleOptions;
        var defaultRole = addRoleOptions.FirstOrDefault() ?? "User";

        return new AdminAccountsViewModel
        {
            DateRangeLabel = $"Account sync {DateTime.Now:dd MMM yyyy}",
            SummaryItems = BuildAccountSummary(accounts),
            Accounts = accounts.Select(MapAccount).ToList(),
            AddRoleOptions = addRoleOptions,
            EditRoleOptions = editRoleOptions,
            StatusOptions = statusOptions,
            CanCreateStaffAccounts = AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey),
            CanChangeRoles = AdminPortalAuth.CanChangeAccountRoles(currentRoleKey),
            AddForm = addForm ?? new AdminAccountEditorViewModel
            {
                Role = defaultRole,
                Status = "Active"
            },
            EditForm = editForm ?? new AdminAccountEditorViewModel
            {
                Role = defaultRole,
                Status = "Active"
            },
            ActiveModal = activeModal
        };
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

    private AdminCodesViewModel BuildCodesModel()
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

        var promotionRows = promotions
            .SelectMany(BuildPromotionRows)
            .OrderBy(record => record.StatusKey == "active" ? 0 : 1)
            .ThenBy(record => record.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activePromotions = promotions.Count(promotion => NormalizePromotionStatusKey(promotion.Status) == "active");
        var autoPromotions = promotions.Count(promotion => promotion.AutoApply && !promotion.RequiresCode);
        var codeBasedPromotions = promotions.Count(promotion => promotion.RequiresCode || promotion.PromoCodes.Count > 0);
        var loyaltyPromotions = promotions.Count(promotion => string.Equals(promotion.CampaignType, "loyalty", StringComparison.OrdinalIgnoreCase));
        var stackablePromotions = promotions.Count(promotion => promotion.CanStack);
        var freeShippingPromotions = promotions.Count(promotion => promotion.FreeShipping);
        var rewardPromotions = promotions.Count(promotion => (promotion.RewardQty ?? 0) > 0 || (promotion.PointsCost ?? 0) > 0);
        var totalCodeUses = promotions
            .SelectMany(promotion => promotion.PromoCodes)
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
                    Value = promotions.SelectMany(promotion => promotion.PromoCodes).Count().ToString(CultureInfo.InvariantCulture),
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
            Promotions = promotionRows
        };
    }

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
            Code = BuildPromotionCodeLabel(promotion, promoCode),
            Title = promotion.Title,
            DiscountLabel = BuildPromotionDiscountLabel(promotion, promoCode),
            RuleLabel = BuildPromotionRuleLabel(promotion, promoCode),
            Status = FormatPromotionStatusLabel(statusSource),
            StatusKey = NormalizePromotionStatusKey(statusSource),
            UsageLabel = BuildPromotionUsageLabel(promotion, promoCode),
            ExpiryLabel = BuildPromotionExpiryLabel(promotion, promoCode),
            Note = BuildPromotionNote(promotion, promoCode)
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

    private AdminProfileViewModel BuildProfileModel()
    {
        var currentAdminId = GetCurrentAdminAccountId();
        var adminAccount = currentAdminId <= 0
            ? null
            : _dbContext.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .FirstOrDefault(user => user.Id == currentAdminId);
        var customerCount = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Count(user => user.Role.RoleKey == "user");

        return new AdminProfileViewModel
        {
            AccountCode = adminAccount is null ? "-" : $"ACC-{adminAccount.Id:0000}",
            FullName = adminAccount?.FullName
                ?? (HttpContext.Session.GetString(AdminPortalAuth.SessionAccountNameKey) ?? "Bakery Team"),
            Role = FormatRoleLabel(
                adminAccount?.Role?.RoleName,
                adminAccount?.Role?.RoleKey,
                HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleLabelKey)),
            Email = adminAccount?.Email ?? "-",
            Phone = string.IsNullOrWhiteSpace(adminAccount?.Phone) ? "-" : adminAccount!.Phone!,
            Status = adminAccount is null ? "Unknown" : FormatStatusLabel(adminAccount.Status),
            LastActiveLabel = adminAccount is null
                ? "-"
                : FormatLastActive(adminAccount.LastActiveAt ?? adminAccount.CreatedAt),
            Notes = string.IsNullOrWhiteSpace(adminAccount?.Notes) ? "ไม่มีหมายเหตุในระบบ" : adminAccount!.Notes!,
            SummaryItems =
            [
                new AdminInfoItemViewModel { Label = "Orders in system", Value = _dbContext.Orders.AsNoTracking().Count().ToString(), Detail = "ทุกออเดอร์ที่อยู่ในฐานข้อมูล", AccentKey = "orange" },
                new AdminInfoItemViewModel { Label = "Products live", Value = _dbContext.Products.AsNoTracking().Count(product => product.IsActive).ToString(), Detail = "สินค้าที่เปิดขายบนหน้าร้าน", AccentKey = "green" },
                new AdminInfoItemViewModel { Label = "Registered users", Value = customerCount.ToString(), Detail = "บัญชีลูกค้าที่สมัครแล้ว", AccentKey = "blue" },
                new AdminInfoItemViewModel { Label = "Contact messages", Value = _dbContext.ContactMessages.AsNoTracking().Count().ToString(), Detail = "ข้อความที่เข้ามาจากหน้าเว็บไซต์", AccentKey = "gold" }
            ]
        };
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildDashboardMetrics(
        IReadOnlyList<Order> orders,
        IReadOnlyList<Product> products,
        IReadOnlyList<User> customers)
    {
        var totalRevenue = orders.Sum(order => order.TotalAmount);
        var deliveredOrders = orders.Count(order => NormalizeOrderStatus(order.OrderStatus) == "delivered");
        var activeCustomers = customers.Count(customer => NormalizeStatusKey(customer.Status) == "active");
        var liveProducts = products.Count(product => product.IsActive);
        var lowStockCount = products.Count(product => product.IsActive && product.StockQty > 0 && product.StockQty <= 10);

        return
        [
            new AdminMetricCardViewModel { Label = "Revenue", Value = $"{totalRevenue:0.##} ฿", Delta = $"{orders.Count} orders total", PositiveTrend = totalRevenue > 0, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Delivered", Value = deliveredOrders.ToString(), Delta = $"{orders.Count - deliveredOrders} still open", PositiveTrend = deliveredOrders > 0, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Customers", Value = customers.Count.ToString(), Delta = $"{activeCustomers} active accounts", PositiveTrend = activeCustomers > 0, AccentKey = "blue" },
            new AdminMetricCardViewModel { Label = "Live Products", Value = liveProducts.ToString(), Delta = $"{lowStockCount} low stock", PositiveTrend = liveProducts > 0, AccentKey = "gold" }
        ];
    }

    private static IReadOnlyList<AdminChartPointViewModel> BuildDashboardTrendChart(IReadOnlyList<Order> orders)
    {
        var startDate = DateTime.Today.AddDays(-6);
        var days = Enumerable.Range(0, 7)
            .Select(offset => startDate.AddDays(offset))
            .ToList();
        var revenues = days
            .Select(day => orders
                .Where(order => order.CreatedAt.Date == day.Date)
                .Sum(order => order.TotalAmount))
            .ToList();
        var maxRevenue = revenues.DefaultIfEmpty(0).Max();

        return days
            .Select((day, index) => new AdminChartPointViewModel
            {
                Label = day.ToString("dd"),
                Value = maxRevenue == 0
                    ? 12
                    : Math.Max(12, (int)Math.Round((double)(revenues[index] / maxRevenue) * 100)),
                IsHighlighted = day.Date == DateTime.Today
            })
            .ToList();
    }

    private static IReadOnlyList<AdminDashboardTopProductViewModel> BuildDashboardTopProducts(IReadOnlyList<Order> orders)
    {
        return orders
            .SelectMany(order => order.OrderItems)
            .GroupBy(item => item.ProductName)
            .Select(group => new
            {
                Name = group.Key,
                UnitsSold = group.Sum(item => item.Qty),
                Revenue = group.Sum(item => item.LineTotal)
            })
            .OrderByDescending(item => item.Revenue)
            .ThenByDescending(item => item.UnitsSold)
            .Take(5)
            .Select(item => new AdminDashboardTopProductViewModel
            {
                Name = item.Name,
                UnitsSoldLabel = $"{item.UnitsSold:N0} sold",
                RevenueLabel = $"{item.Revenue:0.##} ฿"
            })
            .ToList();
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildCustomerMetrics(IReadOnlyList<User> customers)
    {
        var activeCount = customers.Count(customer => NormalizeStatusKey(customer.Status) == "active");
        var orderingCustomers = customers.Count(customer => customer.Orders.Count > 0);
        var totalRevenue = customers.Sum(customer => customer.Orders.Sum(order => order.TotalAmount));

        return
        [
            new AdminMetricCardViewModel { Label = "Customers", Value = customers.Count.ToString(), Delta = $"{orderingCustomers} with orders", PositiveTrend = customers.Count > 0, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Active", Value = activeCount.ToString(), Delta = $"{customers.Count - activeCount} need review", PositiveTrend = activeCount > 0, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Revenue", Value = $"{totalRevenue:0.##} ฿", Delta = "รวมยอดซื้อจากลูกค้าทั้งหมด", PositiveTrend = totalRevenue > 0, AccentKey = "gold" },
            new AdminMetricCardViewModel { Label = "No Orders Yet", Value = (customers.Count - orderingCustomers).ToString(), Delta = "กลุ่มที่ยังไม่เคยสั่งซื้อ", PositiveTrend = orderingCustomers == customers.Count, AccentKey = "blue" }
        ];
    }

    private static AdminCustomerRecordViewModel MapCustomer(User customer)
    {
        var orderCount = customer.Orders.Count;
        var totalSpend = customer.Orders.Sum(order => order.TotalAmount);
        var lastOrderAt = customer.Orders
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => (DateTime?)order.CreatedAt)
            .FirstOrDefault();

        return new AdminCustomerRecordViewModel
        {
            CustomerId = customer.Id,
            CustomerCode = $"CUS-{customer.Id:0000}",
            FullName = customer.FullName,
            Email = customer.Email,
            PhoneNumber = string.IsNullOrWhiteSpace(customer.Phone) ? "-" : customer.Phone,
            Status = FormatStatusLabel(customer.Status),
            StatusKey = NormalizeStatusKey(customer.Status),
            OrderCountLabel = orderCount == 1 ? "1 order" : $"{orderCount} orders",
            TotalSpendLabel = $"{totalSpend:0.##} ฿",
            LastOrderLabel = lastOrderAt.HasValue ? lastOrderAt.Value.ToString("dd MMM yyyy") : "ยังไม่มีออเดอร์",
            LastActiveLabel = FormatLastActive(customer.LastActiveAt ?? customer.CreatedAt)
        };
    }

    private static InventoryItemInput CreateInventoryInput(AdminItemEditorViewModel form)
    {
        return new InventoryItemInput
        {
            Name = form.Name,
            Category = form.Category,
            Sku = form.Sku,
            Price = form.Price,
            StockQuantity = form.StockQuantity,
            ReorderLevel = form.ReorderLevel,
            Tagline = form.Tagline,
            Notes = form.Notes,
            ImagePath = form.ImagePath,
            IsPublished = form.IsPublished
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

    private static AccountInput CreateAccountInput(AdminAccountEditorViewModel form)
    {
        return new AccountInput
        {
            FullName = form.FullName,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            Role = form.Role,
            Status = form.Status,
            Notes = form.Notes,
            Password = form.Password ?? string.Empty
        };
    }

    private string GetCurrentAdminRoleKey()
    {
        return HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey)?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private int GetCurrentAdminAccountId()
    {
        return int.TryParse(HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey), out var accountId)
            ? accountId
            : 0;
    }

    private static string NormalizeStatusKey(string? status)
    {
        return (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "suspended" => "suspended",
            "closed" => "closed",
            _ => "active"
        };
    }

    private static string FormatStatusLabel(string? status)
    {
        return NormalizeStatusKey(status) switch
        {
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => "Active"
        };
    }

    private static string FormatRoleLabel(string? roleName, string? roleKey, string? fallbackLabel = null)
    {
        var source = !string.IsNullOrWhiteSpace(roleName)
            ? roleName
            : !string.IsNullOrWhiteSpace(fallbackLabel)
                ? fallbackLabel
                : roleKey;

        return (source ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "owner" => "Owner",
            "admin" => "Admin",
            "manager" => "Manager",
            "support" => "Support",
            "staff" => "Staff",
            "user" => "User",
            _ => string.IsNullOrWhiteSpace(source) ? "Staff" : source!.Trim()
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

    private bool IsAjaxRequest()
    {
        return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
               string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
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

    private static IReadOnlyList<string> BuildAddRoleOptions(string currentRoleKey)
    {
        return AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey)
            ? ["User", "Staff"]
            : ["User"];
    }

    private static bool CanAssignRole(string currentRoleKey, string requestedRole)
    {
        return BuildAddRoleOptions(currentRoleKey)
            .Contains(requestedRole, StringComparer.OrdinalIgnoreCase);
    }

    private static bool CanKeepOrAssignRole(string currentRoleKey, string requestedRole, string existingRole)
    {
        return string.Equals(requestedRole, existingRole, StringComparison.OrdinalIgnoreCase) ||
               CanAssignRole(currentRoleKey, requestedRole);
    }

    private static IReadOnlyList<AdminInfoItemViewModel> BuildAccountSummary(IReadOnlyList<AccountRecord> accounts)
    {
        var activeCount = accounts.Count(account => account.Status == "Active");
        var suspendedCount = accounts.Count(account => account.Status == "Suspended");
        var closedCount = accounts.Count(account => account.Status == "Closed");

        return
        [
            new AdminInfoItemViewModel { Label = "All Accounts", Value = accounts.Count.ToString(), Detail = "System members" },
            new AdminInfoItemViewModel { Label = "Active", Value = activeCount.ToString(), Detail = "Can sign in", AccentKey = "green" },
            new AdminInfoItemViewModel { Label = "Suspended", Value = suspendedCount.ToString(), Detail = "Waiting review", AccentKey = "gold" },
            new AdminInfoItemViewModel { Label = "Closed", Value = closedCount.ToString(), Detail = "Kept for history", AccentKey = "red" }
        ];
    }

    private static AdminAccountRecordViewModel MapAccount(AccountRecord account)
    {
        return new AdminAccountRecordViewModel
        {
            AccountId = account.AccountId,
            AccountCode = account.AccountCode,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            Role = account.Role,
            Status = account.Status,
            StatusKey = account.Status.ToLowerInvariant(),
            LastActive = FormatLastActive(account.LastActiveAt),
            LastActiveSort = account.LastActiveAt.Ticks,
            Notes = account.Notes
        };
    }

    private static string FormatLastActive(DateTime lastActiveAt)
    {
        return lastActiveAt.Date == DateTime.Today
            ? $"Today, {lastActiveAt:hh:mm tt}"
            : $"{lastActiveAt:MMM dd, yyyy}";
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

    private static IReadOnlyList<string> BuildImageOptions(IReadOnlyList<InventoryItemRecord> items, params string?[] extraImagePaths)
    {
        return items
            .Select(item => item.ImagePath)
            .Concat(
            [
                "/images/theme-cake.svg",
                "/images/theme-macaron.svg",
                "/images/theme-cream.svg",
                "/images/theme-gold.svg",
                "/images/theme-berry.svg",
                "/images/theme-milk.svg"
            ])
            .Concat(extraImagePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
