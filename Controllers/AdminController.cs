using Microsoft.AspNetCore.Mvc;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AdminController : Controller
{
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
    public IActionResult Reports()
    {
        return View(BuildReportsModel());
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

    private static AdminReportsViewModel BuildReportsModel()
    {
        return new AdminReportsViewModel
        {
            Sections =
            [
                new AdminCampaignSectionViewModel
                {
                    StepNumber = "1",
                    Title = "Campaign info",
                    Items =
                    [
                        new AdminInfoItemViewModel { Label = "Campaign name", Value = "ex. Birthday Offer" },
                        new AdminInfoItemViewModel { Label = "Brands/Outlets", Value = "Hard Rock cafe, Koregaon park", Detail = "+3 more" },
                        new AdminInfoItemViewModel { Label = "Channels", Value = "Email, SMS" }
                    ]
                },
                new AdminCampaignSectionViewModel
                {
                    StepNumber = "2",
                    Title = "Audience",
                    Items =
                    [
                        new AdminInfoItemViewModel { Label = "Target Customers", Value = "5,000" },
                        new AdminInfoItemViewModel { Label = "Email Only", Value = "2,750" },
                        new AdminInfoItemViewModel { Label = "Sms Only", Value = "2,250" },
                        new AdminInfoItemViewModel { Label = "Customers", Value = "All Customers" }
                    ]
                },
                new AdminCampaignSectionViewModel
                {
                    StepNumber = "3",
                    Title = "Time manage",
                    Items =
                    [
                        new AdminInfoItemViewModel { Label = "Check", Value = "Every hour" },
                        new AdminInfoItemViewModel { Label = "Time range", Value = "Today" },
                        new AdminInfoItemViewModel { Label = "Run length", Value = "Active days" },
                        new AdminInfoItemViewModel { Label = "Schedule", Value = "12 Feb 2020 - 20 Jun 2020" }
                    ]
                }
            ],
            Rules =
            [
                new AdminCampaignRuleViewModel
                {
                    Metric = "Spend",
                    IntroLabel = "If",
                    Comparison = "=",
                    Threshold = "$100",
                    MidLabel = "And",
                    ActionLabel = "Increase budget",
                    ActionValue = "$60"
                },
                new AdminCampaignRuleViewModel
                {
                    Metric = "Spend",
                    IntroLabel = "Nested condition",
                    Comparison = "=",
                    Threshold = "$100",
                    MidLabel = "Than",
                    ActionLabel = "New Rule",
                    ActionValue = "Ready"
                }
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
}
