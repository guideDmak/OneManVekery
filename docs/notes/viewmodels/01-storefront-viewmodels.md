# Storefront ViewModels

ไฟล์หลัก: `ViewModel/StorefrontViewModels.cs`

กลุ่มนี้ใช้กับหน้าร้านทั้งหมด ตั้งแต่หน้าแรก เลือกสินค้า ตะกร้า checkout สถานะ order และหน้าข้อมูลร้าน

## แผนที่ class

| Class | บรรทัด | ใช้กับ | Controller builder/action |
| --- | --- | --- | --- |
| `HomeIndexViewModel` | 5 | `Views/Home/Index.cshtml` | `HomeController.Index()` |
| `ShopPageViewModel` | 14 | `Views/Home/Shop.cshtml` | `HomeController.Shop()` |
| `CartPageViewModel` | 21 | `Views/Home/Cart.cshtml` | `BuildCartPageModel()` |
| `OrderStatusPageViewModel` | 88 | `Views/Home/OrderStatus.cshtml` | `BuildOrderStatusPageModel()` |
| `MyOrdersPageViewModel` | 139 | `Views/Home/MyOrders.cshtml` | `BuildMyOrdersPageModel()` |
| `AboutPageViewModel` | 179 | `Views/Home/About.cshtml` | `HomeController.About()` |
| `ContactPageViewModel` | 188 | `Views/Home/Contact.cshtml` | `BuildContactPageModel()` |
| `CategoryCardViewModel` | 197 | category cards หน้าแรก | `BuildCategoryCards()` |
| `ProductCardViewModel` | 220 | product cards หลายหน้า | `MapProductCard()` |
| `CartLineViewModel` | 243 | line item ในตะกร้า | `BuildCartLines()` |
| `OrderReceiptLineViewModel` | 266 | receipt/order status | `BuildOrderStatusPageModel()` |
| `OrderProgressStepViewModel` | 279 | progress timeline | `BuildOrderStatusSteps()` |
| `PaymentOptionViewModel` | 290 | checkout payment options | `BuildPaymentOptions()` |
| `CheckoutBenefitViewModel` | 299 | promo/points benefits | promo/checkout helpers |
| `ServiceFeatureViewModel` | 310 | About values | `HomeController.About()` |
| `ContactInfoCardViewModel` | 319 | Contact cards | `BuildContactPageModel()` |
| `ContactFormViewModel` | 330 | contact form POST | `HomeController.Contact()` |
| `CartCheckoutViewModel` | 349 | checkout form POST | `HomeController.Checkout()` |

## HomeIndexViewModel

หน้าที่: model หลักของหน้าแรก

Property:

| Property | ความหมาย |
| --- | --- |
| `Categories` | หมวดสินค้าที่โชว์ในส่วนเลือกดูตามสไตล์ที่ชอบ |
| `Products` | สินค้าขายดีที่โชว์ในหน้าแรก |
| `NewArrivals` | สินค้ามาใหม่ที่ใช้กับ hero/new arrival |

Flow:

```text
GET /
  -> HomeController.Index()
  -> load products
  -> build sales lookup
  -> BuildCategoryCards(products, salesLookup)
  -> BuildBestSellingProducts(products, salesLookup, 8)
  -> BuildNewArrivalProducts(products)
  -> HomeIndexViewModel
  -> Views/Home/Index.cshtml
```

หมายเหตุ:

- `Products` ตอนนี้ตั้งใจให้เป็นสินค้าขายดี 8 ชิ้น
- `Categories` ไม่ใช่แค่ชื่อหมวด แต่รวม best seller ของแต่ละหมวดไว้ด้วยผ่าน `CategoryCardViewModel`

## ShopPageViewModel

หน้าที่: model ของหน้ารวมสินค้า

Property:

| Property | ความหมาย |
| --- | --- |
| `Products` | สินค้าทั้งหมดที่เปิดขาย |
| `Categories` | list ชื่อหมวดสำหรับ filter |

Flow:

```text
GET /Home/Shop
  -> HomeController.Shop()
  -> load active products
  -> MapProductCard()
  -> distinct categories
  -> ShopPageViewModel
  -> Views/Home/Shop.cshtml
```

## CartPageViewModel

หน้าที่: model ใหญ่ของหน้าตะกร้าและ checkout

กลุ่ม property:

