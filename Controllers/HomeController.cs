using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OneManVekery.Models;
using OneManVekery.Services;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class HomeController : Controller
{
    private readonly IStoreCatalogService _storeCatalogService;
    private readonly IStoreCartService _storeCartService;
    private readonly IStoreOrderService _storeOrderService;

    public HomeController(
        IStoreCatalogService storeCatalogService,
        IStoreCartService storeCartService,
        IStoreOrderService storeOrderService)
    {
        _storeCatalogService = storeCatalogService;
        _storeCartService = storeCartService;
        _storeOrderService = storeOrderService;
    }

    public IActionResult Index()
    {
        return View(new HomeIndexViewModel
        {
            Categories = GetCategories(),
            Products = _storeCatalogService.GetProducts(),
            Inspirations = GetInspirations(),
            Features = GetStoreFeatures()
        });
    }

    [HttpGet]
    public IActionResult Shop()
    {
        return View(new ShopPageViewModel
        {
            Products = _storeCatalogService.GetProducts(),
            Features = GetStoreFeatures()
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
        return View(new AboutPageViewModel
        {
            Stats =
            [
                new AboutStatViewModel { Value = "500+", Label = "ออเดอร์ที่แพ็กอย่างตั้งใจ" },
                new AboutStatViewModel { Value = "12", Label = "เมนูซิกเนเจอร์ของร้าน" },
                new AboutStatViewModel { Value = "7 วัน", Label = "อบสดใหม่ทุกสัปดาห์" }
            ],
            Values =
            [
                new ServiceFeatureViewModel
                {
                    IconText = "01",
                    Title = "อบสดทุกวัน",
                    Description = "อบขนมใหม่ทุกวันเพื่อให้รสชาติและเนื้อสัมผัสดีที่สุดก่อนถึงมือลูกค้า"
                },
                new ServiceFeatureViewModel
                {
                    IconText = "02",
                    Title = "ภาพลักษณ์ชมพูละมุน",
                    Description = "แบรนด์และแพ็กเกจเน้นโทนชมพู-ขาวให้รู้สึกอบอุ่นและน่าหยิบเป็นของฝาก"
                },
                new ServiceFeatureViewModel
                {
                    IconText = "03",
                    Title = "เหมาะกับทุกโอกาสพิเศษ",
                    Description = "ออกแบบเมนูให้เหมาะทั้งวันเกิด เบรกออฟฟิศ หรือของขวัญเล็ก ๆ"
                }
            ],
            Steps =
            [
                new ProcessStepViewModel
                {
                    Number = "01",
                    Title = "ผสม",
                    Description = "คัดวัตถุดิบและผสมสูตรให้ได้กลิ่นและความนุ่มตามมาตรฐานร้าน"
                },
                new ProcessStepViewModel
                {
                    Number = "02",
                    Title = "อบ",
                    Description = "อบเป็นรอบ ๆ ตลอดวันเพื่อคุมคุณภาพและให้ขนมออกจากเตาอย่างสดที่สุด"
                },
                new ProcessStepViewModel
                {
                    Number = "03",
                    Title = "ตกแต่งและจัดส่ง",
                    Description = "ตกแต่ง แพ็ก และจัดส่งด้วยโทนภาพลักษณ์เดียวกับหน้าร้านและหน้าเว็บ"
                }
            ]
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

        TempData["SiteNotice"] = "ส่งข้อความตัวอย่างเรียบร้อยแล้ว";
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

    private static ContactPageViewModel BuildContactPageModel(ContactFormViewModel? form = null)
    {
        return new ContactPageViewModel
        {
            Form = form ?? new ContactFormViewModel(),
            ContactCards =
            [
                new ContactInfoCardViewModel
                {
                    IconText = "A",
                    Title = "ที่อยู่",
                    LineOne = "123 Pink Bakery Lane",
                    LineTwo = "Bangkok, Thailand 10200"
                },
                new ContactInfoCardViewModel
                {
                    IconText = "P",
                    Title = "ช่องทางติดต่อ",
                    LineOne = "โทร: 089-000-1122",
                    LineTwo = "Line: @onemanvekery"
                },
                new ContactInfoCardViewModel
                {
                    IconText = "T",
                    Title = "เวลาทำการ",
                    LineOne = "จันทร์-ศุกร์: 09:00 - 20:00",
                    LineTwo = "เสาร์-อาทิตย์: 09:00 - 21:00"
                }
            ],
            Features = GetStoreFeatures()
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

    private static OrderStatusPageViewModel BuildOrderStatusPageModel(OrderReceiptRecord order)
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

    private static IReadOnlyList<PaymentOptionViewModel> BuildPaymentOptions()
    {
        return
        [
            new PaymentOptionViewModel
            {
                Code = "promptpay",
                Label = "PromptPay",
                Description = "ชำระเร็วผ่าน QR พร้อมเพย์หลังยืนยันคำสั่งซื้อ"
            },
            new PaymentOptionViewModel
            {
                Code = "card",
                Label = "บัตรเครดิต / เดบิต",
                Description = "จ่ายด้วยบัตรเพื่อสรุปยอดทันที"
            },
            new PaymentOptionViewModel
            {
                Code = "bank-transfer",
                Label = "โอนผ่านธนาคาร",
                Description = "แนบสลิปหลังโอนเงินเข้าบัญชีร้าน"
            },
            new PaymentOptionViewModel
            {
                Code = "cash-on-delivery",
                Label = "เก็บเงินปลายทาง",
                Description = "ชำระเงินปลายทางเมื่อรับสินค้า"
            }
        ];
    }

    private static string ResolvePaymentLabel(string paymentMethod)
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

    private static IReadOnlyList<OrderProgressStepViewModel> BuildOrderProgressSteps(string currentStatusCode)
    {
        var steps = new[]
        {
            ("paid", "จ่ายเสร็จ", "ร้านยืนยันคำสั่งซื้อและรับชำระเงินเรียบร้อยแล้ว", "01"),
            ("shipping", "กำลังส่ง", "ออเดอร์กำลังถูกแพ็กและส่งออกไปยังปลายทาง", "02"),
            ("delivered", "ส่งเสร็จสิ้น", "ออเดอร์ส่งถึงผู้รับเรียบร้อยและปิดงานสมบูรณ์", "03")
        };

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

    private static (string Title, string Description) ResolveOrderStatus(string currentStatusCode)
    {
        return currentStatusCode switch
        {
            "shipping" => ("กำลังส่ง", "คนส่งกำลังนำขนมออกจากร้านและมุ่งหน้าไปยังปลายทาง"),
            "delivered" => ("ส่งเสร็จสิ้น", "ออเดอร์นี้ส่งถึงผู้รับเรียบร้อยแล้ว"),
            _ => ("จ่ายเสร็จ", "ระบบยืนยันการชำระเงินแล้วและกำลังเตรียมออเดอร์ให้คุณ")
        };
    }

    private static IReadOnlyList<CategoryCardViewModel> GetCategories()
    {
        return
        [
            new CategoryCardViewModel
            {
                Title = "มาการอง",
                Subtitle = "เปลือกบาง ไส้นุ่ม เหมาะกับการจัดเป็นกล่องของขวัญ",
                ThemeKey = "macaron",
                ImagePath = "/images/theme-macaron.svg"
            },
            new CategoryCardViewModel
            {
                Title = "เค้ก",
                Subtitle = "เค้กสำหรับวันพิเศษ พร้อมครีมสดและผลไม้",
                ThemeKey = "cake",
                ImagePath = "/images/theme-cake.svg"
            },
            new CategoryCardViewModel
            {
                Title = "เบเกอรี่อบสด",
                Subtitle = "ครัวซองต์ ชูครีม และขนมอบประจำวันจากเตาร้อน ๆ",
                ThemeKey = "bakery",
                ImagePath = "/images/theme-gold.svg"
            }
        ];
    }

    private static IReadOnlyList<InspirationCardViewModel> GetInspirations()
    {
        return
        [
            new InspirationCardViewModel
            {
                Number = "01",
                Title = "โต๊ะขนมจิบน้ำชา",
                Subtitle = "โทนมาการองละมุน",
                ThemeKey = "macaron",
                ImagePath = "/images/inspiration-tea.svg"
            },
            new InspirationCardViewModel
            {
                Number = "02",
                Title = "เซตวันเกิด",
                Subtitle = "บรรยากาศเค้กฉลอง",
                ThemeKey = "cake",
                ImagePath = "/images/inspiration-birthday.svg"
            }
        ];
    }

    private static IReadOnlyList<ServiceFeatureViewModel> GetStoreFeatures()
    {
        return
        [
            new ServiceFeatureViewModel
            {
                IconText = "F",
                Title = "อบสดทุกวัน",
                Description = "ทำใหม่ทุกวันจากรอบอบของร้าน"
            },
            new ServiceFeatureViewModel
            {
                IconText = "P",
                Title = "วัตถุดิบพรีเมียม",
                Description = "ครีม เนย และไส้คุณภาพดีในทุกเมนู"
            },
            new ServiceFeatureViewModel
            {
                IconText = "S",
                Title = "ส่งฟรีตามเงื่อนไข",
                Description = "ส่งฟรีเมื่อยอดสั่งซื้อถึงขั้นต่ำที่กำหนด"
            },
            new ServiceFeatureViewModel
            {
                IconText = "H",
                Title = "ดูแลทุกออเดอร์",
                Description = "ตอบคำถามและช่วยดูแลตลอดการสั่งซื้อ"
            }
        ];
    }
}
