using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OneManVekery.Models.Db;
using OneManVekery.Services;
using OneManVekery.ViewModel;
using System.Globalization;

namespace OneManVekery.Controllers;

public class AdminController : Controller
{
    private readonly IAccountDirectoryService _accountDirectoryService;
    private readonly IInventoryCatalogService _inventoryCatalogService;
    private readonly OneManVekeryDBContext _dbContext;

    public AdminController(
        IAccountDirectoryService accountDirectoryService,
        IInventoryCatalogService inventoryCatalogService,
        OneManVekeryDBContext dbContext)
    {
        _accountDirectoryService = accountDirectoryService;
        _inventoryCatalogService = inventoryCatalogService;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetProductVisibility(int productId, bool isPublished)
    {
        var product = _inventoryCatalogService.GetItem(productId);
        if (product is null)
        {
            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการอัปเดต";
            return RedirectToAction(nameof(Products));
        }

        if (!_inventoryCatalogService.SetPublishedState(productId, isPublished))
        {
            TempData["SiteNotice"] = "อัปเดตสถานะสินค้าไม่สำเร็จ";
            return RedirectToAction(nameof(Products));
        }

        TempData["SiteNotice"] = isPublished
            ? $"เปิดขายสินค้า {product.Name} บนหน้าร้านแล้ว"
            : $"ซ่อนสินค้า {product.Name} ออกจากหน้าร้านแล้ว";

        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public IActionResult Items()
    {
        return View(BuildItemsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddItem([Bind(Prefix = "AddForm")] AdminItemEditorViewModel form)
    {
        if (_inventoryCatalogService.SkuExists(form.Sku))
        {
            ModelState.AddModelError("AddForm.Sku", "SKU นี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Items", BuildItemsModel(addForm: form, activeModal: "add"));
        }

        var createdItem = _inventoryCatalogService.AddItem(CreateInventoryInput(form));
        TempData["SiteNotice"] = $"เพิ่มสินค้า {createdItem.Name} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateItem([Bind(Prefix = "EditForm")] AdminItemEditorViewModel form)
    {
        if (form.ItemId <= 0 || _inventoryCatalogService.GetItem(form.ItemId) is null)
        {
            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Items));
        }

        if (_inventoryCatalogService.SkuExists(form.Sku, form.ItemId))
        {
            ModelState.AddModelError("EditForm.Sku", "SKU นี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Items", BuildItemsModel(editForm: form, activeModal: "edit"));
        }

        _inventoryCatalogService.UpdateItem(form.ItemId, CreateInventoryInput(form));
        TempData["SiteNotice"] = $"อัปเดตสินค้า {form.Name} แล้ว";

        return RedirectToAction(nameof(Items));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AdjustItemStock(int itemId, int quantityAmount = 1, int quantityDirection = 1)
    {
        var existingItem = _inventoryCatalogService.GetItem(itemId);
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

        if (!_inventoryCatalogService.AdjustStock(itemId, quantityDelta))
        {
            return HandleItemStockResult($"สต็อกของ {existingItem.Name} ลดต่ำกว่า 0 ไม่ได้", existingItem, isSuccess: false, statusCode: StatusCodes.Status400BadRequest);
        }

        var updatedItem = _inventoryCatalogService.GetItem(itemId) ?? existingItem;
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

        if (_accountDirectoryService.EmailExists(form.Email))
        {
            ModelState.AddModelError("AddForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(addForm: form, activeModal: "account-add"));
        }

        var createdAccount = _accountDirectoryService.AddAccount(CreateAccountInput(form));
        TempData["SiteNotice"] = $"เพิ่มบัญชี {createdAccount.FullName} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAccount([Bind(Prefix = "EditForm")] AdminAccountEditorViewModel form)
    {
        var currentRoleKey = GetCurrentAdminRoleKey();
        var existingAccount = form.AccountId <= 0 ? null : _accountDirectoryService.GetAccount(form.AccountId);

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

        if (_accountDirectoryService.EmailExists(form.Email, form.AccountId))
        {
            ModelState.AddModelError("EditForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(editForm: form, activeModal: "account-edit"));
        }

        _accountDirectoryService.UpdateAccount(form.AccountId, CreateAccountInput(form));
        TempData["SiteNotice"] = $"อัปเดตบัญชี {form.FullName} แล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CloseAccount(int accountId)
    {
        var existingAccount = _accountDirectoryService.GetAccount(accountId);
        if (accountId <= 0 || existingAccount is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการปิด";
            return RedirectToAction(nameof(Accounts));
        }

        if (!_accountDirectoryService.CloseAccount(accountId))
        {
            TempData["SiteNotice"] = "ไม่สามารถปิดบัญชีนี้ได้";
            return RedirectToAction(nameof(Accounts));
        }

        TempData["SiteNotice"] = $"ปิดบัญชี {existingAccount.FullName} แล้ว";
        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public IActionResult Reports()
    {
        return RedirectToAction(nameof(Accounts));
    }

    [HttpGet]
    public IActionResult Profile()
    {
        return View(BuildProfileModel());
    }

    private static AdminDashboardViewModel BuildDashboardModel()
    {
        return new AdminDashboardViewModel
        {
            DateRangeLabel = "Jan 01 - Jan 28",
            Metrics = BuildDashboardMetrics(),
            DashboardBars =
            [
                new AdminChartPointViewModel { Label = "20", Value = 48 },
                new AdminChartPointViewModel { Label = "22", Value = 72 },
                new AdminChartPointViewModel { Label = "24", Value = 58 },
                new AdminChartPointViewModel { Label = "26", Value = 64 },
                new AdminChartPointViewModel { Label = "28", Value = 52 },
                new AdminChartPointViewModel { Label = "30", Value = 66 },
                new AdminChartPointViewModel { Label = "02", Value = 44 },
                new AdminChartPointViewModel { Label = "04", Value = 78, IsHighlighted = true },
                new AdminChartPointViewModel { Label = "06", Value = 57 },
                new AdminChartPointViewModel { Label = "08", Value = 39 },
                new AdminChartPointViewModel { Label = "10", Value = 54 },
                new AdminChartPointViewModel { Label = "12", Value = 62 }
            ],
            CartRecoveryPercent = 38,
            AbandonedCartCount = 720,
            AbandonedRevenue = 5900,
            DeviceRevenue =
            [
                new AdminDeviceRevenueViewModel { Label = "Desktop", Value = "$830.03", Share = "64.2%", AccentKey = "orange" },
                new AdminDeviceRevenueViewModel { Label = "Mobile", Value = "$755.75", Share = "48.6%", AccentKey = "blue" },
                new AdminDeviceRevenueViewModel { Label = "Tablet", Value = "$550.81", Share = "15.3%", AccentKey = "red" },
                new AdminDeviceRevenueViewModel { Label = "Unknown", Value = "$150.84", Share = "8.6%", AccentKey = "purple" }
            ],
            StoreVisits = 8950,
            Visitors = 1520,
            TrafficLine =
            [
                new AdminChartPointViewModel { Label = "16", Value = 34 },
                new AdminChartPointViewModel { Label = "18", Value = 26 },
                new AdminChartPointViewModel { Label = "20", Value = 24 },
                new AdminChartPointViewModel { Label = "22", Value = 40 },
                new AdminChartPointViewModel { Label = "24", Value = 76, IsHighlighted = true },
                new AdminChartPointViewModel { Label = "26", Value = 52 },
                new AdminChartPointViewModel { Label = "28", Value = 30 },
                new AdminChartPointViewModel { Label = "30", Value = 58 }
            ],
            Bestsellers =
            [
                new AdminBestsellerViewModel { Product = "Deco accessory", Price = "$21.19", Sold = "409", Profit = "$1822.87" },
                new AdminBestsellerViewModel { Product = "Pottery Vase", Price = "$14.18", Sold = "396", Profit = "$8545.25" },
                new AdminBestsellerViewModel { Product = "Rose Holdback", Price = "$18.15", Sold = "243", Profit = "$7287.01" },
                new AdminBestsellerViewModel { Product = "Flowering Cactus", Price = "$74.16", Sold = "636", Profit = "$9325.47" }
            ],
            ForecastCards =
            [
                new AdminForecastCardViewModel { Label = "Revenue", Value = "+24.2%", Delta = "vs last period", PositiveTrend = true, AccentKey = "orange" },
                new AdminForecastCardViewModel { Label = "Net Profit", Value = "-2.5%", Delta = "vs last period", PositiveTrend = false, AccentKey = "red" },
                new AdminForecastCardViewModel { Label = "Orders", Value = "+32.8%", Delta = "vs last period", PositiveTrend = true, AccentKey = "green" },
                new AdminForecastCardViewModel { Label = "Visitors", Value = "+60%", Delta = "vs last period", PositiveTrend = true, AccentKey = "gold" }
            ],
            LatestOrders = BuildDashboardOrders()
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

    private static AdminCustomersViewModel BuildCustomersModel()
    {
        return new AdminCustomersViewModel
        {
            DateRangeLabel = "Jan 01 - Jan 28",
            Metrics = BuildDetailMetrics(),
            CustomerName = "Connie Robertson",
            Email = "victoriasimmmons@2020.com",
            Segment = "VIP Customer",
            SummaryItems =
            [
                new AdminInfoItemViewModel { Label = "Group", Value = "9,520", Detail = "Loyalty score" },
                new AdminInfoItemViewModel { Label = "Location", Value = "Undefined, Minnesota 40 United States.", Detail = "Last seen online" },
                new AdminInfoItemViewModel { Label = "First Order", Value = "September 30, 2019 1:49 PM", Detail = "Acquired from campaign" },
                new AdminInfoItemViewModel { Label = "Latest Orders", Value = "February 14, 2020 7:52 AM", Detail = "High-value checkout" }
            ],
            RevenueLabel = "Revenue",
            RevenueValue = "$2.5M",
            RevenueDelta = "+12%",
            RevenueChart = BuildRevenueChart(),
            Orders =
            [
                new AdminLatestOrderViewModel { OrderId = "#32000200", Product = "Basket with handles", Quantity = "2", Date = "Jan 10, 2020", Revenue = "$253.82", NetProfit = "$60.76", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000201", Product = "Analog Table Clock", Quantity = "1", Date = "Sep 4, 2020", Revenue = "$556.24", NetProfit = "$66.41", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000202", Product = "Flower vase", Quantity = "3", Date = "Aug 30, 2020", Revenue = "$115.26", NetProfit = "$95.66", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000203", Product = "Deco accessory", Quantity = "3", Date = "Aug 29, 2020", Revenue = "$675.51", NetProfit = "$84.80", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000204", Product = "Pottery Vase", Quantity = "4", Date = "Dec 26, 2020", Revenue = "$910.71", NetProfit = "$46.52", Status = "Shipping" },
                new AdminLatestOrderViewModel { OrderId = "#32000205", Product = "Rose Holdback", Quantity = "2", Date = "Apr 27, 2020", Revenue = "$897.90", NetProfit = "$81.54", Status = "Completed" }
            ]
        };
    }

    private AdminProductsViewModel BuildProductsModel()
    {
        var items = _inventoryCatalogService.GetAllItems();
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
        var accounts = _accountDirectoryService.GetAllAccounts();
        var statusOptions = _accountDirectoryService.GetStatuses();
        var addRoleOptions = BuildAddRoleOptions(currentRoleKey);
        var editRoleOptions = BuildEditRoleOptions(currentRoleKey);
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
        AdminItemEditorViewModel? addForm = null,
        AdminItemEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var items = _inventoryCatalogService.GetAllItems();
        var categories = BuildCategories(items, addForm?.Category, editForm?.Category);
        var imageOptions = BuildImageOptions(items, addForm?.ImagePath, editForm?.ImagePath);

        return new AdminItemsPageViewModel
        {
            DateRangeLabel = $"Inventory sync {DateTime.Now:dd MMM yyyy}",
            SummaryItems = BuildInventorySummary(items),
            Items = items.Select(MapInventoryItem).ToList(),
            Categories = categories,
            ImageOptions = imageOptions,
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

    private static AdminProfileViewModel BuildProfileModel()
    {
        return new AdminProfileViewModel
        {
            FullName = "Mad Bakery",
            Role = "Store Administrator",
            Email = "hello@madbakery.com",
            Phone = "+66 98 765 4321",
            Bio = "Oversees bakery storefront, order flow, customer care, and seasonal campaigns in one place.",
            SummaryItems =
            [
                new AdminInfoItemViewModel { Label = "Managed Orders", Value = "920", Detail = "This month" },
                new AdminInfoItemViewModel { Label = "Products Live", Value = "48", Detail = "Across all categories" },
                new AdminInfoItemViewModel { Label = "Campaigns", Value = "12", Detail = "Currently active" },
                new AdminInfoItemViewModel { Label = "Response Rate", Value = "98%", Detail = "Customer messages handled" }
            ],
            PreferenceItems =
            [
                new AdminInfoItemViewModel { Label = "Theme", Value = "Pink & Cream", Detail = "Bakery brand palette" },
                new AdminInfoItemViewModel { Label = "Primary Role", Value = "Owner access", Detail = "Full dashboard visibility" },
                new AdminInfoItemViewModel { Label = "Notifications", Value = "Orders, stock, campaigns", Detail = "Realtime summary" },
                new AdminInfoItemViewModel { Label = "Last Session", Value = "Today, 09:15 AM", Detail = "Bangkok timezone" }
            ]
        };
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildDashboardMetrics()
    {
        return
        [
            new AdminMetricCardViewModel { Label = "Revenue", Value = "$7,825", Delta = "+22%", PositiveTrend = true, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Orders", Value = "920", Delta = "-25%", PositiveTrend = false, AccentKey = "red" },
            new AdminMetricCardViewModel { Label = "Visitors", Value = "15.5K", Delta = "+49%", PositiveTrend = true, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Conversion", Value = "28%", Delta = "+1.9%", PositiveTrend = true, AccentKey = "gold" }
        ];
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildDetailMetrics()
    {
        return
        [
            new AdminMetricCardViewModel { Label = "Revenue", Value = "$75,620", Delta = "+22%", PositiveTrend = true, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Orders Paid", Value = "520", Delta = "+5.7%", PositiveTrend = true, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Refunds", Value = "7,283", Delta = "18%", PositiveTrend = false, AccentKey = "red" },
            new AdminMetricCardViewModel { Label = "Net Profit", Value = "28%", Delta = "+12%", PositiveTrend = true, AccentKey = "gold" }
        ];
    }

    private static IReadOnlyList<AdminChartPointViewModel> BuildRevenueChart()
    {
        return
        [
            new AdminChartPointViewModel { Label = "16", Value = 36 },
            new AdminChartPointViewModel { Label = "18", Value = 42 },
            new AdminChartPointViewModel { Label = "20", Value = 48 },
            new AdminChartPointViewModel { Label = "22", Value = 54 },
            new AdminChartPointViewModel { Label = "24", Value = 77, IsHighlighted = true },
            new AdminChartPointViewModel { Label = "26", Value = 68 },
            new AdminChartPointViewModel { Label = "28", Value = 58 },
            new AdminChartPointViewModel { Label = "30", Value = 66 },
            new AdminChartPointViewModel { Label = "02", Value = 49 }
        ];
    }

    private static IReadOnlyList<AdminLatestOrderViewModel> BuildDashboardOrders()
    {
        return
        [
            new AdminLatestOrderViewModel { Product = "Analog Table Clock", Quantity = "x2", Date = "Feb 5, 2020", Revenue = "$253.82", NetProfit = "$60.76", Status = "Pending" },
            new AdminLatestOrderViewModel { Product = "Basket with handles", Quantity = "x3", Date = "Sep 8, 2020", Revenue = "$556.24", NetProfit = "$66.41", Status = "Shipping" },
            new AdminLatestOrderViewModel { Product = "Flower vase", Quantity = "x3", Date = "Dec 21, 2020", Revenue = "$115.26", NetProfit = "$95.66", Status = "Refund" },
            new AdminLatestOrderViewModel { Product = "Deco accessory", Quantity = "x2", Date = "Aug 13, 2020", Revenue = "$675.51", NetProfit = "$84.80", Status = "Completed" },
            new AdminLatestOrderViewModel { Product = "Pottery Vase", Quantity = "x2", Date = "May 8, 2020", Revenue = "$910.71", NetProfit = "$46.52", Status = "Shipping" },
            new AdminLatestOrderViewModel { Product = "Rose Holdback", Quantity = "x4", Date = "Nov 15, 2020", Revenue = "$897.90", NetProfit = "$81.54", Status = "Completed" },
            new AdminLatestOrderViewModel { Product = "Table Lamp", Quantity = "x4", Date = "Sep 14, 2020", Revenue = "$563.43", NetProfit = "$17.46", Status = "Pending" },
            new AdminLatestOrderViewModel { Product = "Wall Clock", Quantity = "x3", Date = "May 15, 2020", Revenue = "$883.96", NetProfit = "$43.08", Status = "Refund" },
            new AdminLatestOrderViewModel { Product = "Flowering Cactus", Quantity = "x2", Date = "Sep 2, 2020", Revenue = "$162.15", NetProfit = "$86.65", Status = "Completed" },
            new AdminLatestOrderViewModel { Product = "Shell Collection", Quantity = "x4", Date = "Sep 20, 2020", Revenue = "$378.34", NetProfit = "$49.08", Status = "Completed" }
        ];
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

    private static IReadOnlyList<string> BuildAddRoleOptions(string currentRoleKey)
    {
        return AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey)
            ? ["User", "Staff"]
            : ["User"];
    }

    private static IReadOnlyList<string> BuildEditRoleOptions(string currentRoleKey)
    {
        return BuildAddRoleOptions(currentRoleKey);
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
            PriceLabel = $"{item.Price:0.##} ฿",
            StockQuantity = item.StockQuantity,
            ReorderLevel = item.ReorderLevel,
            StatusLabel = status.Label,
            StatusKey = status.Key,
            UpdatedAtLabel = item.UpdatedAt.ToString("dd MMM yyyy, HH:mm"),
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