| กลุ่ม | Property |
| --- | --- |
| รายการสินค้า | `Items`, `ItemCount`, `Subtotal` |
| วิธีจ่ายเงิน | `PaymentOptions`, `Checkout.PaymentMethod` |
| form checkout | `Checkout` |
| ค่าส่ง/ยอดรวม | `DeliveryFee`, `DiscountAmount`, `ShippingDiscountAmount`, `TotalSavings`, `Total` |
| promo | `AppliedPromoCode`, `AppliedPromoTitle`, `AppliedPromoDescription`, `PromoMessage`, `PromoMessageState`, `HasAppliedPromotion`, `HasPromoMessage` |
| points | `CurrentPoints`, `PointsEarned`, `PointsRedeemed`, `PointsDiscountAmount`, `MaxPointDiscountRedeem`, `PointDiscountRateLabel`, `ProjectedPointsBalance` |
| reward item | `PointsNeededForFreeItem`, `RewardPointCost`, `RewardQty`, `RewardProductName`, `CanRedeemFreeItem`, `WillRedeemFreeItem` |
| view state | `HasItems` |

Computed property:

```csharp
public bool WillRedeemFreeItem => Checkout.UsePointsReward && CanRedeemFreeItem;
public decimal TotalSavings => DiscountAmount + ShippingDiscountAmount;
public decimal Total => Math.Max(0, Subtotal + DeliveryFee - TotalSavings);
public bool HasItems => Items.Count > 0;
public bool HasAppliedPromotion => !string.IsNullOrWhiteSpace(AppliedPromoCode) && TotalSavings > 0;
public bool HasPromoMessage => !string.IsNullOrWhiteSpace(PromoMessage);
```

ความหมาย:

- `WillRedeemFreeItem` ใช้เช็กว่า user เลือกแลกของฟรีและมีแต้มพอจริง
- `TotalSavings` รวมส่วนลดสินค้าและค่าส่ง
- `Total` กันยอดติดลบด้วย `Math.Max(0, ...)`
- `HasItems` ช่วยให้ view สลับ empty state กับ cart list
- `HasAppliedPromotion` ใช้โชว์ promotion summary เฉพาะตอนมีโค้ดและมีส่วนลดจริง

Flow:

```text
GET /Home/Cart
  -> BuildCartPageModel()
  -> read cart session
  -> BuildCartLines()
  -> calculate subtotal/delivery/promo/points
  -> CartPageViewModel
  -> Views/Home/Cart.cshtml
```

POST checkout:

```text
POST /Home/Checkout
  -> bind CartCheckoutViewModel
  -> validate ModelState
  -> calculate discounts/rewards
  -> create order
  -> redirect OrderStatus
```

## CartCheckoutViewModel

หน้าที่: form ที่ user กรอกใน checkout

Property:

| Property | Validation | ใช้ทำอะไร |
| --- | --- | --- |
| `PromoCode` | ไม่มี required | รับโค้ดส่วนลด |
| `UsePointsReward` | bool | user เลือกใช้แต้มแลกขนมฟรีไหม |
| `PointsToRedeem` | `[Range(0, int.MaxValue)]` | จำนวนแต้มที่ใช้ลดราคา |
| `CustomerName` | `[Required]` | ชื่อผู้รับ |
| `PhoneNumber` | `[Required]`, `[Phone]` | เบอร์ผู้รับ |
| `DeliveryAddress` | `[Required]` | ที่อยู่จัดส่ง |
| `PaymentMethod` | `[Required]` | วิธีชำระเงิน default `promptpay` |
| `Notes` | ไม่มี required | note เพิ่มเติม |

เหตุผลที่ใช้ `set;`:

ASP.NET Core model binder ต้อง set ค่าจาก form POST กลับเข้ามาใน object นี้

## ProductCardViewModel

หน้าที่: card สินค้าแบบ reusable

Property:

| Property | ความหมาย |
| --- | --- |
| `ProductId` | id แบบ string เพราะ view ใช้ส่ง form/cart |
| `Name` | ชื่อสินค้า |
| `Category` | หมวดสินค้า |
| `Description` | รายละเอียดสั้น |
| `Price` | ราคา numeric สำหรับแสดง/ส่งต่อ |
| `OriginalPrice` | ราคาเดิมถ้ามี promotion |
| `Badge` | badge เช่น ขายดี/มาใหม่/หมดแล้ว |
| `ThemeKey` | key สำหรับเลือก style สี/ภาพ |
| `ImagePath` | path รูปสินค้า |
| `IsSoldOut` | ใช้ disable ปุ่มหรือแสดง sold out |

ใช้ใน:

- หน้าแรก
- shop
- best selling section
- new arrivals

## CategoryCardViewModel

หน้าที่: card หมวดสินค้าในหน้าแรก พร้อมสินค้าเด่นของหมวด

