using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.Services;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class HomeController : Controller
{
    private readonly IStoreCatalogService _storeCatalogService;
    private readonly IStoreCartService _storeCartService;
    private readonly IStoreOrderService _storeOrderService;
    private readonly OneManVekeryDBContext _dbContext;
    private readonly StorefrontContentOptions _storefrontContent;

    public HomeController(
        IStoreCatalogService storeCatalogService,
        IStoreCartService storeCartService,
        IStoreOrderService storeOrderService,
        OneManVekeryDBContext dbContext,
        IOptions<StorefrontContentOptions> storefrontContentOptions)
    {
        _storeCatalogService = storeCatalogService;
        _storeCartService = storeCartService;
        _storeOrderService = storeOrderService;
        _dbContext = dbContext;
        _storefrontContent = storefrontContentOptions.Value;
    }

    public IActionResult Index()
    {
        var products = _storeCatalogService.GetProducts();

        return View(new HomeIndexViewModel
        {
            Categories = BuildCategoryCards(products),
            Products = products,
            Inspirations = BuildInspirationCards(products),
            Features = BuildStoreFeatures()
        });
    }

    [HttpGet]
    public IActionResult Shop()
    {
        var products = _storeCatalogService.GetProducts();

        return View(new ShopPageViewModel
        {
            Products = products,
            Categories = products
                .Select(product => product.Category)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList(),
            Features = BuildStoreFeatures()
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
        var order = _storeOrderService.GetOrder(orderNumber);
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
        if (_storeCartService.AddItem(productId))
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
        if (!_storeCartService.ChangeQuantity(productId, delta))
        {
            TempData["SiteNotice"] = "ไม่สามารถอัปเดตจำนวนสินค้าได้";
        }

        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(string productId)
    {
        if (_storeCartService.RemoveItem(productId))
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
        var cartItems = _storeCartService.GetItems();
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
        var order = _storeOrderService.CreateOrder(
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

        _storeCartService.Clear();

        return RedirectToAction(nameof(OrderStatus), new { orderNumber = order.OrderNumber });
    }

    [HttpGet]
    public IActionResult About()
    {
        var aboutContent = _storefrontContent.About ?? new StorefrontAboutOptions();
        var activeProducts = _dbContext.Products.AsNoTracking().Count(product => product.IsActive);
        var totalOrders = _dbContext.Orders.AsNoTracking().Count();
        var totalCustomers = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Count(user => user.Role.RoleKey == "user");

        return View(new AboutPageViewModel
        {
            StoryTitle = aboutContent.StoryTitle,
            StoryParagraphs = aboutContent.StoryParagraphs,
            Quote = aboutContent.Quote,
            QuoteCaption = aboutContent.QuoteCaption,
            Stats =
            [
                new AboutStatViewModel { Value = totalOrders.ToString("N0"), Label = "ออเดอร์ในระบบ" },
                new AboutStatViewModel { Value = activeProducts.ToString("N0"), Label = "สินค้าที่เปิดขายอยู่" },
                new AboutStatViewModel { Value = totalCustomers.ToString("N0"), Label = "บัญชีลูกค้าที่สมัครแล้ว" }
            ],
            Values = aboutContent.Values
                .Select(item => new ServiceFeatureViewModel
                {
                    IconText = item.IconText,
                    Title = item.Title,
                    Description = item.Description
                })
                .ToList(),
            Steps = aboutContent.Steps
                .Select(item => new ProcessStepViewModel
                {
                    Number = item.Number,
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
            HeadingDescription = contactContent.HeadingDescription,
            ContactCards = contactContent.Cards
                .Select(card => new ContactInfoCardViewModel
                {
                    IconText = card.IconText,
                    Title = card.Title,
                    LineOne = card.LineOne,
                    LineTwo = card.LineTwo
                })
                .ToList(),
            Features = BuildStoreFeatures()
        };
    }

    private CartPageViewModel BuildCartPageModel(CartCheckoutViewModel? checkout = null)
    {
        var cartItems = _storeCartService.GetItems();
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

    private static IReadOnlyList<InspirationCardViewModel> BuildInspirationCards(IReadOnlyList<ProductCardViewModel> products)
    {
        return products
            .Take(2)
            .Select((product, index) => new InspirationCardViewModel
            {
                Number = (index + 1).ToString("00"),
                Title = product.Name,
                Subtitle = product.Category,
                ThemeKey = product.ThemeKey,
                ImagePath = product.ImagePath
            })
            .ToList();
    }

    private IReadOnlyList<ServiceFeatureViewModel> BuildStoreFeatures()
    {
        return _storefrontContent.Features
            .Select(feature => new ServiceFeatureViewModel
            {
                IconText = feature.IconText,
                Title = feature.Title,
                Description = feature.Description
            })
            .ToList();
    }
}
