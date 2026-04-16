# 01 Controller Actions

ไฟล์หลัก: `Controllers/HomeController.cs`

ไฟล์นี้ควรมองเป็น "ประตูทางเข้า" ของหน้าร้าน ทุก public action ในไฟล์นี้คือจุดที่ browser หรือ form submit เข้ามา ส่วน logic ยาว ๆ ถูกแยกไปอยู่ใน `Controllers/Home/*.cs`

## บรรทัด 1-13: using, namespace, class

| บรรทัด | โค้ด | อธิบาย |
| --- | --- | --- |
| 1 | `using System.Diagnostics;` | ใช้ใน `Error()` เพื่ออ่าน `Activity.Current?.Id` |
| 2 | `using System.Globalization;` | ใช้ parse/format เลขแบบ invariant เช่น product id, ราคา, order number |
| 3 | `using System.Text.Json;` | ใช้อ่าน metadata ของสินค้าใน `ParseProductMeta` ที่อยู่ partial file |
| 4 | `using Microsoft.AspNetCore.Mvc;` | ให้ใช้ `Controller`, `IActionResult`, attributes เช่น `[HttpGet]` |
| 5 | `using Microsoft.EntityFrameworkCore;` | ให้ใช้ EF Core helpers เช่น `AsNoTracking`, `Include` ใน partial files |
| 6 | `using Microsoft.Extensions.Options;` | ใช้รับ config `StorefrontContentOptions` ผ่าน DI |
| 7 | `using OneManVekery.Models;` | model ทั่วไป เช่น option classes และ view support models |
| 8 | `using OneManVekery.Models.Db;` | entity จาก database เช่น `Product`, `Order`, `ContactMessage` |
| 9 | `using OneManVekery.ViewModel;` | view model ที่ส่งเข้า Razor views |
| 11 | `namespace OneManVekery.Controllers;` | ระบุ namespace ของ controller |
| 13 | `public partial class HomeController : Controller` | ประกาศ controller หลัก และเปิดให้แยก class นี้ไปหลายไฟล์ด้วย `partial` |

## บรรทัด 15-22: constants และ fields

| บรรทัด | โค้ด | อธิบาย |
| --- | --- | --- |
| 15 | `DefaultReorderLevel = 10` | จำนวน stock ที่ถือว่าใกล้หมด ถ้า product meta ไม่กำหนดเอง |
| 16 | `CartSessionKey` | key สำหรับเก็บ cart ใน session |
| 17 | `PromoCodeSessionKey` | key สำหรับเก็บ promo code ที่ user apply ไว้ |
| 18 | `DeliveryFeeAmount = 45m` | ค่าส่งเริ่มต้น 45 บาท |
| 19 | `PointDiscountPointStep = 10` | ทุก 10 points ใช้ลดได้หนึ่ง step |
| 20 | `PointDiscountValuePerStep = 1m` | 1 step ลดได้ 1 บาท |
| 21 | `_dbContext` | database context หลัก ใช้อ่าน/เขียนข้อมูล |
| 22 | `_storefrontContent` | config content ของ storefront เช่น About/Contact |

## บรรทัด 24-30: constructor

```csharp
public HomeController(
    OneManVekeryDBContext dbContext,
    IOptions<StorefrontContentOptions> storefrontContentOptions)
{
    _dbContext = dbContext;
    _storefrontContent = storefrontContentOptions.Value;
}
```

คำอธิบายทีละส่วน:

- `HomeController(...)` คือ constructor ที่ ASP.NET Core เรียกตอนสร้าง controller
- `OneManVekeryDBContext dbContext` ถูก inject มาจาก DI container เพื่อเข้าถึง database
- `IOptions<StorefrontContentOptions>` คือ config/options pattern ของ ASP.NET Core
- `_dbContext = dbContext;` เก็บ context ไว้ใช้ในทุก action/helper
- `_storefrontContent = storefrontContentOptions.Value;` ดึงค่า options จริงออกมาเก็บไว้

## บรรทัด 32-44: `Index()`

หน้าที่: สร้างข้อมูลหน้าแรก

```text
GET /
  -> Index()
  -> load products
  -> load sales lookup
  -> build category cards
  -> build best selling products 8 items
  -> load new arrivals
  -> render Views/Home/Index.cshtml
```

