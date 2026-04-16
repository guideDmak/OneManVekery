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

        if (!ModelState.IsValid)
        {
            return View("Orders", BuildOrdersModel(editForm: form, activeModal: "order-edit"));
        }

        var normalizedOrderStatus = NormalizeOrderStatus(form.OrderStatus);
        var normalizedPaymentStatus = NormalizePaymentStatus(form.PaymentStatus);

        order.OrderStatus = normalizedOrderStatus;
        order.PaymentStatus = normalizedPaymentStatus;
        order.Note = string.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim();

        _dbContext.SaveChanges();

        TempData["SiteNotice"] = $"อัปเดตสถานะออเดอร์ {order.OrderNo} แล้ว";
        return RedirectToAction(nameof(Orders));
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
            SubtotalLabel = $"{order.Subtotal:0.##} ฿",
            DeliveryFeeLabel = $"{order.DeliveryFee:0.##} ฿",
            DiscountAmountLabel = order.DiscountAmount > 0 ? $"-{order.DiscountAmount:0.##} ฿" : "0 ฿",
            ShippingDiscountAmountLabel = order.ShippingDiscountAmount > 0 ? $"-{order.ShippingDiscountAmount:0.##} ฿" : "0 ฿",
            DiscountCode = order.DiscountCode ?? string.Empty,
            PointsEarnedLabel = $"+{order.PointsEarned:0} P",
            PointsRedeemedLabel = order.PointsRedeemed > 0 ? $"-{order.PointsRedeemed:0} P" : "0 P",
            PaymentMethodLabel = $"{FormatPaymentMethod(order.PaymentMethod)} / {paymentStatus.Label}",
            PaymentStatus = paymentStatus.Label,
            PaymentStatusKey = paymentStatus.Key,
            OrderStatus = orderStatus.Label,
            OrderStatusKey = orderStatus.Key,
            Phone = order.Phone,
            Address = order.Address,
            Note = order.Note ?? string.Empty,
            Items = order.OrderItems
                .OrderBy(item => item.Id)
                .Select(item => new AdminOrderItemRecordViewModel
                {
                    ProductName = item.ProductName,
                    Quantity = item.Qty,
                    UnitPriceLabel = $"{item.Price:0.##} ฿",
                    LineTotalLabel = $"{item.LineTotal:0.##} ฿"
                })
                .ToList(),
            Benefits = order.OrderPromotions
                .OrderBy(item => item.Id)
                .Select(MapOrderBenefit)
                .ToList()
        };
    }

    private static AdminOrderBenefitRecordViewModel MapOrderBenefit(OrderPromotion benefit)
    {
        var valueParts = new List<string>();

        if (benefit.DiscountAmount > 0)
        {
            valueParts.Add($"-{benefit.DiscountAmount:0.##} ฿");
        }

        if (benefit.ShippingDiscountAmount > 0)
        {
            valueParts.Add($"ส่งฟรี {benefit.ShippingDiscountAmount:0.##} ฿");
        }

        if (benefit.PointsEarned > 0)
        {
            valueParts.Add($"+{benefit.PointsEarned:0} P");
        }

        if (benefit.PointsRedeemed > 0)
        {
            valueParts.Add($"-{benefit.PointsRedeemed:0} P");
        }

        return new AdminOrderBenefitRecordViewModel
        {
            Title = benefit.PromotionTitle,
            Description = benefit.Note ?? string.Empty,
            ValueLabel = valueParts.Count == 0 ? "-" : string.Join(" | ", valueParts)
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
}
