using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class HomeController : Controller
{
    private const int DefaultReorderLevel = 10;
    private const string CartSessionKey = "one-man-vekery-cart";
    private const string OrderSessionKey = "one-man-vekery-orders";
    private readonly OneManVekeryDBContext _dbContext;
    private readonly StorefrontContentOptions _storefrontContent;

    public HomeController(
        OneManVekeryDBContext dbContext,
        IOptions<StorefrontContentOptions> storefrontContentOptions)
    {
        _dbContext = dbContext;
        _storefrontContent = storefrontContentOptions.Value;
    }

    public IActionResult Index()
    {
        var products = GetProducts();

        return View(new HomeIndexViewModel
        {
            Categories = BuildCategoryCards(products),
            Products = products
        });
    }

    [HttpGet]
    public IActionResult Shop()
    {
        var products = GetProducts();

        return View(new ShopPageViewModel
        {
            Products = products,
            Categories = products
                .Select(product => product.Category)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList()
        });
    }

    [HttpGet]
    public IActionResult Cart()
    {
        return View(BuildCartPageModel());
    }

    [HttpGet]
    public IActionResult OrderStatus(string orderNumber)
    {
        var order = GetOrder(orderNumber);
        if (order is null)
        {
            TempData["SiteNotice"] = "ไม่พบออเดอร์ที่ต้องการ";
            return RedirectToAction(nameof(Shop));
        }

        return View(BuildOrderStatusPageModel(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(string productId)
    {
        if (AddItemToCart(productId))
        {
            TempData["SiteNotice"] = "เพิ่มสินค้าเข้าตะกร้าแล้ว";
        }
        else
        {
            TempData["SiteNotice"] = "ไม่สามารถเพิ่มสินค้านี้เข้าตะกร้าได้";
        }

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangeCartQuantity(string productId, int delta)
    {
        if (!ChangeCartItemQuantity(productId, delta))
        {
            TempData["SiteNotice"] = "ไม่สามารถอัปเดตจำนวนสินค้าได้";
        }

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(string productId)
    {
        if (RemoveCartItem(productId))
        {
            TempData["SiteNotice"] = "ลบสินค้าออกจากตะกร้าแล้ว";
        }
        else
        {
            TempData["SiteNotice"] = "ไม่พบสินค้าที่ต้องการลบ";
        }

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Checkout(CartCheckoutViewModel checkout)
    {
        var cartItems = GetCartItems();
        if (cartItems.Count == 0)
        {
            TempData["SiteNotice"] = "ตะกร้าสินค้ายังว่างอยู่";
            return RedirectToAction(nameof(Cart));
        }

        if (!ModelState.IsValid)
        {
            return View("Cart", BuildCartPageModel(checkout));
        }

        var deliveryFee = CalculateDeliveryFee(cartItems);
        var paymentMethodLabel = ResolvePaymentLabel(checkout.PaymentMethod);
        var order = CreateOrder(
            new CartCheckoutSnapshot
            {
                CustomerName = checkout.CustomerName,
                PhoneNumber = checkout.PhoneNumber,
                DeliveryAddress = checkout.DeliveryAddress,
                PaymentMethodCode = checkout.PaymentMethod,
                Notes = checkout.Notes
            },
            cartItems,
            deliveryFee,
            paymentMethodLabel);

        ClearCart();

        return RedirectToAction(nameof(OrderStatus), new { orderNumber = order.OrderNumber });
    }

    [HttpGet]
    public IActionResult About()
    {
        var aboutContent = _storefrontContent.About ?? new StorefrontAboutOptions();

        return View(new AboutPageViewModel
        {
            StoryTitle = aboutContent.StoryTitle,
            StoryParagraphs = aboutContent.StoryParagraphs,
            Values = aboutContent.Values
                .Select(item => new ServiceFeatureViewModel
                {
                    IconText = item.IconText,
                    Title = item.Title,
                    Description = item.Description
                })
                .ToList()
        });
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View(BuildContactPageModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildContactPageModel(form));
        }

        _dbContext.ContactMessages.Add(new ContactMessage
        {
            Name = form.Name.Trim(),
            Email = form.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(form.PhoneNumber) ? null : form.PhoneNumber.Trim(),
            Subject = string.IsNullOrWhiteSpace(form.Subject) ? null : form.Subject.Trim(),
            Message = form.Message.Trim(),
            Status = "new",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();

        TempData["SiteNotice"] = "ส่งข้อความเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private ContactPageViewModel BuildContactPageModel(ContactFormViewModel? form = null)
    {
        var contactContent = _storefrontContent.Contact ?? new StorefrontContactOptions();

        return new ContactPageViewModel
        {
            Form = form ?? new ContactFormViewModel(),
            HeadingTitle = contactContent.HeadingTitle,
            ContactCards = contactContent.Cards
                .Select(card => new ContactInfoCardViewModel
                {
                    IconText = card.IconText,
                    Title = card.Title,
                    LineOne = card.LineOne,
                    LineTwo = card.LineTwo
                })
                .ToList()
        };
    }

    private CartPageViewModel BuildCartPageModel(CartCheckoutViewModel? checkout = null)
    {
        var cartItems = GetCartItems();
        var subtotal = cartItems.Sum(item => item.LineTotal);
        var deliveryFee = CalculateDeliveryFee(cartItems);

        return new CartPageViewModel
        {
            Items = cartItems.Select(item => new CartLineViewModel
            {
                ProductId = item.ProductId,
                Name = item.Name,
                Category = item.Category,
                Description = item.Description,
                ImagePath = item.ImagePath,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                IsSoldOut = item.IsSoldOut,
                UnitPriceLabel = $"{item.UnitPrice:0.##} ฿",
                LineTotalLabel = $"{item.LineTotal:0.##} ฿"
            }).ToList(),
            PaymentOptions = BuildPaymentOptions(),
            Checkout = checkout ?? new CartCheckoutViewModel
            {
                PaymentMethod = "promptpay"
            },
            ItemCount = cartItems.Sum(item => item.Quantity),
            Subtotal = subtotal,
            DeliveryFee = deliveryFee
        };
    }

    private OrderStatusPageViewModel BuildOrderStatusPageModel(OrderReceiptRecord order)
    {
        var currentStatus = ResolveOrderStatus(order.CurrentStatusCode);

        return new OrderStatusPageViewModel
        {
            OrderNumber = order.OrderNumber,
            CreatedAt = order.CreatedAt,
            CustomerName = order.CustomerName,
            PhoneNumber = order.PhoneNumber,
            DeliveryAddress = order.DeliveryAddress,
            PaymentMethodLabel = order.PaymentMethodLabel,
            Notes = order.Notes,
            CurrentStatusLabel = currentStatus.Title,
            CurrentStatusDescription = currentStatus.Description,
            Items = order.Items.Select(item => new OrderReceiptLineViewModel
            {
                Name = item.Name,
                Category = item.Category,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList(),
            StatusSteps = BuildOrderProgressSteps(order.CurrentStatusCode),
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee
        };
    }

    private IReadOnlyList<ProductCardViewModel> GetProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToList()
            .Select(MapProduct)
            .ToList();
    }

    private ProductCardViewModel? GetProductById(string productId)
    {
        if (!int.TryParse(productId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue))
        {
            return null;
        }

        var product = _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefault(item => item.Id == productIdValue && item.IsActive);

        return product is null ? null : MapProduct(product);
    }

    private IReadOnlyList<CartLineRecord> GetCartItems()
    {
        var catalog = GetProducts().ToDictionary(product => product.ProductId, StringComparer.OrdinalIgnoreCase);

        return ReadCartItems()
            .Where(item => catalog.ContainsKey(item.ProductId))
            .Select(item =>
            {
                var product = catalog[item.ProductId];

                return new CartLineRecord
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Category = product.Category,
                    Description = product.Description,
                    ImagePath = product.ImagePath,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity,
                    IsSoldOut = product.IsSoldOut
                };
            })
            .ToList();
    }

    private bool AddItemToCart(string productId, int quantity = 1)
    {
        var product = GetProductById(productId);
        if (product is null || product.IsSoldOut || quantity <= 0)
        {
            return false;
        }

        var items = ReadCartItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            items[index] = items[index] with { Quantity = Math.Min(items[index].Quantity + quantity, 99) };
        }
        else
        {
            items.Add(new CartSessionItem
            {
                ProductId = productId,
                Quantity = Math.Min(quantity, 99)
            });
        }

        WriteCartItems(items);
        return true;
    }

    private bool ChangeCartItemQuantity(string productId, int delta)
    {
        var currentItem = ReadCartItems().FirstOrDefault(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (currentItem is null)
        {
            return false;
        }

        return UpdateCartItemQuantity(productId, currentItem.Quantity + delta);
    }

    private bool RemoveCartItem(string productId)
    {
        var items = ReadCartItems();
        var removedCount = items.RemoveAll(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (removedCount == 0)
        {
            return false;
        }

        WriteCartItems(items);
        return true;
    }

    private void ClearCart()
    {
        HttpContext.Session.Remove(CartSessionKey);
    }

    private OrderReceiptRecord CreateOrder(
        CartCheckoutSnapshot checkout,
        IReadOnlyList<CartLineRecord> items,
        decimal deliveryFee,
        string paymentMethodLabel)
    {
        var timestamp = DateTimeOffset.Now;
        var orders = ReadOrders();
        var subtotal = items.Sum(item => item.LineTotal);

        var order = new OrderReceiptRecord
        {
            OrderNumber = $"OVK-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{Random.Shared.Next(100, 999)}",
            CreatedAt = timestamp,
            CustomerName = checkout.CustomerName,
            PhoneNumber = checkout.PhoneNumber,
            DeliveryAddress = checkout.DeliveryAddress,
            PaymentMethodCode = checkout.PaymentMethodCode,
            PaymentMethodLabel = paymentMethodLabel,
            Notes = checkout.Notes,
            CurrentStatusCode = "paid",
            Items = items.Select(item => new OrderReceiptLineRecord
            {
                Name = item.Name,
                Category = item.Category,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList(),
            Subtotal = subtotal,
            DeliveryFee = deliveryFee
        };

        orders.Insert(0, order);
        WriteOrders(orders);

        return order;
    }

    private OrderReceiptRecord? GetOrder(string orderNumber)
    {
        return ReadOrders()
            .FirstOrDefault(order => string.Equals(order.OrderNumber, orderNumber, StringComparison.OrdinalIgnoreCase));
    }

    private List<CartSessionItem> ReadCartItems()
    {
        var raw = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CartSessionItem>>(raw) ?? [];
    }

    private void WriteCartItems(List<CartSessionItem> items)
    {
        if (items.Count == 0)
        {
            HttpContext.Session.Remove(CartSessionKey);
            return;
        }

        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(items));
    }

    private bool UpdateCartItemQuantity(string productId, int quantity)
    {
        var items = ReadCartItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        if (quantity <= 0)
        {
            items.RemoveAt(index);
        }
        else
        {
            items[index] = items[index] with { Quantity = Math.Min(quantity, 99) };
        }

        WriteCartItems(items);
        return true;
    }

    private List<OrderReceiptRecord> ReadOrders()
    {
        var raw = HttpContext.Session.GetString(OrderSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<OrderReceiptRecord>>(raw) ?? [];
    }

    private void WriteOrders(List<OrderReceiptRecord> orders)
    {
        HttpContext.Session.SetString(OrderSessionKey, JsonSerializer.Serialize(orders));
    }

    private static ProductCardViewModel MapProduct(Product product)
    {
        var meta = ParseProductMeta(product.Description);
        var category = string.IsNullOrWhiteSpace(product.Category?.Name) ? "Bakery" : product.Category!.Name;
        var themeKey = ResolveThemeKey(category, product.Name, product.ImageUrl);
        var isSoldOut = product.StockQty <= 0;

        return new ProductCardViewModel
        {
            ProductId = product.Id.ToString(CultureInfo.InvariantCulture),
            Name = product.Name,
            Category = category,
            Description = ResolveDescription(product.Description, meta),
            Price = product.Price,
            Badge = isSoldOut
                ? "หมดแล้ว"
                : product.StockQty <= meta.ReorderLevel
                    ? "ใกล้หมด"
                    : string.Empty,
            ThemeKey = themeKey,
            ImagePath = NormalizeProductImagePath(product.ImageUrl, themeKey),
            IsSoldOut = isSoldOut
        };
    }

    private static string ResolveDescription(string? description, ProductMeta meta)
    {
        if (!string.IsNullOrWhiteSpace(meta.Tagline))
        {
            return meta.Tagline;
        }

        if (!string.IsNullOrWhiteSpace(meta.Notes))
        {
            return meta.Notes;
        }

        return string.IsNullOrWhiteSpace(description) || LooksLikeJson(description)
            ? "สดใหม่จากครัวของร้านในทุกออเดอร์"
            : description.Trim();
    }

    private static ProductMeta ParseProductMeta(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new ProductMeta(string.Empty, string.Empty, DefaultReorderLevel);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ProductMetaStorage>(description);
            if (payload is not null)
            {
                return new ProductMeta(
                    payload.Tagline?.Trim() ?? string.Empty,
                    payload.Notes?.Trim() ?? string.Empty,
                    payload.ReorderLevel < 0 ? DefaultReorderLevel : payload.ReorderLevel);
            }
        }
        catch (JsonException)
        {
        }

        return new ProductMeta(string.Empty, string.Empty, DefaultReorderLevel);
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static string ResolveThemeKey(string category, string name, string? imagePath)
    {
        var source = $"{category} {name} {imagePath}".ToLowerInvariant();

        if (source.Contains("macaron"))
        {
            return "macaron";
        }

        if (source.Contains("berry") || source.Contains("blue") || source.Contains("cherry"))
        {
            return "berry";
        }

        if (source.Contains("milk"))
        {
            return "milk";
        }

        if (source.Contains("cream") || source.Contains("choux") || source.Contains("eclair"))
        {
            return "cream";
        }

        if (source.Contains("cake") || source.Contains("cheese"))
        {
            return "cake";
        }

        return "gold";
    }

    private static string NormalizeProductImagePath(string? imagePath, string themeKey)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return themeKey switch
            {
                "macaron" => "/images/theme-macaron.svg",
                "berry" => "/images/theme-berry.svg",
                "milk" => "/images/theme-milk.svg",
                "cream" => "/images/theme-cream.svg",
                "cake" => "/images/theme-cake.svg",
                _ => "/images/theme-gold.svg"
            };
        }

        var normalized = imagePath.Trim();
        return normalized.StartsWith("~/", StringComparison.Ordinal)
            ? "/" + normalized[2..]
            : normalized;
    }

    private sealed class ProductMetaStorage
    {
        public string? Tagline { get; set; }

        public string? Notes { get; set; }

        public int ReorderLevel { get; set; } = DefaultReorderLevel;
    }

    private sealed record ProductMeta(string Tagline, string Notes, int ReorderLevel);

    private IReadOnlyList<PaymentOptionViewModel> BuildPaymentOptions()
    {
        return _storefrontContent.PaymentOptions
            .Select(option => new PaymentOptionViewModel
            {
                Code = option.Code,
                Label = option.Label,
                Description = option.Description
            })
            .ToList();
    }

    private string ResolvePaymentLabel(string paymentMethod)
    {
        return BuildPaymentOptions()
            .FirstOrDefault(option => option.Code == paymentMethod)?.Label
            ?? "วิธีที่เลือก";
    }

    private static decimal CalculateDeliveryFee(IReadOnlyList<CartLineRecord> cartItems)
    {
        if (cartItems.Count == 0)
        {
            return 0;
        }

        var subtotal = cartItems.Sum(item => item.LineTotal);
        return subtotal >= 300 ? 0 : 45;
    }

    private IReadOnlyList<OrderProgressStepViewModel> BuildOrderProgressSteps(string currentStatusCode)
    {
        var steps = _storefrontContent.OrderStatusSteps
            .Select(step => (step.Code, step.Title, step.Description, step.Marker))
            .ToArray();

        var currentIndex = Array.FindIndex(steps, step => step.Item1 == currentStatusCode);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        return steps.Select((step, index) => new OrderProgressStepViewModel
        {
            Title = step.Item2,
            Description = step.Item3,
            Marker = step.Item4,
            State = index < currentIndex
                ? "complete"
                : index == currentIndex
                    ? "current"
                    : "pending"
        }).ToList();
    }

    private (string Title, string Description) ResolveOrderStatus(string currentStatusCode)
    {
        var status = _storefrontContent.OrderStatusSteps
            .FirstOrDefault(step => string.Equals(step.Code, currentStatusCode, StringComparison.OrdinalIgnoreCase))
            ?? _storefrontContent.OrderStatusSteps.FirstOrDefault();

        return status is null
            ? ("สถานะคำสั่งซื้อ", "ระบบกำลังอัปเดตสถานะล่าสุดของออเดอร์นี้")
            : (status.Title, status.CurrentDescription);
    }

    private static IReadOnlyList<CategoryCardViewModel> BuildCategoryCards(IReadOnlyList<ProductCardViewModel> products)
    {
        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Category))
            .GroupBy(product => product.Category, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var preview = group.FirstOrDefault(product => !string.IsNullOrWhiteSpace(product.ImagePath)) ?? group.First();

                return new CategoryCardViewModel
                {
                    Title = group.First().Category,
                    Subtitle = $"{group.Count()} รายการที่พร้อมแสดงบนหน้าร้าน",
                    ThemeKey = preview.ThemeKey,
                    ImagePath = preview.ImagePath
                };
            })
            .ToList();
    }

}
