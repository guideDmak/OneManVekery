# Storefront Home Notes

เอกสารชุดนี้อธิบายส่วนหน้าร้านของ `HomeController` และหน้าที่เกี่ยวข้องแบบแยกหมวด เพื่อให้อ่านโค้ดตาม flow ได้ง่ายขึ้นหลังจากแยก controller เป็น partial files

## อ่านไฟล์ไหนก่อน

1. `README.md` ไฟล์นี้เป็นแผนที่รวม ว่าแต่ละหมวดอยู่ตรงไหนและควรอ่านลำดับไหน
2. `01-controller-actions.md` อธิบาย action หลักใน `Controllers/HomeController.cs` ว่า request เข้ามาแล้วไปทางไหน
3. `02-home-products-categories.md` อธิบายหน้าแรก, สินค้าขายดี 8 ชิ้น, หมวดสินค้า, product card และ view model
4. `03-cart-checkout-promotions.md` อธิบายตะกร้า, checkout, ราคา, promo code, promotion, loyalty point และการสร้าง order
5. `04-views-css-layout.md` อธิบาย Razor view และ CSS ส่วน hero image, product grid 4x2, และการจัดแถวที่เหลือให้อยู่กลาง
6. `05-other-storefront-pages.md` อธิบาย About, Contact, Cart view, MyOrders และ OrderStatus

## โครงสร้างไฟล์ controller หลังแยก

| ไฟล์ | หน้าที่หลัก | เหตุผลที่แยก |
| --- | --- | --- |
| `Controllers/HomeController.cs` | public actions, route entry points, constructor, constants | เป็นหน้าประตูของ controller อ่านแล้วรู้ว่าแต่ละ URL เรียกอะไร |
| `Controllers/Home/PageModels.cs` | ประกอบ view model สำหรับ Contact, Cart, OrderStatus, MyOrders | โค้ดที่แปลงข้อมูลเป็น model สำหรับ view |
| `Controllers/Home/CartCatalog.cs` | อ่านสินค้า, อ่าน/เขียนตะกร้าใน session, เพิ่ม/ลด/ลบสินค้า, สร้าง order | เป็นงาน catalog และ cart state |
| `Controllers/Home/CheckoutPricing.cs` | คำนวณ subtotal, delivery, discount, point discount และ benefit ที่แสดงใน checkout | แยก logic ราคาออกจาก action เพื่ออ่านง่าย |
| `Controllers/Home/Promotions.cs` | หา promotion ที่ active, ตรวจ threshold, คำนวณ discount, validate promo code | แยกกติกาส่วนลดและโปรโมชั่น |
| `Controllers/Home/ProductsOrders.cs` | map product, normalize image/theme, ตรวจ stock, order number, order status, category/best seller cards | รวม helper ของ product display และ order display |
| `Controllers/Home/InternalTypes.cs` | record ภายใน controller เช่น `PricingSummary`, `PromoResolution` | เก็บ type ภายในไม่ให้ปนกับ business flow |

## ภาพรวม flow หน้าร้าน

```text
Browser
  -> ASP.NET Core route
  -> HomeController action
  -> helper ใน Controllers/Home/*.cs
  -> Entity Framework อ่าน/เขียน database หรือ Session
  -> สร้าง ViewModel ใน ViewModel/StorefrontViewModels.cs
  -> Razor view ใน Views/Home/*.cshtml
  -> CSS/JS ใน wwwroot
  -> HTML กลับไปที่ Browser
```

## Flow หน้าแรกแบบย่อ

```text
GET /
  -> HomeController.Index()
  -> GetProducts()
  -> BuildProductSalesLookup()
  -> BuildCategoryCards(products, salesLookup)
  -> BuildBestSellingProducts(products, salesLookup, 8)
  -> GetNewArrivalProducts()
  -> Views/Home/Index.cshtml
  -> CSS hero/card/grid rules
```

หน้าแรกใช้ข้อมูล 3 ชุด:

| ข้อมูล | มาจาก | ใช้ตรงไหน |
| --- | --- | --- |
| `Categories` | `BuildCategoryCards` | section "เลือกดูตามสไตล์ที่ชอบ" |
| `Products` | `BuildBestSellingProducts(..., 8)` | section "สินค้าขายดี" แบบ 4x2 |
| `NewArrivals` | `GetNewArrivalProducts` | carousel ใน hero |

## Flow หน้าสินค้าแบบย่อ

```text
GET /Home/Shop
  -> HomeController.Shop()
  -> GetProducts()
  -> distinct category list
  -> Views/Home/Shop.cshtml
  -> JS filter/search จาก data-product-card
  -> CSS catalog-grid จัด card เป็น 4 columns และแถวท้ายอยู่กลาง
```

## Flow checkout แบบย่อ

```text
POST /Home/Checkout
  -> เช็ก login
  -> GetCartItems()
  -> ApplySignedInCheckoutDefaults()
  -> BuildPricingSummary()
  -> validate promo / points / stock
  -> TryCreateOrder()
  -> ClearCart()
  -> redirect ไป OrderStatus
```

## คำที่ควรรู้ก่อนอ่าน

| คำ | ความหมายในโปรเจกต์นี้ |
| --- | --- |
| Action | public method ใน controller ที่รับ request จาก route |
| ViewModel | object ที่ controller เตรียมไว้ให้ Razor view แสดงผล |
| Entity | class ที่ผูกกับ database เช่น `Product`, `Order`, `Promotion` |
| Session | พื้นที่เก็บข้อมูลชั่วคราวของ user เช่น cart และ promo code |
| Partial class | การแยก class เดียวกันออกเป็นหลายไฟล์ ใน build สุดท้าย C# รวมกลับเป็น class เดียว |
| Sales lookup | dictionary ที่ map `ProductId` ไปเป็นจำนวนขายและรายได้รวม |
| Theme key | key สำหรับเลือกชุดสี/ภาพประกอบของการ์ดสินค้า |

## เหตุผลที่ใช้ partial class

`HomeController` ยังเป็น controller เดิมชื่อเดิม route เดิม แต่ helper ถูกย้ายออกเป็นไฟล์ย่อย เพื่อให้:

- action หลักอ่านง่ายขึ้น
- งานแต่ละหมวดไม่ปนกัน
- ลดโอกาสแก้ผิดส่วน
- เวลา review เห็น diff เฉพาะหมวด
- ไม่ต้องเปลี่ยน URL, view, dependency injection หรือ tests

สิ่งสำคัญคือไฟล์ย่อยทุกไฟล์ยังประกาศ:

```csharp
public partial class HomeController
```

แปลว่า compile แล้วทุกไฟล์คือ class `HomeController` ตัวเดียวกัน ไม่ใช่ controller ใหม่