Property:

| Property | ความหมาย |
| --- | --- |
| `Title` | ชื่อหมวด |
| `Subtitle` | คำอธิบายหมวด |
| `ThemeKey` | theme CSS |
| `ImagePath` | รูปตัวแทนหมวด/สินค้าเด่น |
| `ItemCount` | จำนวนสินค้าในหมวด |
| `FeaturedProductName` | ชื่อสินค้าขายดีในหมวด |
| `FeaturedProductDescription` | รายละเอียดสินค้าเด่น |
| `FeaturedProductPriceLabel` | ราคาที่ format แล้ว |
| `FeaturedProductSalesLabel` | label ยอดขาย |
| `FeaturedProductBadge` | badge ของสินค้าเด่น |

Flow:

```text
products group by category
  -> pick best-selling product per category
  -> build CategoryCardViewModel
  -> HomeIndexViewModel.Categories
  -> Index.cshtml render cards
```

## OrderStatusPageViewModel

หน้าที่: หน้ารายละเอียดหลังสั่งซื้อ

กลุ่มข้อมูล:

- ข้อมูล order: `OrderNumber`, `CreatedAt`, `CustomerName`, `PhoneNumber`, `DeliveryAddress`, `Notes`
- payment: `PaymentMethodLabel`
- status: `CurrentStatusLabel`, `CurrentStatusDescription`, `StatusSteps`
- item receipt: `Items`
- discount/points: `AppliedBenefits`, `DiscountAmount`, `ShippingDiscountAmount`, `PointsEarned`, `PointsRedeemed`, `PointsBalanceAfter`
- totals: `Subtotal`, `DeliveryFee`, `TotalSavings`, `Total`

จุดสำคัญ:

- `StatusSteps` ทำให้ view ไม่ต้องรู้ rule ว่าสถานะไหน active แล้ว
- `AppliedBenefits` reuse รูปแบบเดียวกับ cart benefits
- `Total` คำนวณแบบเดียวกับ cart เพื่อให้ยอดหน้าสรุปตรงกัน

## MyOrdersPageViewModel และ MyOrderCardViewModel

`MyOrdersPageViewModel` คือหน้า list order ของ user

| Property | ความหมาย |
| --- | --- |
| `Orders` | รายการ order card |
| `OrderCount` | จำนวน order |
| `TotalSpendLabel` | ยอดใช้จ่ายรวมที่ format แล้ว |
| `LastOrderLabel` | label order ล่าสุด |
| `HasOrders` | ใช้แสดง empty state |

`MyOrderCardViewModel` คือ card แต่ละ order

จุดสำคัญ:

- มี `StatusKey` เพื่อให้ view ใส่ class ตามสถานะ
- มี `PromoLabel`, `PointsEarned`, `PointsRedeemed` เพื่อสรุป benefit ต่อ order
- ข้อมูลเป็น label สำเร็จรูป เช่น `TotalAmountLabel`, `ItemCountLabel`

## About และ Contact ViewModels

`AboutPageViewModel`:

- `StoryTitle`
- `StoryParagraphs`
- `Values` เป็น list ของ `ServiceFeatureViewModel`

`ContactPageViewModel`:

- `Form` คือ `ContactFormViewModel`
- `HeadingTitle`
- `ContactCards`

`ContactFormViewModel`:

| Property | Validation |
| --- | --- |
| `Name` | `[Required]` |
| `PhoneNumber` | `[Required]`, `[Phone]` |
| `Email` | `[Required]`, `[EmailAddress]` |
| `Subject` | optional |
| `Message` | `[Required]` |

## ข้อควรระวังเวลาแก้ Storefront ViewModel

- ถ้าเพิ่มข้อมูลใน product card ต้องแก้ `ProductCardViewModel`, mapper ใน controller และทุก view ที่ render card
- ถ้าเพิ่ม field checkout ต้องแก้ `CartCheckoutViewModel`, `Views/Home/Cart.cshtml`, `HomeController.Checkout()`, และ `BuildCartPageModel()`
- ถ้าแก้สูตรยอดรวม ต้องตรวจทั้ง `CartPageViewModel` และ `OrderStatusPageViewModel`
- ถ้าเพิ่ม promotion/points display ควรใช้ `CheckoutBenefitViewModel` แทนการสร้าง markup เฉพาะหน้า
- ถ้าข้อมูลเป็น label ที่ format แล้ว อย่านำกลับไปคำนวณ ให้ใช้ numeric property เช่น `Price`, `Subtotal`, `DiscountAmount`
