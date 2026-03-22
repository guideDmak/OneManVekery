using Microsoft.AspNetCore.Mvc;
using OneManVekery.Services;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AdminController : Controller
{
    private readonly IAccountDirectoryService _accountDirectoryService;
    private readonly IInventoryCatalogService _inventoryCatalogService;

    public AdminController(
        IAccountDirectoryService accountDirectoryService,
        IInventoryCatalogService inventoryCatalogService)
    {
        _accountDirectoryService = accountDirectoryService;
        _inventoryCatalogService = inventoryCatalogService;
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
        if (form.ItemId == Guid.Empty || _inventoryCatalogService.GetItem(form.ItemId) is null)
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
    public IActionResult AdjustItemStock(Guid itemId, int quantityDelta)
    {
        var existingItem = _inventoryCatalogService.GetItem(itemId);
        if (itemId == Guid.Empty || existingItem is null)
        {
            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการปรับสต็อก";
            return RedirectToAction(nameof(Items));
        }

        if (quantityDelta == 0)
        {
            TempData["SiteNotice"] = "กรุณาระบุจำนวนสต็อกที่ต้องการเปลี่ยน";
            return RedirectToAction(nameof(Items));
        }

        if (!_inventoryCatalogService.AdjustStock(itemId, quantityDelta))
        {
            TempData["SiteNotice"] = $"สต็อกของ {existingItem.Name} ลดต่ำกว่า 0 ไม่ได้";
            return RedirectToAction(nameof(Items));
        }

        var actionLabel = quantityDelta > 0
            ? $"เพิ่มสต็อก {quantityDelta}"
            : $"ลดสต็อก {Math.Abs(quantityDelta)}";

        TempData["SiteNotice"] = $"{actionLabel} สำหรับ {existingItem.Name} แล้ว";

        return RedirectToAction(nameof(Items));
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
        if (form.AccountId == Guid.Empty || _accountDirectoryService.GetAccount(form.AccountId) is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Accounts));
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
    public IActionResult CloseAccount(Guid accountId)
    {
        var existingAccount = _accountDirectoryService.GetAccount(accountId);
        if (accountId == Guid.Empty || existingAccount is null)
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

    private static AdminOrdersViewModel BuildOrdersModel()
    {
        return new AdminOrdersViewModel
        {
            DateRangeLabel = "Jan 01 - Jan 28",
            Metrics = BuildDashboardMetrics(),
            UpdateLabel = "Orders Update",
            UpdateValue = "$2.5M",
            UpdateDelta = "+12%",
            UpdateChart =
            [
                new AdminChartPointViewModel { Label = "16", Value = 42 },
                new AdminChartPointViewModel { Label = "18", Value = 48 },
                new AdminChartPointViewModel { Label = "20", Value = 52 },
                new AdminChartPointViewModel { Label = "22", Value = 61 },
                new AdminChartPointViewModel { Label = "24", Value = 82, IsHighlighted = true },
                new AdminChartPointViewModel { Label = "26", Value = 76 },
                new AdminChartPointViewModel { Label = "28", Value = 68 },
                new AdminChartPointViewModel { Label = "30", Value = 58 },
                new AdminChartPointViewModel { Label = "02", Value = 46 },
                new AdminChartPointViewModel { Label = "04", Value = 72 },
                new AdminChartPointViewModel { Label = "06", Value = 55 },
                new AdminChartPointViewModel { Label = "08", Value = 63 },
                new AdminChartPointViewModel { Label = "10", Value = 74 }
            ],
            FulfillmentSummary =
            [
                new AdminInfoItemViewModel { Label = "Completed", Value = "328", Detail = "Paid on time", AccentKey = "green" },
                new AdminInfoItemViewModel { Label = "Shipping", Value = "184", Detail = "In transit", AccentKey = "blue" },
                new AdminInfoItemViewModel { Label = "Pending", Value = "92", Detail = "Need confirm", AccentKey = "gold" },
                new AdminInfoItemViewModel { Label = "Refund", Value = "16", Detail = "Need review", AccentKey = "red" }
            ],
            Orders =
            [
                new AdminLatestOrderViewModel { OrderId = "#5302002", Product = "Basket with handles", SecondaryText = "Grocery", Customer = "Ronald Jones", Quantity = "2 Items", Date = "Jan 10, 2020", Revenue = "$253.82", NetProfit = "$60.76", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#5302003", Product = "Pottery Vase", SecondaryText = "Decor", Customer = "Jacob Mckinney", Quantity = "5 Items", Date = "Sep 4, 2020", Revenue = "$556.24", NetProfit = "$66.41", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#5302004", Product = "Rose Holdback", SecondaryText = "Decor", Customer = "Randall Murphy", Quantity = "7 Items", Date = "Aug 30, 2020", Revenue = "$115.26", NetProfit = "$95.66", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#5302005", Product = "Analog Table Clock", SecondaryText = "Home", Customer = "Philip Webb", Quantity = "3 Items", Date = "Aug 29, 2020", Revenue = "$675.51", NetProfit = "$84.80", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#5302006", Product = "Flower vase", SecondaryText = "Decor", Customer = "Arthur Bell", Quantity = "4 Items", Date = "Dec 26, 2020", Revenue = "$910.71", NetProfit = "$46.52", Status = "Shipping" },
                new AdminLatestOrderViewModel { OrderId = "#5302007", Product = "Table Lamp", SecondaryText = "Lighting", Customer = "Gregory Nguyen", Quantity = "5 Items", Date = "Apr 27, 2020", Revenue = "$897.90", NetProfit = "$81.54", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#5302008", Product = "Wall Clock", SecondaryText = "Home", Customer = "Soham Henry", Quantity = "3 Items", Date = "May 5, 2020", Revenue = "$563.43", NetProfit = "$17.46", Status = "Pending" },
                new AdminLatestOrderViewModel { OrderId = "#5302009", Product = "Flowering Cactus", SecondaryText = "Garden", Customer = "Jenny Hawkins", Quantity = "5 Items", Date = "Oct 15, 2020", Revenue = "$883.96", NetProfit = "$43.08", Status = "Refund" },
                new AdminLatestOrderViewModel { OrderId = "#5302010", Product = "Shell Collection", SecondaryText = "Decor", Customer = "Diane Cooper", Quantity = "4 Items", Date = "Jul 12, 2020", Revenue = "$162.15", NetProfit = "$86.65", Status = "Pending" },
                new AdminLatestOrderViewModel { OrderId = "#5302012", Product = "Deco accessory", SecondaryText = "Decor", Customer = "Max Williamson", Quantity = "2 Items", Date = "Jun 28, 2020", Revenue = "$378.34", NetProfit = "$49.08", Status = "Completed" }
            ]
        };
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

    private static AdminProductsViewModel BuildProductsModel()
    {
        return new AdminProductsViewModel
        {
            DateRangeLabel = "Jan 01 - Jan 28",
            Metrics = BuildDetailMetrics(),
            ProductName = "Basket with handles",
            Category = "General",
            ProductCaption = "Soft launch highlight",
            SummaryItems =
            [
                new AdminInfoItemViewModel { Label = "In Stock", Value = "9,520", Detail = "Ready to ship" },
                new AdminInfoItemViewModel { Label = "Colors", Value = "Black, White, Blue, Yellow", Detail = "4 variants" },
                new AdminInfoItemViewModel { Label = "Start Time", Value = "September 30, 2018", Detail = "Published product" },
                new AdminInfoItemViewModel { Label = "Life Time Sells", Value = "40,02,030", Detail = "Across all channels" }
            ],
            RevenueLabel = "Revenue",
            RevenueValue = "$2.5M",
            RevenueDelta = "+12%",
            RevenueChart = BuildRevenueChart(),
            Orders =
            [
                new AdminLatestOrderViewModel { OrderId = "#32000200", Customer = "Priscilla Warren", Quantity = "2", Date = "Jan 10, 2020", Revenue = "$253.82", NetProfit = "$60.76", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000201", Customer = "Tanya Wilson", Quantity = "1", Date = "Sep 4, 2020", Revenue = "$556.24", NetProfit = "$66.41", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000202", Customer = "Theresa Alexander", Quantity = "3", Date = "Aug 30, 2020", Revenue = "$115.26", NetProfit = "$95.66", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000203", Customer = "Mitchell Lane", Quantity = "3", Date = "Aug 29, 2020", Revenue = "$675.51", NetProfit = "$84.80", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000204", Customer = "Arlene Richards", Quantity = "4", Date = "Dec 26, 2020", Revenue = "$910.71", NetProfit = "$46.52", Status = "Shipping" },
                new AdminLatestOrderViewModel { OrderId = "#32000205", Customer = "Angel Howard", Quantity = "2", Date = "Apr 27, 2020", Revenue = "$897.90", NetProfit = "$81.54", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000206", Customer = "Greg Fox", Quantity = "1", Date = "May 5, 2020", Revenue = "$563.43", NetProfit = "$17.46", Status = "Pending" },
                new AdminLatestOrderViewModel { OrderId = "#32000207", Customer = "Devon Bell", Quantity = "4", Date = "Oct 15, 2020", Revenue = "$883.96", NetProfit = "$43.08", Status = "Refund" },
                new AdminLatestOrderViewModel { OrderId = "#32000208", Customer = "Shane Henry", Quantity = "3", Date = "Jul 12, 2020", Revenue = "$162.15", NetProfit = "$86.65", Status = "Completed" },
                new AdminLatestOrderViewModel { OrderId = "#32000209", Customer = "Stella Webb", Quantity = "2", Date = "Jun 28, 2020", Revenue = "$378.34", NetProfit = "$49.08", Status = "Completed" }
            ]
        };
    }

    private AdminAccountsViewModel BuildAccountsModel(
        AdminAccountEditorViewModel? addForm = null,
        AdminAccountEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var accounts = _accountDirectoryService.GetAllAccounts();
        var roles = _accountDirectoryService.GetRoles();
        var statusOptions = _accountDirectoryService.GetStatuses();

        return new AdminAccountsViewModel
        {
            DateRangeLabel = $"Account sync {DateTime.Now:dd MMM yyyy}",
            SummaryItems = BuildAccountSummary(accounts),
            Accounts = accounts.Select(MapAccount).ToList(),
            Roles = roles,
            StatusOptions = statusOptions,
            AddForm = addForm ?? new AdminAccountEditorViewModel
            {
                Role = "Staff",
                Status = "Active"
            },
            EditForm = editForm ?? new AdminAccountEditorViewModel
            {
                Role = "Staff",
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

    private static AccountInput CreateAccountInput(AdminAccountEditorViewModel form)
    {
        return new AccountInput
        {
            FullName = form.FullName,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            Role = form.Role,
            Status = form.Status,
            Notes = form.Notes
        };
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
