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
    public IActionResult Index()
    {
        return View(BuildDashboardModel());
    }

    private AdminDashboardViewModel BuildDashboardModel()
    {
        var orders = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Include(order => order.OrderPromotions)
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
}
