using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OneManVekery.Models;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new HomeIndexViewModel
        {
            Categories = GetCategories(),
            Products = GetProducts(),
            Inspirations = GetInspirations(),
            Features = GetStoreFeatures()
        });
    }

    [HttpGet]
    public IActionResult Shop()
    {
        return View(new ShopPageViewModel
        {
            Products = GetProducts(),
            Features = GetStoreFeatures()
        });
    }

    [HttpGet]
    public IActionResult About()
    {
        return View(new AboutPageViewModel
        {
            Stats =
            [
                new AboutStatViewModel { Value = "500+", Label = "orders packed with care" },
                new AboutStatViewModel { Value = "12", Label = "signature bakery items" },
                new AboutStatViewModel { Value = "7 days", Label = "fresh batches every week" }
            ],
            Values =
            [
                new ServiceFeatureViewModel
                {
                    IconText = "01",
                    Title = "Fresh Daily",
                    Description = "อบขนมใหม่ทุกวันเพื่อให้รสชาติและเนื้อสัมผัสดีที่สุดก่อนถึงมือลูกค้า"
                },
                new ServiceFeatureViewModel
                {
                    IconText = "02",
                    Title = "Soft Pink Identity",
                    Description = "แบรนด์และแพ็กเกจเน้นโทนชมพู-ขาวให้รู้สึกอบอุ่นและน่าหยิบเป็นของฝาก"
                },
                new ServiceFeatureViewModel
                {
                    IconText = "03",
                    Title = "Made For Celebration",
                    Description = "ออกแบบเมนูให้เหมาะทั้งวันเกิด เบรกออฟฟิศ หรือของขวัญเล็ก ๆ"
                }
            ],
            Steps =
            [
                new ProcessStepViewModel
                {
                    Number = "01",
                    Title = "Mix",
                    Description = "คัดวัตถุดิบและผสมสูตรให้ได้กลิ่นและความนุ่มตามมาตรฐานร้าน"
                },
                new ProcessStepViewModel
                {
                    Number = "02",
                    Title = "Bake",
                    Description = "อบเป็นรอบ ๆ ตลอดวันเพื่อคุมคุณภาพและให้ขนมออกจากเตาอย่างสดที่สุด"
                },
                new ProcessStepViewModel
                {
                    Number = "03",
                    Title = "Finish",
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
                    Title = "Address",
                    LineOne = "123 Pink Bakery Lane",
                    LineTwo = "Bangkok, Thailand 10200"
                },
                new ContactInfoCardViewModel
                {
                    IconText = "P",
                    Title = "Phone",
                    LineOne = "Mobile: 089-000-1122",
                    LineTwo = "Line: @onemanvekery"
                },
                new ContactInfoCardViewModel
                {
                    IconText = "T",
                    Title = "Working Time",
                    LineOne = "Monday-Friday: 09:00 - 20:00",
                    LineTwo = "Saturday-Sunday: 09:00 - 21:00"
                }
            ],
            Features = GetStoreFeatures()
        };
    }

    private static IReadOnlyList<CategoryCardViewModel> GetCategories()
    {
        return
        [
            new CategoryCardViewModel
            {
                Title = "Macaron",
                Subtitle = "Soft shell, sweet filling, perfect for gift boxes",
                ThemeKey = "macaron",
                ImagePath = "/images/theme-macaron.svg"
            },
            new CategoryCardViewModel
            {
                Title = "Cake",
                Subtitle = "Celebration cakes with fresh cream and fruit notes",
                ThemeKey = "cake",
                ImagePath = "/images/theme-cake.svg"
            },
            new CategoryCardViewModel
            {
                Title = "Fresh Bakery",
                Subtitle = "Croissant, choux and daily baked sweet pastries",
                ThemeKey = "bakery",
                ImagePath = "/images/theme-gold.svg"
            }
        ];
    }

    private static IReadOnlyList<ProductCardViewModel> GetProducts()
    {
        return
        [
            new ProductCardViewModel
            {
                Name = "Rose Macaron Box",
                Category = "Macaron",
                Description = "Rose and vanilla macarons for soft pink gift sets",
                Price = 120,
                OriginalPrice = 140,
                Badge = "-15%",
                ThemeKey = "macaron",
                ImagePath = "/images/theme-macaron.svg"
            },
            new ProductCardViewModel
            {
                Name = "Strawberry Shortcake",
                Category = "Cake",
                Description = "Fresh cream cake with soft sponge and strawberry topping",
                Price = 145,
                OriginalPrice = 165,
                Badge = "New",
                ThemeKey = "cake",
                ImagePath = "/images/theme-cake.svg"
            },
            new ProductCardViewModel
            {
                Name = "Vanilla Choux Cream",
                Category = "Choux Cream",
                Description = "Light pastry shell with smooth vanilla custard filling",
                Price = 55,
                ThemeKey = "cream",
                ImagePath = "/images/theme-cream.svg"
            },
            new ProductCardViewModel
            {
                Name = "Butter Croissant",
                Category = "Bakery",
                Description = "Flaky layers with rich butter aroma from the morning batch",
                Price = 69,
                Badge = "Sold Out",
                ThemeKey = "gold",
                ImagePath = "/images/theme-gold.svg",
                IsSoldOut = true
            },
            new ProductCardViewModel
            {
                Name = "Blueberry Cheesecake",
                Category = "Cake",
                Description = "Creamy cheesecake finished with blueberry glaze",
                Price = 159,
                Badge = "New",
                ThemeKey = "berry",
                ImagePath = "/images/theme-berry.svg"
            },
            new ProductCardViewModel
            {
                Name = "Mini Eclair Set",
                Category = "Bakery",
                Description = "Small eclair box for afternoon sharing and coffee time",
                Price = 89,
                OriginalPrice = 110,
                Badge = "-20%",
                ThemeKey = "cream",
                ImagePath = "/images/theme-cream.svg"
            },
            new ProductCardViewModel
            {
                Name = "Milk Cloud Roll",
                Category = "Cake",
                Description = "Japanese style roll cake with soft milk whipped cream",
                Price = 135,
                ThemeKey = "milk",
                ImagePath = "/images/theme-milk.svg"
            },
            new ProductCardViewModel
            {
                Name = "Cherry Tart Slice",
                Category = "Bakery",
                Description = "Buttery tart shell with cherry compote and almond cream",
                Price = 95,
                ThemeKey = "berry",
                ImagePath = "/images/theme-berry.svg"
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
                Title = "High Tea Table",
                Subtitle = "Macaron Mood",
                ThemeKey = "macaron",
                ImagePath = "/images/inspiration-tea.svg"
            },
            new InspirationCardViewModel
            {
                Number = "02",
                Title = "Birthday Set Up",
                Subtitle = "Cake Celebration",
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
                Title = "Fresh Daily",
                Description = "crafted from fresh bakery batches"
            },
            new ServiceFeatureViewModel
            {
                IconText = "P",
                Title = "Premium Ingredients",
                Description = "soft cream, butter and quality fillings"
            },
            new ServiceFeatureViewModel
            {
                IconText = "S",
                Title = "Free Shipping",
                Description = "order over 100 THB"
            },
            new ServiceFeatureViewModel
            {
                IconText = "H",
                Title = "Friendly Support",
                Description = "helpful support for every order"
            }
        ];
    }
}