อธิบายทีละบรรทัด:

| บรรทัด | โค้ด | อธิบาย |
| --- | --- | --- |
| 32 | `public IActionResult Index()` | action หน้าแรก |
| 34 | `var products = GetProducts();` | โหลดสินค้าที่ active ทั้งหมด แล้ว map เป็น `ProductCardViewModel` |
| 35 | `var salesLookup = BuildProductSalesLookup();` | โหลดยอดขายจาก `OrderItems` แล้ว group ตามสินค้า |
| 36 | `var bestSellingProducts = BuildBestSellingProducts(products, salesLookup, 8);` | จัดอันดับสินค้าขายดีรวมทั้งร้าน และเลือก 8 ชิ้นสำหรับ 4x2 |
| 38 | `return View(new HomeIndexViewModel` | ส่ง model ไป view หน้าแรก |
| 40 | `Categories = BuildCategoryCards(products, salesLookup)` | สร้าง card หมวด พร้อมสินค้าขายดีในหมวดนั้น |
| 41 | `Products = bestSellingProducts` | section "สินค้าขายดี" ใช้ list นี้ ไม่ใช่สินค้าทั้งหมด |
| 42 | `NewArrivals = GetNewArrivalProducts()` | hero carousel ใช้สินค้าใหม่ล่าสุด |

## บรรทัด 46-61: `Shop()`

หน้าที่: แสดงหน้ารวมสินค้าและ filter หมวด

| บรรทัด | อธิบาย |
| --- | --- |
| 46 | `[HttpGet]` กำหนดว่า action นี้รับ GET |
| 47 | `Shop()` คือ action `/Home/Shop` |
| 49 | โหลด active products ทั้งหมด |
| 51-60 | สร้าง `ShopPageViewModel` |
| 53 | `Products = products` ส่งสินค้าทั้งหมดไปแสดง |
| 54-59 | สร้างรายการ category แบบไม่ซ้ำ ตัดค่าว่าง เรียงชื่อ |

## บรรทัด 63-73: `Cart()`

หน้าที่: เปิดหน้าตะกร้า

| บรรทัด | อธิบาย |
| --- | --- |
| 63 | action รับ GET |
| 64 | `Cart()` คือ `/Home/Cart` |
| 66 | ถ้า user ยังไม่ login |
| 68 | ตั้งข้อความแจ้ง |
| 69 | redirect ไป `Account/Login` |
| 72 | ถ้า login แล้ว สร้าง `CartPageViewModel` และส่งเข้า view |

## บรรทัด 75-85: `MyOrders()`

หน้าที่: แสดงรายการออเดอร์ของ user

- ใช้ login guard เหมือน `Cart()`
- ถ้า login แล้วเรียก `BuildMyOrdersPageModel()`
- helper นี้อยู่ใน `Controllers/Home/PageModels.cs`

## บรรทัด 87-104: `OrderStatus(string orderNumber)`

หน้าที่: แสดงสถานะออเดอร์เฉพาะเลขออเดอร์

ลำดับทำงาน:

1. เช็ก login
2. เรียก `GetOrderForCurrentUser(orderNumber)`
3. ถ้าไม่พบออเดอร์ redirect กลับ `MyOrders`
4. ถ้าพบ สร้าง `OrderStatusPageViewModel`

เหตุผลที่ต้องใช้ `GetOrderForCurrentUser` คือป้องกัน user เปิดเลขออเดอร์ของคนอื่น

## บรรทัด 106-126: `AddToCart(string productId)`

หน้าที่: เพิ่มสินค้าเข้าตะกร้า

| บรรทัด | อธิบาย |
| --- | --- |
| 106 | รับ POST |
| 107 | ต้องมี anti-forgery token |
| 108 | รับ `productId` จาก form |
| 110-114 | ถ้าไม่ login ให้ redirect |
| 116 | เรียก `AddItemToCart(productId)` |
| 118 | ถ้าเพิ่มสำเร็จ แจ้ง user |
| 122 | ถ้าเพิ่มไม่ได้ แจ้ง error |
| 125 | redirect กลับตะกร้า |

