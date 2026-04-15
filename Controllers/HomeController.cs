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
    private const string PromoCodeSessionKey = "one-man-vekery-promo-code";
    private const decimal DeliveryFeeAmount = 45m;
    private const int PointDiscountPointStep = 10;
    private const decimal PointDiscountValuePerStep = 1m;
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
        var salesLookup = BuildProductSalesLookup();
        var bestSellingProducts = BuildBestSellingProducts(products, salesLookup, 8);

        return View(new HomeIndexViewModel
        {
            Categories = BuildCategoryCards(products, salesLookup),
            Products = bestSellingProducts,
            NewArrivals = GetNewArrivalProducts()
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
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนใช้งานตะกร้าสินค้า";
            return RedirectToAction("Login", "Account");
        }

        return View(BuildCartPageModel());
    }

    [HttpGet]
    public IActionResult MyOrders()
    {
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนดูคำสั่งซื้อของคุณ";
            return RedirectToAction("Login", "Account");
        }

        return View(BuildMyOrdersPageModel());
    }

    [HttpGet]
    public IActionResult OrderStatus(string orderNumber)
    {
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนดูสถานะคำสั่งซื้อ";
            return RedirectToAction("Login", "Account");
        }

        var order = GetOrderForCurrentUser(orderNumber);
        if (order is null)
        {
            TempData["SiteNotice"] = "ไม่พบออเดอร์ที่ต้องการ";
            return RedirectToAction(nameof(MyOrders));
        }

        return View(BuildOrderStatusPageModel(order));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(string productId)
    {
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนเพิ่มสินค้าในตะกร้า";
            return RedirectToAction("Login", "Account");
        }

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
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนใช้งานตะกร้าสินค้า";
            return RedirectToAction("Login", "Account");
        }

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
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนใช้งานตะกร้าสินค้า";
            return RedirectToAction("Login", "Account");
        }

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
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนสั่งซื้อสินค้า";
            return RedirectToAction("Login", "Account");
        }

        var cartItems = GetCartItems();
        if (cartItems.Count == 0)
        {
            TempData["SiteNotice"] = "ตะกร้าสินค้ายังว่างอยู่";
            return RedirectToAction(nameof(Cart));
        }

        checkout = ApplySignedInCheckoutDefaults(checkout);
        var pricing = BuildPricingSummary(cartItems, checkout);

        if (!string.IsNullOrWhiteSpace(checkout.PromoCode) && !pricing.Promo.IsValid)
        {
            AddCheckoutFieldError(nameof(CartCheckoutViewModel.PromoCode), pricing.Promo.Message);
        }

        if (checkout.UsePointsReward && !pricing.CanRedeemFreeItem)
        {
            AddCheckoutFieldError(nameof(CartCheckoutViewModel.UsePointsReward), $"ต้องมีอย่างน้อย {pricing.RewardPointCost} พอยต์ก่อนจึงจะแลกของรางวัลนี้ได้");
        }

        if (checkout.PointsToRedeem > pricing.MaxPointDiscountRedeem)
        {
            AddCheckoutFieldError(nameof(CartCheckoutViewModel.PointsToRedeem), $"ใช้พอยต์ลดราคาได้สูงสุด {pricing.MaxPointDiscountRedeem} P สำหรับออเดอร์นี้");
        }

        if (!TryValidateCartInventory(cartItems, out var inventoryMessage))
        {
            ModelState.AddModelError(string.Empty, inventoryMessage);
        }

        if (!ModelState.IsValid)
        {
            return View("Cart", BuildCartPageModel(checkout));
        }

        PersistPromoCode(pricing.Promo.IsApplied ? pricing.Promo.Code : null);
        var signedInUserId = GetSignedInStorefrontUserId();

        if (!TryCreateOrder(
                signedInUserId,
                new CartCheckoutSnapshot
                {
                    PromoCode = pricing.Promo.IsApplied ? pricing.Promo.Code : checkout.PromoCode ?? string.Empty,
                    UsePointsReward = checkout.UsePointsReward,
                    CustomerName = checkout.CustomerName,
                    PhoneNumber = checkout.PhoneNumber,
                    DeliveryAddress = checkout.DeliveryAddress,
                    PaymentMethodCode = checkout.PaymentMethod,
                    Notes = checkout.Notes ?? string.Empty
                },
                cartItems,
                pricing,
                pricing.Promo,
                out var order,
                out var checkoutError))
        {
            ModelState.AddModelError(string.Empty, checkoutError);
            return View("Cart", BuildCartPageModel(checkout));
        }

        ClearCart();

        return RedirectToAction(nameof(OrderStatus), new { orderNumber = order!.OrderNo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyPromoCode(CartCheckoutViewModel checkout)
    {
        if (!IsStorefrontUserSignedIn())
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อนใช้งานโค้ดส่วนลด";
            return RedirectToAction("Login", "Account");
        }

        var cartItems = GetCartItems();
        if (cartItems.Count == 0)
        {
            TempData["SiteNotice"] = "ตะกร้าสินค้ายังว่างอยู่";
            return RedirectToAction(nameof(Cart));
        }

        checkout = ApplySignedInCheckoutDefaults(checkout);
        ClearCheckoutValidationForPromoPreview();

        var pricing = BuildPricingSummary(cartItems, checkout);
        var hadPersistedPromoCode = !string.IsNullOrWhiteSpace(ReadPersistedPromoCode());
        var previewAction = Request.Form["checkoutPreviewAction"].ToString();

        if (string.IsNullOrWhiteSpace(checkout.PromoCode))
        {
            PersistPromoCode(null);
            if (!hadPersistedPromoCode && string.Equals(previewAction, "promo", StringComparison.OrdinalIgnoreCase))
            {
                AddCheckoutFieldError(nameof(CartCheckoutViewModel.PromoCode), "กรุณากรอกโค้ดส่วนลด");
            }
        }
        else if (!pricing.Promo.IsValid)
        {
            AddCheckoutFieldError(nameof(CartCheckoutViewModel.PromoCode), pricing.Promo.Message);
            PersistPromoCode(null);
        }
        else
        {
            PersistPromoCode(pricing.Promo.Code);
        }

        if (checkout.PointsToRedeem > pricing.MaxPointDiscountRedeem)
        {
            AddCheckoutFieldError(nameof(CartCheckoutViewModel.PointsToRedeem), $"ใช้พอยต์ลดราคาได้สูงสุด {pricing.MaxPointDiscountRedeem} P สำหรับออเดอร์นี้");
        }

        return View("Cart", BuildCartPageModel(checkout));
    }

    [HttpGet]
    public IActionResult About()
    {
        var aboutContent = _storefrontContent.About ?? new StorefrontAboutOptions();
        var storyParagraphs = aboutContent.StoryParagraphs ?? [];
        var values = aboutContent.Values ?? [];

        return View(new AboutPageViewModel
        {
            StoryTitle = aboutContent.StoryTitle ?? string.Empty,
            StoryParagraphs = storyParagraphs
                .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
                .ToList(),
            Values = values
                .Where(item => item is not null)
                .Select(item => new ServiceFeatureViewModel
                {
                    IconText = item.IconText ?? string.Empty,
                    Title = item.Title ?? string.Empty,
                    Description = item.Description ?? string.Empty
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
        var contactForm = ApplySignedInContactDefaults(form ?? new ContactFormViewModel());

        return new ContactPageViewModel
        {
            Form = contactForm,
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

    private ContactFormViewModel ApplySignedInContactDefaults(ContactFormViewModel form)
    {
        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);
        if (!int.TryParse(accountId, out var accountIdValue) || accountIdValue <= 0)
        {
            return form;
        }

        var account = _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == accountIdValue)
            .Select(user => new
            {
                user.FullName,
                user.Email,
                user.Phone
            })
            .FirstOrDefault();

        if (account is null)
        {
            return form;
        }

        if (string.IsNullOrWhiteSpace(form.Name))
        {
            form.Name = account.FullName ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(form.Email))
        {
            form.Email = account.Email ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(form.PhoneNumber))
        {
            form.PhoneNumber = account.Phone ?? string.Empty;
        }

        return form;
    }

    private CartPageViewModel BuildCartPageModel(CartCheckoutViewModel? checkout = null)
    {
        var cartItems = GetCartItems();
        var checkoutModel = ApplySignedInCheckoutDefaults(checkout ?? new CartCheckoutViewModel
        {
            PaymentMethod = "promptpay"
        }, includePersistedPromoCode: checkout is null);
        var pricing = BuildPricingSummary(cartItems, checkoutModel);

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
            AppliedBenefits = pricing.AppliedBenefits
                .Select(MapBenefit)
                .ToList(),
            Checkout = checkoutModel,
            ItemCount = cartItems.Sum(item => item.Quantity),
            Subtotal = pricing.Subtotal,
            DeliveryFee = pricing.DeliveryFee,
            DiscountAmount = pricing.DiscountAmount,
            ShippingDiscountAmount = pricing.ShippingDiscountAmount,
            AppliedPromoCode = pricing.Promo.IsApplied ? pricing.Promo.Code : string.Empty,
            AppliedPromoTitle = pricing.Promo.IsApplied ? pricing.Promo.Title : string.Empty,
            AppliedPromoDescription = pricing.Promo.IsApplied ? pricing.Promo.Description : string.Empty,
            PromoMessage = pricing.Promo.Message,
            PromoMessageState = pricing.Promo.MessageState,
            CurrentPoints = pricing.CurrentPoints,
            PointsEarned = pricing.PointsEarned,
            PointsRedeemed = pricing.PointsRedeemed,
            PointsDiscountAmount = pricing.PointsDiscountAmount,
            MaxPointDiscountRedeem = pricing.MaxPointDiscountRedeem,
            PointDiscountRateLabel = $"{PointDiscountPointStep} P = {PointDiscountValuePerStep:0.##} ฿",
            ProjectedPointsBalance = pricing.ProjectedPointsBalance,
            PointsNeededForFreeItem = Math.Max(0, pricing.RewardPointCost - pricing.CurrentPoints),
            RewardPointCost = pricing.RewardPointCost,
            RewardQty = pricing.RewardQty,
            RewardProductName = pricing.RewardProductName,
            CanRedeemFreeItem = pricing.CanRedeemFreeItem
        };
    }

    private OrderStatusPageViewModel BuildOrderStatusPageModel(Order order)
    {
        var currentStatus = ResolveOrderStatus(order.OrderStatus);
        var latestPointsLedger = order.LoyaltyPointsLedgers
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.Id)
            .LastOrDefault();
        var promoBenefit = order.OrderPromotions
            .FirstOrDefault(item => item.PromoCodeId.HasValue)
            ?? order.OrderPromotions.FirstOrDefault(item => item.PromotionId == order.PromoCode?.PromotionId);

        return new OrderStatusPageViewModel
        {
            OrderNumber = order.OrderNo,
            CreatedAt = ConvertToStoreTime(order.CreatedAt),
            CustomerName = order.CustomerName,
            PhoneNumber = order.Phone,
            DeliveryAddress = order.Address,
            PaymentMethodLabel = ResolvePaymentLabel(order.PaymentMethod),
            Notes = order.Note ?? string.Empty,
            CurrentStatusLabel = currentStatus.Title,
            CurrentStatusDescription = currentStatus.Description,
            Items = order.OrderItems.Select(item => new OrderReceiptLineViewModel
            {
                Name = item.ProductName,
                Category = item.Product?.Category?.Name ?? string.Empty,
                Quantity = item.Qty,
                UnitPrice = item.Price,
                LineTotal = item.LineTotal
            }).ToList(),
            StatusSteps = BuildOrderProgressSteps(order.OrderStatus),
            AppliedBenefits = order.OrderPromotions
                .OrderBy(item => item.Id)
                .Select(MapBenefit)
                .ToList(),
            Subtotal = order.Subtotal,
            DeliveryFee = order.DeliveryFee,
            DiscountAmount = order.DiscountAmount,
            ShippingDiscountAmount = order.ShippingDiscountAmount,
            AppliedPromoCode = order.DiscountCode ?? string.Empty,
            AppliedPromoTitle = promoBenefit?.PromotionTitle ?? order.PromoCode?.Title ?? string.Empty,
            PointsEarned = order.PointsEarned,
            PointsRedeemed = order.PointsRedeemed,
            PointsBalanceAfter = latestPointsLedger?.BalanceAfter ?? GetCurrentPointsBalance(order.UserId)
        };
    }

    private MyOrdersPageViewModel BuildMyOrdersPageModel()
    {
        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return new MyOrdersPageViewModel();
        }

        var orders = _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.OrderItems)
            .Where(order => order.UserId == accountId.Value)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

        return new MyOrdersPageViewModel
        {
            Orders = orders.Select(MapMyOrderCard).ToList(),
            OrderCount = orders.Count,
            TotalSpendLabel = $"{orders.Sum(order => order.TotalAmount):0.##} ฿",
            LastOrderLabel = orders.Count == 0
                ? "ยังไม่มีคำสั่งซื้อ"
                : ConvertToStoreTime(orders[0].CreatedAt).ToString("dd MMM yyyy, HH:mm")
        };
    }

    private MyOrderCardViewModel MapMyOrderCard(Order order)
    {
        var status = ResolveOrderStatus(order.OrderStatus);
        var firstItem = order.OrderItems
            .OrderBy(item => item.Id)
            .FirstOrDefault();
        var totalQuantity = order.OrderItems.Sum(item => item.Qty);
        var extraItems = Math.Max(0, order.OrderItems.Count - 1);
        var productSummary = firstItem is null
            ? "ไม่มีรายการสินค้า"
            : extraItems > 0
                ? $"{firstItem.ProductName} และอีก {extraItems} รายการ"
                : firstItem.ProductName;

        return new MyOrderCardViewModel
        {
            OrderNumber = order.OrderNo,
            CreatedAt = ConvertToStoreTime(order.CreatedAt),
            ProductSummary = productSummary,
            ItemCountLabel = totalQuantity <= 0 ? "0 ชิ้น" : $"{totalQuantity} ชิ้น",
            TotalAmountLabel = $"{order.TotalAmount:0.##} ฿",
            PaymentMethodLabel = ResolvePaymentLabel(order.PaymentMethod),
            StatusLabel = status.Title,
            StatusDescription = status.Description,
            StatusKey = NormalizeOrderStatusKey(order.OrderStatus),
            PromoLabel = string.IsNullOrWhiteSpace(order.DiscountCode)
                ? string.Empty
                : order.DiscountCode,
            PointsEarned = order.PointsEarned,
            PointsRedeemed = order.PointsRedeemed
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

    private IReadOnlyList<ProductCardViewModel> GetNewArrivalProducts()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive)
            .OrderByDescending(product => product.CreatedAt)
            .ThenByDescending(product => product.Id)
            .Take(3)
            .ToList()
            .Select(MapProduct)
            .ToList();
    }

    private IReadOnlyDictionary<string, ProductSalesSummary> BuildProductSalesLookup()
    {
        return _dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.ProductId.HasValue)
            .Select(item => new
            {
                ProductId = item.ProductId!.Value,
                item.Qty,
                item.LineTotal,
                item.Order.OrderStatus
            })
            .ToList()
            .Where(item => NormalizeOrderStatusKey(item.OrderStatus) is not ("refunded" or "cancelled"))
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key.ToString(CultureInfo.InvariantCulture),
                group => new ProductSalesSummary(
                    group.Sum(item => item.Qty),
                    group.Sum(item => item.LineTotal)),
                StringComparer.OrdinalIgnoreCase);
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
        var product = GetActiveProductEntity(productId);
        if (product is null || product.StockQty <= 0 || quantity <= 0)
        {
            return false;
        }

        var items = ReadCartItems();
        var index = items.FindIndex(item => string.Equals(item.ProductId, productId, StringComparison.OrdinalIgnoreCase));
        var currentQuantity = index >= 0 ? items[index].Quantity : 0;
        var nextQuantity = Math.Min(currentQuantity + quantity, product.StockQty);

        if (nextQuantity <= currentQuantity)
        {
            return false;
        }

        if (index >= 0)
        {
            items[index] = items[index] with { Quantity = Math.Min(nextQuantity, 99) };
        }
        else
        {
            items.Add(new CartSessionItem
            {
                ProductId = productId,
                Quantity = Math.Min(nextQuantity, 99)
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
        HttpContext.Session.Remove(PromoCodeSessionKey);
    }

    private bool TryCreateOrder(
        int? userId,
        CartCheckoutSnapshot checkout,
        IReadOnlyList<CartLineRecord> items,
        PricingSummary pricing,
        PromoResolution promo,
        out Order? order,
        out string errorMessage)
    {
        order = null;
        errorMessage = string.Empty;

        var productIds = items
            .Select(item => int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                ? productIdValue
                : 0)
            .Where(productIdValue => productIdValue > 0)
            .Distinct()
            .ToArray();

        using var transaction = _dbContext.Database.BeginTransaction();

        var products = _dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id);

        foreach (var item in items)
        {
            if (!int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                || !products.TryGetValue(productIdValue, out var product))
            {
                errorMessage = "มีสินค้าบางรายการไม่พร้อมจำหน่ายแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง";
                return false;
            }

            if (!product.IsActive || product.StockQty <= 0)
            {
                errorMessage = $"{product.Name} หมดแล้ว กรุณานำออกจากตะกร้าก่อนสั่งซื้อ";
                return false;
            }

            if (item.Quantity > product.StockQty)
            {
                errorMessage = $"{product.Name} มีคงเหลือเพียง {product.StockQty} ชิ้น กรุณาปรับจำนวนก่อนสั่งซื้อ";
                return false;
            }
        }

        PromoCode? promoCode = null;
        if (promo.IsApplied && promo.PromoCodeId is int promoCodeIdValue)
        {
            promoCode = _dbContext.PromoCodes
                .Include(item => item.Promotion)
                .FirstOrDefault(item => item.Id == promoCodeIdValue);

            if (promoCode is null)
            {
                errorMessage = "ไม่พบโค้ดส่วนลดที่เลือกแล้ว กรุณาลองใหม่อีกครั้ง";
                return false;
            }

            if (!IsRecordActive(promoCode.Status) || !IsWithinUsageWindow(promoCode.StartsAt, promoCode.ExpiresAt))
            {
                errorMessage = "โค้ดส่วนลดนี้หมดอายุหรือยังไม่พร้อมใช้งานแล้ว";
                return false;
            }

            if (promoCode.UsageLimit is int usageLimit && usageLimit > 0 && promoCode.UsedCount >= usageLimit)
            {
                errorMessage = "โค้ดส่วนลดนี้ถูกใช้ครบสิทธิ์แล้ว";
                return false;
            }
        }

        var createdAt = DateTime.UtcNow;
        order = new Order
        {
            OrderNo = GenerateStorefrontOrderNumber(),
            UserId = userId,
            CustomerName = checkout.CustomerName.Trim(),
            Phone = checkout.PhoneNumber.Trim(),
            Address = checkout.DeliveryAddress.Trim(),
            PaymentMethod = checkout.PaymentMethodCode,
            PaymentStatus = "paid",
            OrderStatus = "paid",
            Subtotal = pricing.Subtotal,
            DeliveryFee = pricing.DeliveryFee,
            TotalAmount = Math.Max(0, pricing.Subtotal + pricing.DeliveryFee - pricing.DiscountAmount - pricing.ShippingDiscountAmount),
            Note = string.IsNullOrWhiteSpace(checkout.Notes) ? null : checkout.Notes.Trim(),
            CreatedAt = createdAt,
            PromoCodeId = promo.PromoCodeId,
            DiscountCode = promo.IsApplied ? promo.Code : null,
            DiscountAmount = pricing.DiscountAmount,
            ShippingDiscountAmount = pricing.ShippingDiscountAmount,
            PointsEarned = pricing.PointsEarned,
            PointsRedeemed = pricing.PointsRedeemed
        };

        foreach (var item in items)
        {
            var productIdValue = int.Parse(item.ProductId, CultureInfo.InvariantCulture);
            var product = products[productIdValue];
            product.StockQty -= item.Quantity;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Qty = item.Quantity,
                LineTotal = product.Price * item.Quantity
            });
        }

        foreach (var benefit in pricing.AppliedBenefits)
        {
            order.OrderPromotions.Add(new OrderPromotion
            {
                PromotionId = benefit.PromotionId,
                PromoCodeId = benefit.PromoCodeId,
                PromotionTitle = benefit.Title,
                BenefitType = benefit.BenefitType,
                DiscountAmount = benefit.DiscountAmount,
                ShippingDiscountAmount = benefit.ShippingDiscountAmount,
                PointsEarned = benefit.PointsEarned,
                PointsRedeemed = benefit.PointsRedeemed,
                RewardProductId = benefit.RewardProductId,
                RewardProductName = string.IsNullOrWhiteSpace(benefit.RewardProductName) ? null : benefit.RewardProductName,
                RewardQty = benefit.RewardQty,
                Note = string.IsNullOrWhiteSpace(benefit.Description) ? null : benefit.Description,
                CreatedAt = createdAt
            });
        }

        if (promoCode is not null)
        {
            promoCode.UsedCount += 1;
        }

        _dbContext.Orders.Add(order);

        if (userId is int userIdValue)
        {
            ApplyCheckoutLoyalty(userIdValue, order, pricing, createdAt);
        }

        _dbContext.SaveChanges();
        transaction.Commit();
        return true;
    }

    private Order? GetOrderForCurrentUser(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return null;
        }

        var normalizedOrderNumber = orderNumber.Trim();

        return _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.PromoCode)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product!.Category)
            .Include(order => order.OrderPromotions)
            .Include(order => order.LoyaltyPointsLedgers)
            .FirstOrDefault(order => order.OrderNo == normalizedOrderNumber && order.UserId == accountId.Value);
    }

    private bool IsStorefrontUserSignedIn()
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);

        return int.TryParse(accountId, out var accountIdValue)
               && accountIdValue > 0
               && string.Equals(roleKey, "user", StringComparison.OrdinalIgnoreCase);
    }

    private int? GetSignedInStorefrontUserId()
    {
        if (!IsStorefrontUserSignedIn())
        {
            return null;
        }

        var accountId = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey);
        return int.TryParse(accountId, out var accountIdValue) && accountIdValue > 0
            ? accountIdValue
            : null;
    }

    private List<CartSessionItem> ReadCartItems()
    {
        var raw = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartSessionItem>>(raw) ?? [];
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(CartSessionKey);
            return [];
        }
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
            var product = GetActiveProductEntity(productId);
            if (product is null || product.StockQty <= 0)
            {
                items.RemoveAt(index);
            }
            else
            {
                items[index] = items[index] with { Quantity = Math.Min(Math.Min(quantity, product.StockQty), 99) };
            }
        }

        WriteCartItems(items);
        return true;
    }

    private Product? GetActiveProductEntity(string productId)
    {
        if (!int.TryParse(productId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue))
        {
            return null;
        }

        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(item => item.Id == productIdValue && item.IsActive);
    }

    private CartCheckoutViewModel ApplySignedInCheckoutDefaults(CartCheckoutViewModel checkout, bool includePersistedPromoCode = false)
    {
        checkout.PaymentMethod = string.IsNullOrWhiteSpace(checkout.PaymentMethod)
            ? "promptpay"
            : checkout.PaymentMethod;

        if (includePersistedPromoCode && string.IsNullOrWhiteSpace(checkout.PromoCode))
        {
            checkout.PromoCode = ReadPersistedPromoCode();
        }

        var accountId = GetSignedInStorefrontUserId();
        if (accountId is null)
        {
            return checkout;
        }

        var account = _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == accountId.Value)
            .Select(user => new
            {
                user.FullName,
                user.Phone
            })
            .FirstOrDefault();

        var address = _dbContext.UserAddresses
            .AsNoTracking()
            .Where(item => item.UserId == accountId.Value)
            .OrderByDescending(item => item.IsDefault)
            .ThenByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => new
            {
                item.RecipientName,
                item.Phone,
                item.AddressLine,
                item.PostalCode
            })
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(checkout.CustomerName))
        {
            checkout.CustomerName = address?.RecipientName
                ?? account?.FullName
                ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(checkout.PhoneNumber))
        {
            checkout.PhoneNumber = address?.Phone
                ?? account?.Phone
                ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(checkout.DeliveryAddress) && address is not null)
        {
            checkout.DeliveryAddress = FormatDeliveryAddress(address.AddressLine, address.PostalCode);
        }

        return checkout;
    }

    private static string FormatDeliveryAddress(string addressLine, string? postalCode)
    {
        return string.IsNullOrWhiteSpace(postalCode)
            ? addressLine
            : $"{addressLine} {postalCode}".Trim();
    }

    private PricingSummary BuildPricingSummary(IReadOnlyList<CartLineRecord> cartItems, CartCheckoutViewModel checkout)
    {
        var subtotal = cartItems.Sum(item => item.LineTotal);
        var deliveryFee = CalculateDeliveryFee(cartItems);
        var itemCount = cartItems.Sum(item => item.Quantity);
        var currentPoints = GetCurrentPointsBalance();
        var storeNow = GetStoreNow();
        var autoPromotions = GetActiveAutomaticPromotions(storeNow);
        var loyaltyPromotion = autoPromotions
            .FirstOrDefault(promotion => promotion.PointsAwarded.HasValue || promotion.PointsCost.HasValue || promotion.RewardProductId.HasValue);
        var appliedBenefits = new List<PricingBenefit>();
        var remainingSubtotal = subtotal;
        var remainingShipping = deliveryFee;
        decimal discountAmount = 0;
        decimal shippingDiscountAmount = 0;
        var rewardPointCost = loyaltyPromotion?.PointsCost ?? 0;
        var rewardQty = loyaltyPromotion?.RewardQty ?? 1;
        var rewardProductName = loyaltyPromotion?.RewardProduct?.Name ?? "ของรางวัล";
        var canRedeemFreeItem = loyaltyPromotion is not null
            && loyaltyPromotion.RewardProduct is not null
            && rewardPointCost > 0
            && currentPoints >= rewardPointCost
            && cartItems.Count > 0;

        foreach (var promotion in autoPromotions)
        {
            if (ReferenceEquals(promotion, loyaltyPromotion))
            {
                continue;
            }

            if (!PromotionMeetsThresholds(promotion, subtotal, itemCount))
            {
                continue;
            }

            if (promotion.BuyQty is > 0 && promotion.GetQty is > 0 && remainingSubtotal > 0)
            {
                var (freeItemCount, appliedDiscount) = CalculateBuyGetPromotionDiscount(cartItems, promotion, remainingSubtotal);
                if (appliedDiscount > 0)
                {
                    discountAmount += appliedDiscount;
                    remainingSubtotal -= appliedDiscount;
                    appliedBenefits.Add(new PricingBenefit(
                        promotion.Title,
                        string.IsNullOrWhiteSpace(promotion.Description)
                            ? $"รับสินค้าฟรี {freeItemCount} ชิ้นจากรายการในตะกร้า"
                            : promotion.Description.Trim(),
                        appliedDiscount,
                        0,
                        0,
                        0,
                        "success",
                        promotion.BenefitType,
                        promotion.Id,
                        null,
                        null,
                        string.Empty,
                        null));
                }

                continue;
            }

            if (promotion.FreeShipping && remainingShipping > 0)
            {
                shippingDiscountAmount += remainingShipping;
                appliedBenefits.Add(new PricingBenefit(
                    promotion.Title,
                    string.IsNullOrWhiteSpace(promotion.Description)
                        ? $"ยอดสินค้าครบ {promotion.MinOrderAmount:0.##} ฿ แล้ว"
                        : promotion.Description.Trim(),
                    0,
                    remainingShipping,
                    0,
                    0,
                    "success",
                    promotion.BenefitType,
                    promotion.Id,
                    null,
                    null,
                    string.Empty,
                    null));
                remainingShipping = 0;
                continue;
            }

            if (remainingSubtotal > 0 && (promotion.DiscountPercent is > 0 || promotion.DiscountAmount is > 0))
            {
                var appliedDiscount = CalculatePromotionDiscountAmount(promotion, remainingSubtotal);
                if (appliedDiscount > 0)
                {
                    discountAmount += appliedDiscount;
                    remainingSubtotal -= appliedDiscount;
                    appliedBenefits.Add(new PricingBenefit(
                        promotion.Title,
                        string.IsNullOrWhiteSpace(promotion.Description)
                            ? BuildPromotionDescription(promotion, storeNow, itemCount)
                            : promotion.Description.Trim(),
                        appliedDiscount,
                        0,
                        0,
                        0,
                        "success",
                        promotion.BenefitType,
                        promotion.Id,
                        null,
                        null,
                        string.Empty,
                        null));
                }
            }
        }

        var promo = ResolvePromoCode(checkout.PromoCode, cartItems, subtotal, remainingSubtotal, remainingShipping);
        if (promo.IsApplied)
        {
            if (promo.DiscountAmount > 0)
            {
                discountAmount += promo.DiscountAmount;
                remainingSubtotal -= promo.DiscountAmount;
            }

            if (promo.ShippingDiscountAmount > 0)
            {
                shippingDiscountAmount += promo.ShippingDiscountAmount;
                remainingShipping -= promo.ShippingDiscountAmount;
            }

            appliedBenefits.Add(new PricingBenefit(
                promo.Title,
                string.IsNullOrWhiteSpace(promo.Description) ? $"ใช้โค้ด {promo.Code}" : promo.Description,
                promo.DiscountAmount,
                promo.ShippingDiscountAmount,
                0,
                0,
                "success",
                promo.BenefitType,
                promo.PromotionId,
                promo.PromoCodeId,
                promo.RewardProductId,
                promo.RewardProductName,
                promo.RewardQty));
        }

        var pointsRedeemed = 0;
        if (checkout.UsePointsReward && loyaltyPromotion is not null && canRedeemFreeItem)
        {
            var rewardDiscount = ResolveRewardPromotionDiscount(loyaltyPromotion, remainingSubtotal);
            if (rewardDiscount > 0)
            {
                pointsRedeemed = rewardPointCost;
                discountAmount += rewardDiscount;
                remainingSubtotal -= rewardDiscount;
                appliedBenefits.Add(new PricingBenefit(
                    loyaltyPromotion.Title,
                    string.IsNullOrWhiteSpace(loyaltyPromotion.Description)
                        ? $"ใช้ {rewardPointCost} พอยต์แลก {rewardProductName} ฟรี {rewardQty} ชิ้น"
                        : loyaltyPromotion.Description.Trim(),
                    rewardDiscount,
                    0,
                    0,
                    pointsRedeemed,
                    "success",
                    loyaltyPromotion.BenefitType,
                    loyaltyPromotion.Id,
                    null,
                    loyaltyPromotion.RewardProductId,
                    rewardProductName,
                    rewardQty));
            }
        }

        var maxPointDiscountRedeem = CalculateMaxPointDiscountRedeem(currentPoints - pointsRedeemed, remainingSubtotal);
        var (pointDiscountRedeemed, pointDiscountAmount) = ResolvePointDiscount(checkout.PointsToRedeem, maxPointDiscountRedeem);
        if (pointDiscountRedeemed > 0 && pointDiscountAmount > 0)
        {
            pointsRedeemed += pointDiscountRedeemed;
            discountAmount += pointDiscountAmount;
            remainingSubtotal -= pointDiscountAmount;
            appliedBenefits.Add(new PricingBenefit(
                "ใช้พอยต์ลดราคา",
                $"ใช้ {pointDiscountRedeemed} P ลดราคา {pointDiscountAmount:0.##} ฿",
                pointDiscountAmount,
                0,
                0,
                pointDiscountRedeemed,
                "success",
                "points_discount",
                loyaltyPromotion?.Id,
                null,
                null,
                string.Empty,
                null));
        }

        var pointsEarned = CalculateEarnedPoints(loyaltyPromotion, remainingSubtotal);
        if (pointsEarned > 0)
        {
            appliedBenefits.Add(new PricingBenefit(
                loyaltyPromotion?.Title ?? "รับคะแนนสะสม",
                BuildLoyaltyEarningDescription(loyaltyPromotion),
                0,
                0,
                pointsEarned,
                0,
                "neutral",
                loyaltyPromotion?.BenefitType ?? "points_reward",
                loyaltyPromotion?.Id,
                null,
                loyaltyPromotion?.RewardProductId,
                rewardProductName,
                rewardQty));
        }

        return new PricingSummary(
            subtotal,
            deliveryFee,
            discountAmount,
            shippingDiscountAmount,
            currentPoints,
            pointsEarned,
            pointsRedeemed,
            pointDiscountAmount,
            maxPointDiscountRedeem,
            currentPoints - pointsRedeemed + pointsEarned,
            rewardPointCost,
            rewardQty,
            rewardProductName,
            canRedeemFreeItem,
            loyaltyPromotion?.Id,
            appliedBenefits,
            promo);
    }

    private static int CalculateMaxPointDiscountRedeem(int availablePoints, decimal discountableSubtotal)
    {
        if (availablePoints <= 0 || discountableSubtotal <= 0)
        {
            return 0;
        }

        var pointsBySubtotal = (int)Math.Floor(discountableSubtotal / PointDiscountValuePerStep * PointDiscountPointStep);
        return Math.Min(availablePoints, Math.Max(0, pointsBySubtotal));
    }

    private static (int PointsRedeemed, decimal DiscountAmount) ResolvePointDiscount(int requestedPoints, int maxRedeemablePoints)
    {
        var pointsRedeemed = Math.Min(Math.Max(0, requestedPoints), Math.Max(0, maxRedeemablePoints));
        var discountAmount = Math.Round(pointsRedeemed / (decimal)PointDiscountPointStep * PointDiscountValuePerStep, 2, MidpointRounding.AwayFromZero);

        return (pointsRedeemed, discountAmount);
    }

    private static CheckoutBenefitViewModel MapBenefit(PricingBenefit benefit)
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
            valueParts.Add($"+{benefit.PointsEarned} P");
        }

        if (benefit.PointsRedeemed > 0)
        {
            valueParts.Add($"-{benefit.PointsRedeemed} P");
        }

        return new CheckoutBenefitViewModel
        {
            Title = benefit.Title,
            Description = benefit.Description,
            ValueLabel = valueParts.Count == 0 ? string.Empty : string.Join(" • ", valueParts),
            Tone = benefit.Tone
        };
    }

    private static CheckoutBenefitViewModel MapBenefit(OrderPromotion benefit)
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
            valueParts.Add($"+{benefit.PointsEarned} P");
        }

        if (benefit.PointsRedeemed > 0)
        {
            valueParts.Add($"-{benefit.PointsRedeemed} P");
        }

        var tone = benefit.PointsEarned > 0 && benefit.DiscountAmount <= 0 && benefit.ShippingDiscountAmount <= 0
            ? "neutral"
            : "success";

        return new CheckoutBenefitViewModel
        {
            Title = benefit.PromotionTitle,
            Description = benefit.Note ?? string.Empty,
            ValueLabel = valueParts.Count == 0 ? string.Empty : string.Join(" • ", valueParts),
            Tone = tone
        };
    }

    private int GetCurrentPointsBalance(int? userId = null)
    {
        var targetUserId = userId ?? GetSignedInStorefrontUserId();
        if (targetUserId is null)
        {
            return 0;
        }

        return _dbContext.LoyaltyWallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == targetUserId.Value)
            .Select(wallet => wallet.CurrentPoints)
            .FirstOrDefault();
    }

    private void ApplyCheckoutLoyalty(int userId, Order order, PricingSummary pricing, DateTime createdAt)
    {
        if (pricing.PointsEarned <= 0 && pricing.PointsRedeemed <= 0)
        {
            return;
        }

        var wallet = _dbContext.LoyaltyWallets
            .FirstOrDefault(item => item.UserId == userId);

        if (wallet is null)
        {
            wallet = new LoyaltyWallet
            {
                UserId = userId,
                CurrentPoints = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.LoyaltyWallets.Add(wallet);
        }
        if (pricing.PointsRedeemed > 0)
        {
            wallet.CurrentPoints = Math.Max(0, wallet.CurrentPoints - pricing.PointsRedeemed);
            wallet.LifetimeRedeemed += pricing.PointsRedeemed;

            _dbContext.LoyaltyPointsLedgers.Add(new LoyaltyPointsLedger
            {
                UserId = userId,
                Order = order,
                PromotionId = pricing.LoyaltyPromotionId,
                EntryType = "redeem",
                PointsDelta = -pricing.PointsRedeemed,
                BalanceAfter = wallet.CurrentPoints,
                Note = $"ใช้พอยต์กับออเดอร์ {order.OrderNo}",
                CreatedAt = createdAt
            });
        }

        if (pricing.PointsEarned > 0)
        {
            wallet.CurrentPoints += pricing.PointsEarned;
            wallet.LifetimeEarned += pricing.PointsEarned;

            _dbContext.LoyaltyPointsLedgers.Add(new LoyaltyPointsLedger
            {
                UserId = userId,
                Order = order,
                PromotionId = pricing.LoyaltyPromotionId,
                EntryType = "earn",
                PointsDelta = pricing.PointsEarned,
                BalanceAfter = wallet.CurrentPoints,
                Note = $"รับพอยต์จากออเดอร์ {order.OrderNo}",
                CreatedAt = createdAt
            });
        }

        wallet.UpdatedAt = createdAt;
    }

    private IReadOnlyList<Promotion> GetActiveAutomaticPromotions(DateTimeOffset storeNow)
    {
        return _dbContext.Promotions
            .AsNoTracking()
            .Include(promotion => promotion.RewardProduct)
            .Where(promotion => promotion.AutoApply && !promotion.RequiresCode)
            .ToList()
            .Where(promotion => IsRecordActive(promotion.Status) && IsPromotionActiveNow(promotion, storeNow))
            .OrderBy(promotion => promotion.Priority)
            .ThenBy(promotion => promotion.Id)
            .ToList();
    }

    private static bool IsPromotionActiveNow(Promotion promotion, DateTimeOffset storeNow)
    {
        if (!IsWithinUsageWindow(promotion.StartsAt, promotion.ExpiresAt))
        {
            return false;
        }

        if (promotion.WeekdayMask is int weekdayMask && weekdayMask > 0)
        {
            var currentDayMask = GetWeekdayMask(storeNow.DayOfWeek);
            if ((weekdayMask & currentDayMask) == 0)
            {
                return false;
            }
        }

        if (promotion.DailyStartTime.HasValue || promotion.DailyEndTime.HasValue)
        {
            var nowTime = TimeOnly.FromDateTime(storeNow.DateTime);
            var startTime = promotion.DailyStartTime ?? TimeOnly.MinValue;
            var endTime = promotion.DailyEndTime ?? TimeOnly.MaxValue;

            if (startTime <= endTime)
            {
                if (nowTime < startTime || nowTime >= endTime)
                {
                    return false;
                }
            }
            else if (nowTime < startTime && nowTime >= endTime)
            {
                return false;
            }
        }

        return true;
    }

    private static int GetWeekdayMask(DayOfWeek dayOfWeek)
    {
        return 1 << (int)dayOfWeek;
    }

    private static bool PromotionMeetsThresholds(Promotion promotion, decimal subtotal, int itemCount)
    {
        if (promotion.MinOrderAmount is decimal minOrderAmount && minOrderAmount > 0 && subtotal < minOrderAmount)
        {
            return false;
        }

        if (promotion.MinItemQty is int minItemQty && minItemQty > 0 && itemCount < minItemQty)
        {
            return false;
        }

        return true;
    }

    private static (int FreeItemCount, decimal DiscountAmount) CalculateBuyGetPromotionDiscount(
        IReadOnlyList<CartLineRecord> cartItems,
        Promotion promotion,
        decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0 || promotion.BuyQty is not > 0 || promotion.GetQty is not > 0)
        {
            return (0, 0);
        }

        var setSize = promotion.BuyQty.Value + promotion.GetQty.Value;
        var freeItemCount = 0;
        var discountAmount = 0m;

        foreach (var item in cartItems)
        {
            if (item.Quantity < setSize)
            {
                continue;
            }

            var eligibleFreeQty = (item.Quantity / setSize) * promotion.GetQty.Value;
            if (eligibleFreeQty <= 0)
            {
                continue;
            }

            freeItemCount += eligibleFreeQty;
            discountAmount += eligibleFreeQty * item.UnitPrice;
        }

        return (freeItemCount, Math.Min(discountAmount, remainingSubtotal));
    }

    private static decimal CalculatePromotionDiscountAmount(Promotion promotion, decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0)
        {
            return 0;
        }

        decimal discountAmount = 0;

        if (promotion.DiscountPercent is decimal discountPercent && discountPercent > 0)
        {
            discountAmount = Math.Round(remainingSubtotal * (discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        }
        else if (promotion.DiscountAmount is decimal fixedDiscountAmount && fixedDiscountAmount > 0)
        {
            discountAmount = fixedDiscountAmount;
        }

        if (promotion.MaxDiscountAmount is decimal maxDiscountAmount && maxDiscountAmount > 0)
        {
            discountAmount = Math.Min(discountAmount, maxDiscountAmount);
        }

        return Math.Min(discountAmount, remainingSubtotal);
    }

    private static decimal ResolveRewardPromotionDiscount(Promotion promotion, decimal remainingSubtotal)
    {
        if (remainingSubtotal <= 0 || promotion.RewardProduct is null)
        {
            return 0;
        }

        var rewardQty = promotion.RewardQty.GetValueOrDefault(1);
        if (rewardQty <= 0)
        {
            rewardQty = 1;
        }

        var rewardValue = promotion.RewardProduct.Price * rewardQty;
        return Math.Min(rewardValue, remainingSubtotal);
    }

    private static int CalculateEarnedPoints(Promotion? promotion, decimal remainingSubtotal)
    {
        if (promotion?.PointsAwarded is not > 0)
        {
            return 0;
        }

        if (promotion.SpendStepAmount is not decimal spendStepAmount || spendStepAmount <= 0 || remainingSubtotal < spendStepAmount)
        {
            return 0;
        }

        return (int)Math.Floor(remainingSubtotal / spendStepAmount) * promotion.PointsAwarded.Value;
    }

    private static string BuildPromotionDescription(Promotion promotion, DateTimeOffset storeNow, int itemCount)
    {
        if (promotion.MinItemQty is int minItemQty && minItemQty > 0)
        {
            return $"จำนวนสินค้าในตะกร้าครบ {itemCount} ชิ้นแล้ว";
        }

        if (promotion.FreeShipping && promotion.MinOrderAmount is decimal minOrderAmount && minOrderAmount > 0)
        {
            return $"ยอดสินค้าครบ {minOrderAmount:0.##} ฿ แล้ว";
        }

        if (promotion.DailyStartTime.HasValue || promotion.DailyEndTime.HasValue)
        {
            return $"สิทธิ์พิเศษตามช่วงเวลา {storeNow:HH:mm}";
        }

        return "สิทธิ์นี้ถูกใช้กับออเดอร์ปัจจุบันแล้ว";
    }

    private static string BuildLoyaltyEarningDescription(Promotion? promotion)
    {
        if (promotion?.SpendStepAmount is decimal spendStepAmount && promotion.PointsAwarded is int pointsAwarded)
        {
            return $"ทุกยอดซื้อครบ {spendStepAmount:0.##} ฿ รับ {pointsAwarded} พอยต์";
        }

        return "ได้รับคะแนนสะสมจากออเดอร์นี้";
    }

    private static DateTimeOffset GetStoreNow()
    {
        var utcNow = DateTimeOffset.UtcNow;

        try
        {
            return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return utcNow.ToLocalTime();
            }
        }
    }

    private PromoResolution ResolvePromoCode(
        string? rawCode,
        IReadOnlyList<CartLineRecord> cartItems,
        decimal eligibilitySubtotal,
        decimal discountBaseSubtotal,
        decimal deliveryFee)
    {
        var storeNow = GetStoreNow();
        var normalizedCode = rawCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return PromoResolution.Empty;
        }

        var promoCode = _dbContext.PromoCodes
            .AsNoTracking()
            .Include(item => item.Promotion)
                .ThenInclude(promotion => promotion!.RewardProduct)
            .FirstOrDefault(item => item.Code == normalizedCode);

        if (promoCode is null)
        {
            return PromoResolution.Invalid(normalizedCode, "ไม่พบโค้ดส่วนลดนี้");
        }

        if (!IsRecordActive(promoCode.Status))
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้ยังไม่พร้อมใช้งาน");
        }

        if (!IsWithinUsageWindow(promoCode.StartsAt, promoCode.ExpiresAt))
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้หมดอายุหรือยังไม่เริ่มใช้งาน");
        }

        if (promoCode.UsageLimit is int usageLimit && usageLimit > 0 && promoCode.UsedCount >= usageLimit)
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดส่วนลดนี้ถูกใช้ครบสิทธิ์แล้ว");
        }

        var promotion = promoCode.Promotion;
        if (promotion is not null)
        {
            if (!IsRecordActive(promotion.Status))
            {
                return PromoResolution.Invalid(normalizedCode, "แคมเปญส่วนลดนี้ยังไม่พร้อมใช้งาน");
            }

            if (!IsPromotionActiveNow(promotion, storeNow))
            {
                return PromoResolution.Invalid(normalizedCode, "แคมเปญส่วนลดนี้หมดอายุหรือยังไม่เริ่มใช้งาน");
            }
        }

        var minOrderAmount = new[] { promoCode.MinOrderAmount, promotion?.MinOrderAmount }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(0)
            .Max();

        if (minOrderAmount > 0 && eligibilitySubtotal < minOrderAmount)
        {
            return PromoResolution.Invalid(normalizedCode, $"โค้ดนี้ใช้ได้เมื่อสั่งซื้อขั้นต่ำ {minOrderAmount:0.##} ฿");
        }

        var minItemQuantity = promotion?.MinItemQty ?? 0;
        var itemCount = cartItems.Sum(item => item.Quantity);
        if (minItemQuantity > 0 && itemCount < minItemQuantity)
        {
            return PromoResolution.Invalid(normalizedCode, $"โค้ดนี้ใช้ได้เมื่อซื้ออย่างน้อย {minItemQuantity} ชิ้น");
        }

        var discountAmount = CalculatePromoDiscountAmount(promoCode, promotion, discountBaseSubtotal);
        var shippingDiscountAmount = CalculateShippingDiscountAmount(promoCode, promotion, deliveryFee);
        var totalSavings = discountAmount + shippingDiscountAmount;

        if (totalSavings <= 0)
        {
            return PromoResolution.Invalid(normalizedCode, "โค้ดนี้ยังไม่ลดเพิ่มจากยอดสั่งซื้อปัจจุบัน");
        }

        var title = string.IsNullOrWhiteSpace(promoCode.Title)
            ? promotion?.Title ?? normalizedCode
            : promoCode.Title.Trim();
        var description = string.IsNullOrWhiteSpace(promoCode.Description)
            ? promotion?.Description?.Trim() ?? string.Empty
            : promoCode.Description.Trim();
        var savedLabel = totalSavings.ToString("0.##", CultureInfo.InvariantCulture);

        return PromoResolution.Applied(
            normalizedCode,
            title,
            description,
            discountAmount,
            shippingDiscountAmount,
            $"ใช้โค้ด {normalizedCode} แล้ว ประหยัด {savedLabel} ฿",
            promoCode.Id,
            promotion?.Id,
            promotion?.BenefitType ?? NormalizeDiscountType(promoCode.DiscountType),
            promotion?.RewardProductId,
            promotion?.RewardProduct?.Name ?? string.Empty,
            promotion?.RewardQty);
    }

    private static decimal CalculatePromoDiscountAmount(PromoCode promoCode, Promotion? promotion, decimal subtotal)
    {
        var discountType = NormalizeDiscountType(promoCode.DiscountType);
        var maxDiscount = promoCode.MaxDiscountAmount ?? promotion?.MaxDiscountAmount;
        decimal discountAmount;

        switch (discountType)
        {
            case "percent":
                discountAmount = subtotal * (promoCode.DiscountValue / 100m);
                break;
            case "amount":
                discountAmount = promoCode.DiscountValue;
                break;
            default:
                if (promotion?.DiscountPercent is decimal promotionPercent && promotionPercent > 0)
                {
                    discountAmount = subtotal * (promotionPercent / 100m);
                }
                else
                {
                    discountAmount = promotion?.DiscountAmount ?? 0;
                }

                break;
        }

        if (maxDiscount is decimal cap && cap > 0)
        {
            discountAmount = Math.Min(discountAmount, cap);
        }

        return Math.Min(Math.Max(discountAmount, 0), subtotal);
    }

    private static decimal CalculateShippingDiscountAmount(PromoCode promoCode, Promotion? promotion, decimal deliveryFee)
    {
        if (deliveryFee <= 0)
        {
            return 0;
        }

        var discountType = NormalizeDiscountType(promoCode.DiscountType);

        if (discountType == "shipping")
        {
            var shippingDiscount = promoCode.DiscountValue <= 0
                ? deliveryFee
                : promoCode.DiscountValue;

            return Math.Min(shippingDiscount, deliveryFee);
        }

        if (promotion?.FreeShipping == true)
        {
            return deliveryFee;
        }

        return 0;
    }

    private static string NormalizeDiscountType(string? discountType)
    {
        var normalized = discountType?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "percent" or "percentage" or "percent_discount" or "order_percent" => "percent",
            "amount" or "fixed" or "flat" or "fixed_amount" or "order_amount" => "amount",
            "shipping" or "shipping_discount" or "free_shipping" => "shipping",
            _ => normalized
        };
    }

    private static bool IsRecordActive(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized is "active" or "enabled" or "published" or "live";
    }

    private static bool IsWithinUsageWindow(DateTime? startsAt, DateTime? expiresAt)
    {
        var utcNow = DateTime.UtcNow;

        if (startsAt.HasValue && startsAt.Value > utcNow)
        {
            return false;
        }

        if (expiresAt.HasValue && expiresAt.Value < utcNow)
        {
            return false;
        }

        return true;
    }

    private string GenerateStorefrontOrderNumber()
    {
        string orderNumber;
        do
        {
            var timestamp = GetStoreNow();
            orderNumber = $"OVK-{timestamp:yyyyMMdd}-{timestamp:HHmmss}-{Random.Shared.Next(100, 999)}";
        }
        while (_dbContext.Orders.AsNoTracking().Any(order => order.OrderNo == orderNumber));

        return orderNumber;
    }

    private static DateTimeOffset ConvertToStoreTime(DateTime utcDateTime)
    {
        var utcOffset = new DateTimeOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc));

        try
        {
            return TimeZoneInfo.ConvertTime(utcOffset, TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok"));
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utcOffset, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            }
            catch (TimeZoneNotFoundException)
            {
                return utcOffset.ToLocalTime();
            }
        }
    }

    private bool TryValidateCartInventory(IReadOnlyList<CartLineRecord> cartItems, out string message)
    {
        message = string.Empty;

        var productIds = cartItems
            .Select(item => int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                ? productIdValue
                : 0)
            .Where(productIdValue => productIdValue > 0)
            .Distinct()
            .ToArray();

        var inventory = _dbContext.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.StockQty,
                product.IsActive
            })
            .ToDictionary(product => product.Id);

        foreach (var item in cartItems)
        {
            if (!int.TryParse(item.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productIdValue)
                || !inventory.TryGetValue(productIdValue, out var product))
            {
                message = "มีสินค้าบางรายการไม่พร้อมจำหน่ายแล้ว กรุณาตรวจสอบตะกร้าอีกครั้ง";
                return false;
            }

            if (!product.IsActive || product.StockQty <= 0)
            {
                message = $"{product.Name} หมดแล้ว กรุณานำออกจากตะกร้าก่อนสั่งซื้อ";
                return false;
            }

            if (item.Quantity > product.StockQty)
            {
                message = $"{product.Name} มีคงเหลือเพียง {product.StockQty} ชิ้น กรุณาปรับจำนวนก่อนสั่งซื้อ";
                return false;
            }
        }

        return true;
    }

    private void ClearCheckoutValidationForPromoPreview()
    {
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.CustomerName));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PhoneNumber));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.DeliveryAddress));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PaymentMethod));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.UsePointsReward));
        RemoveCheckoutFieldErrors(nameof(CartCheckoutViewModel.PointsToRedeem));
    }

    private void RemoveCheckoutFieldErrors(string fieldName)
    {
        foreach (var key in ModelState.Keys.ToList())
        {
            if (string.Equals(key, fieldName, StringComparison.OrdinalIgnoreCase)
                || key.EndsWith($".{fieldName}", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.Remove(key);
            }
        }
    }

    private void AddCheckoutFieldError(string fieldName, string errorMessage)
    {
        var key = $"Checkout.{fieldName}";
        ModelState.Remove(key);
        ModelState.AddModelError(key, errorMessage);
    }

    private string ReadPersistedPromoCode()
    {
        return HttpContext.Session.GetString(PromoCodeSessionKey)?.Trim().ToUpperInvariant() ?? string.Empty;
    }

    private void PersistPromoCode(string? promoCode)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
        {
            HttpContext.Session.Remove(PromoCodeSessionKey);
            return;
        }

        HttpContext.Session.SetString(PromoCodeSessionKey, promoCode.Trim().ToUpperInvariant());
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
        return cartItems.Count == 0 ? 0 : DeliveryFeeAmount;
    }

    private IReadOnlyList<OrderProgressStepViewModel> BuildOrderProgressSteps(string currentStatusCode)
    {
        var normalizedStatus = NormalizeOrderStatusKey(currentStatusCode);
        if (normalizedStatus is "refunded" or "cancelled")
        {
            var specialTitle = normalizedStatus == "refunded" ? "คืนเงินแล้ว" : "ยกเลิกออเดอร์";
            var specialDescription = normalizedStatus == "refunded"
                ? "ร้านดำเนินการคืนเงินสำหรับคำสั่งซื้อนี้แล้ว"
                : "คำสั่งซื้อนี้ถูกยกเลิกและจะไม่เข้าสู่ขั้นตอนจัดส่ง";
            var specialMarker = normalizedStatus == "refunded" ? "RF" : "CN";

            return
            [
                new OrderProgressStepViewModel
                {
                    Title = "รับคำสั่งซื้อ",
                    Description = "คำสั่งซื้อถูกบันทึกในระบบเรียบร้อยแล้ว",
                    Marker = "01",
                    State = "complete"
                },
                new OrderProgressStepViewModel
                {
                    Title = specialTitle,
                    Description = specialDescription,
                    Marker = specialMarker,
                    State = normalizedStatus
                }
            ];
        }

        var steps = _storefrontContent.OrderStatusSteps
            .Select(step => (step.Code, step.Title, step.Description, step.Marker))
            .ToArray();

        var currentIndex = Array.FindIndex(steps, step => string.Equals(step.Item1, currentStatusCode, StringComparison.OrdinalIgnoreCase));
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
        var normalizedStatus = NormalizeOrderStatusKey(currentStatusCode);
        if (normalizedStatus == "refunded")
        {
            return ("คืนเงินแล้ว", "ร้านดำเนินการคืนเงินให้คำสั่งซื้อนี้แล้ว หากมีคำถามเพิ่มเติมสามารถติดต่อร้านได้");
        }

        if (normalizedStatus == "cancelled")
        {
            return ("ยกเลิกออเดอร์", "คำสั่งซื้อนี้ถูกยกเลิกแล้ว และจะไม่เข้าสู่ขั้นตอนจัดส่ง");
        }

        var status = _storefrontContent.OrderStatusSteps
            .FirstOrDefault(step => string.Equals(step.Code, currentStatusCode, StringComparison.OrdinalIgnoreCase))
            ?? _storefrontContent.OrderStatusSteps.FirstOrDefault();

        return status is null
            ? ("สถานะคำสั่งซื้อ", "ระบบกำลังอัปเดตสถานะล่าสุดของออเดอร์นี้")
            : (status.Title, status.CurrentDescription);
    }

    private static string NormalizeOrderStatusKey(string? status)
    {
        var normalized = status?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized switch
        {
            "paid" or "pending" => "paid",
            "shipping" or "shipped" => "shipping",
            "delivered" or "complete" or "completed" => "delivered",
            "refunded" => "refunded",
            "cancelled" or "canceled" => "cancelled",
            _ => "paid"
        };
    }

    private static IReadOnlyList<CategoryCardViewModel> BuildCategoryCards(
        IReadOnlyList<ProductCardViewModel> products,
        IReadOnlyDictionary<string, ProductSalesSummary> salesLookup)
    {
        return products
            .Where(product => !string.IsNullOrWhiteSpace(product.Category))
            .GroupBy(product => product.Category, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group =>
            {
                var rankedProducts = group
                    .Select(product =>
                    {
                        var sales = salesLookup.TryGetValue(product.ProductId, out var summary)
                            ? summary
                            : new ProductSalesSummary(0, 0);

                        return new
                        {
                            Product = product,
                            Sales = sales
                        };
                    })
                    .OrderByDescending(item => item.Sales.UnitsSold)
                    .ThenByDescending(item => item.Sales.Revenue)
                    .ThenBy(item => item.Product.IsSoldOut)
                    .ThenBy(item => item.Product.Name)
                    .ToList();
                var featured = rankedProducts.First();
                var itemCount = group.Count();

                return new CategoryCardViewModel
                {
                    Title = group.First().Category,
                    Subtitle = $"{itemCount} เมนูในหมวดนี้",
                    ThemeKey = featured.Product.ThemeKey,
                    ImagePath = featured.Product.ImagePath,
                    ItemCount = itemCount,
                    FeaturedProductName = featured.Product.Name,
                    FeaturedProductDescription = featured.Product.Description,
                    FeaturedProductPriceLabel = $"{featured.Product.Price:0.##} ฿",
                    FeaturedProductSalesLabel = featured.Sales.UnitsSold > 0
                        ? $"ขายแล้ว {featured.Sales.UnitsSold:N0} ชิ้น"
                        : "เมนูเด่นของหมวดนี้",
                    FeaturedProductBadge = featured.Sales.UnitsSold > 0 ? "ขายดี" : "แนะนำ"
                };
            })
            .ToList();
    }

    private static IReadOnlyList<ProductCardViewModel> BuildBestSellingProducts(
        IReadOnlyList<ProductCardViewModel> products,
        IReadOnlyDictionary<string, ProductSalesSummary> salesLookup,
        int take)
    {
        return products
            .Select(product =>
            {
                var sales = salesLookup.TryGetValue(product.ProductId, out var summary)
                    ? summary
                    : new ProductSalesSummary(0, 0);

                return new
                {
                    Product = product,
                    Sales = sales
                };
            })
            .OrderByDescending(item => item.Sales.UnitsSold)
            .ThenByDescending(item => item.Sales.Revenue)
            .ThenBy(item => item.Product.IsSoldOut)
            .ThenBy(item => item.Product.Name)
            .Take(take)
            .Select(item => item.Product)
            .ToList();
    }

    private sealed record ProductSalesSummary(int UnitsSold, decimal Revenue);

    private sealed record PricingSummary(
        decimal Subtotal,
        decimal DeliveryFee,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        int CurrentPoints,
        int PointsEarned,
        int PointsRedeemed,
        decimal PointsDiscountAmount,
        int MaxPointDiscountRedeem,
        int ProjectedPointsBalance,
        int RewardPointCost,
        int RewardQty,
        string RewardProductName,
        bool CanRedeemFreeItem,
        int? LoyaltyPromotionId,
        IReadOnlyList<PricingBenefit> AppliedBenefits,
        PromoResolution Promo);

    private sealed record PricingBenefit(
        string Title,
        string Description,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        int PointsEarned,
        int PointsRedeemed,
        string Tone,
        string BenefitType,
        int? PromotionId,
        int? PromoCodeId,
        int? RewardProductId,
        string RewardProductName,
        int? RewardQty);

    private sealed record PromoResolution(
        bool IsValid,
        bool IsApplied,
        string Code,
        string Title,
        string Description,
        decimal DiscountAmount,
        decimal ShippingDiscountAmount,
        string Message,
        string MessageState,
        int? PromoCodeId,
        int? PromotionId,
        string BenefitType,
        int? RewardProductId,
        string RewardProductName,
        int? RewardQty)
    {
        public static PromoResolution Empty { get; } = new(
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            string.Empty,
            string.Empty,
            null,
            null,
            string.Empty,
            null,
            string.Empty,
            null);

        public static PromoResolution Invalid(string code, string message) => new(
            false,
            false,
            code,
            string.Empty,
            string.Empty,
            0,
            0,
            message,
            "warning",
            null,
            null,
            string.Empty,
            null,
            string.Empty,
            null);

        public static PromoResolution Applied(
            string code,
            string title,
            string description,
            decimal discountAmount,
            decimal shippingDiscountAmount,
            string message,
            int? promoCodeId,
            int? promotionId,
            string benefitType,
            int? rewardProductId,
            string rewardProductName,
            int? rewardQty) => new(
            true,
            true,
            code,
            title,
            description,
            discountAmount,
            shippingDiscountAmount,
            message,
            "success",
            promoCodeId,
            promotionId,
            benefitType,
            rewardProductId,
            rewardProductName,
            rewardQty);
    }

}
