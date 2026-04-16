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

public partial class HomeController : Controller
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

}