## บรรทัด 128-144: `ChangeCartQuantity(string productId, int delta)`

หน้าที่: เพิ่ม/ลดจำนวนสินค้าในตะกร้า

- `delta` เป็นค่าที่ form ส่งมา เช่น `1` หรือ `-1`
- action ไม่แก้ session เอง แต่ส่งต่อให้ `ChangeCartItemQuantity`
- ถ้า update ไม่ได้ จะใส่ `TempData["SiteNotice"]`
- สุดท้าย redirect กลับ `Cart`

## บรรทัด 146-166: `RemoveFromCart(string productId)`

หน้าที่: ลบสินค้าออกจาก session cart

- เรียก `RemoveCartItem(productId)`
- ถ้าลบสำเร็จ แจ้งว่าลบแล้ว
- ถ้าไม่เจอสินค้า แจ้งว่าไม่พบสินค้า

## บรรทัด 168-240: `Checkout(CartCheckoutViewModel checkout)`

หน้าที่: submit checkout และสร้าง order จริง

ลำดับหลัก:

```text
login guard
  -> GetCartItems()
  -> ApplySignedInCheckoutDefaults()
  -> BuildPricingSummary()
  -> validate promo
  -> validate point reward
  -> validate stock
  -> TryCreateOrder()
  -> ClearCart()
  -> RedirectToAction(OrderStatus)
```

จุดสำคัญ:

| บรรทัด | อธิบาย |
| --- | --- |
| 178 | โหลด cart จาก session แล้วผูกกับ product ปัจจุบัน |
| 179-183 | ถ้าตะกร้าว่าง ให้กลับหน้า Cart |
| 185 | เติมข้อมูล user ที่ login เช่น ชื่อ เบอร์ ที่อยู่ |
| 186 | คำนวณราคาและ benefit ทุกอย่าง |
| 188-201 | validate promo code, reward, point discount |
| 203-206 | validate stock ล่าสุดก่อนสร้าง order |
| 208-211 | ถ้ามี error กลับไปหน้า Cart พร้อม model เดิม |
| 213 | persist promo code เฉพาะ code ที่ใช้ได้จริง |
| 216-232 | เรียก `TryCreateOrder` เพื่อเขียน order ลง database |
| 238 | clear cart และ promo session หลังสำเร็จ |
| 240 | redirect ไปหน้า status ของ order ใหม่ |

## บรรทัด 243-290: `ApplyPromoCode(CartCheckoutViewModel checkout)`

หน้าที่: preview promo/points ในหน้าตะกร้าโดยยังไม่สร้าง order

ต่างจาก `Checkout()`:

- ไม่สร้าง order
- ไม่ clear cart
- ไม่ validate field ที่ไม่เกี่ยวกับ promo เช่น ชื่อ/ที่อยู่
- ใช้ `ClearCheckoutValidationForPromoPreview()` เพื่อให้ user กด apply promo ได้แม้ยังไม่กรอก checkout ครบ

## บรรทัด 293-315: `About()`

หน้าที่: อ่าน config About จาก `_storefrontContent` แล้วส่งเข้า view

- `StoryTitle` เป็นหัวเรื่อง
- `StoryParagraphs` ตัด paragraph ว่างออก
- `Values` map เป็น `ServiceFeatureViewModel`

## บรรทัด 318-321: `Contact()` GET

เปิดหน้า contact และใช้ `BuildContactPageModel()` เพื่อ:

- โหลด content จาก config
- เติมข้อมูล user ที่ login ถ้ามี

## บรรทัด 324-347: `Contact(ContactFormViewModel form)` POST

หน้าที่: รับ contact form และบันทึกลง database

ลำดับ:

1. ถ้า `ModelState` ไม่ valid ให้กลับ view เดิม
2. สร้าง `ContactMessage`
3. trim ค่า text ก่อนบันทึก
4. set `Status = "new"`
5. `CreatedAt = DateTime.UtcNow`
6. `SaveChanges()`
7. redirect กลับ Contact พร้อม notice

## บรรทัด 349-357: `Privacy()` และ `Error()`

- `Privacy()` แสดงหน้า privacy ธรรมดา
- `Error()` สร้าง `ErrorViewModel` พร้อม request id เพื่อใช้ debug error page

