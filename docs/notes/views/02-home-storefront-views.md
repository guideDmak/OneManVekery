# Home and Storefront Views

โฟลเดอร์หลัก: `Views/Home/`

กลุ่มนี้คือหน้าร้านทั้งหมด ตั้งแต่หน้าแรก เลือกสินค้า ตะกร้า checkout สถานะคำสั่งซื้อ ประวัติคำสั่งซื้อ และหน้าข้อมูลร้าน

## แผนที่ไฟล์

| View | Model | Action | หน้าที่ |
| --- | --- | --- | --- |
| `Index.cshtml` | `HomeIndexViewModel` | `HomeController.Index()` | หน้าแรก, hero, หมวด, สินค้าขายดี |
| `Shop.cshtml` | `ShopPageViewModel` | `HomeController.Shop()` | catalog, search/filter, add to cart |
| `Cart.cshtml` | `CartPageViewModel` | `HomeController.Cart()`, checkout actions | ตะกร้า, promo, points, checkout |
| `OrderStatus.cshtml` | `OrderStatusPageViewModel` | `HomeController.OrderStatus()` | หน้าสรุปคำสั่งซื้อ |
| `MyOrders.cshtml` | `MyOrdersPageViewModel` | `HomeController.MyOrders()` | ประวัติคำสั่งซื้อ user |
| `About.cshtml` | `AboutPageViewModel` | `HomeController.About()` | เรื่องราวร้านและ values |
| `Contact.cshtml` | `ContactPageViewModel` | `HomeController.Contact()` | ข้อมูลติดต่อและ contact form |
| `Privacy.cshtml` | ไม่มี model | `HomeController.Privacy()` | หน้า privacy template |

## `Index.cshtml`

หน้าที่:

- render hero/new arrival carousel
- render หมวดสินค้าและสินค้าเด่นต่อหมวด
- render สินค้าขายดี
- มี form add to cart บน product cards

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `Model.NewArrivals` | สร้าง hero carousel |
| `Model.Categories` | card หมวดสินค้า |
| `Model.Products` | section สินค้าขายดี |

โครงหลัก:

```text
HomeIndexViewModel
  -> hero section
     -> NewArrivals carousel
  -> category/range section
     -> Categories foreach
  -> recommended/best-selling products
     -> Products foreach
```

Bootstrap carousel:

```html
id="newArrivalCarousel"
data-bs-ride="carousel"
data-bs-interval="4200"
```

form สำคัญ:

```html
<form asp-action="AddToCart" method="post" class="store-cart-form" data-login-required-form>
```

ความหมาย:

- submit ไป `HomeController.AddToCart`
- `data-login-required-form` ให้ `site.js` เช็กว่า user login เป็น storefront user หรือยัง
- ถ้ายังไม่ login จะเปิด login prompt modal แทนการ submit

## `Shop.cshtml`

หน้าที่:

- แสดงสินค้าทั้งหมด
- search/filter ฝั่ง client
- add to cart
- empty state เมื่อ filter แล้วไม่เจอสินค้า

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `Model.Products` | catalog cards |
| `Model.Categories` | select filter หมวด |

`data-*` สำคัญ:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-product-search` | root ของ search panel |
| `data-results-label` | แสดงจำนวนสินค้าที่ match |
| `data-search-input` | input ค้นหา |
| `data-category-input` | select หมวด |
| `data-product-list` | container ของ product cards |
| `data-product-card` | card ที่ JS show/hide |
| `data-name` | text สำหรับ search |
| `data-category` | category สำหรับ filter |
| `data-empty-state` | แสดงเมื่อไม่มีผลลัพธ์ |
| `data-login-required-form` | guard add to cart |

Flow search/filter:

```text
user พิมพ์ search หรือเลือก category
  -> site.js อ่าน data-product-card
  -> เทียบ data-name/data-category
  -> show/hide cards
  -> update data-results-label
  -> toggle data-empty-state
```

## `Cart.cshtml`

หน้าที่:

- แสดงสินค้าในตะกร้า
- เพิ่ม/ลดจำนวน
- remove item
- ใส่ promo code
- ใช้ points ลดราคา/แลกของ
- กรอก checkout form
- เปิด review modal ก่อน submit จริง

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `Model.Items` | รายการสินค้าในตะกร้า |
| `Model.HasItems` | เลือกระหว่าง empty state กับ cart content |
| `Model.Checkout` | form checkout |
| `Model.PaymentOptions` | radio payment options |
| `Model.AppliedBenefits` | promotion/points benefit |
| `Model.Total`, `Model.TotalSavings` | summary ยอดเงิน |

forms:

| Form | Action | หน้าที่ |
| --- | --- | --- |
| quantity decrement | `ChangeCartQuantity` | ลดจำนวน |
| quantity increment | `ChangeCartQuantity` | เพิ่มจำนวน |
| remove | `RemoveFromCart` | เอาออกจากตะกร้า |
| checkout | `Checkout` | ยืนยัน order |
| promo preview button | `ApplyPromoCode` | ใช้โค้ดโดยไม่ validate field checkout ทั้งหมด |
| points preview button | `ApplyPromoCode` | update points preview |

จุดสำคัญของ `ApplyPromoCode`:

```html
formnovalidate
name="checkoutPreviewAction"
value="promo" หรือ "points"
```

เหตุผล:

- user อาจแค่ลองโค้ดหรือแต้ม ยังไม่ได้กรอกชื่อ/ที่อยู่ครบ
- `formnovalidate` กัน browser validation ของ checkout fields
- controller ใช้ `checkoutPreviewAction` แยกว่ากด promo หรือ points

`data-*` checkout สำคัญ:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-checkout-form` | root form checkout |
| `data-checkout-promo-field` | promo code input |
| `data-checkout-preview-submit` | ปุ่ม preview promo/points |
| `data-checkout-points-field` | points input |
| `data-checkout-payment-field` | payment radio |
| `data-checkout-customer-field` | ชื่อลูกค้า |
| `data-checkout-phone-field` | เบอร์ |
| `data-checkout-address-field` | ที่อยู่ |
| `data-checkout-notes-field` | note |
| `data-checkout-submit` | ปุ่ม submit checkout |
| `data-checkout-review-modal` | modal review |
| `data-checkout-review-confirm` | ปุ่มยืนยันใน modal |

Flow checkout review:

```text
user กด ยืนยันการชำระเงิน
  -> site.js intercept
  -> อ่าน field checkout
  -> เติมข้อมูลลง review modal
  -> เปิด modal
  -> user กดยืนยัน
  -> submit form จริงไป Checkout
```

ข้อควรระวัง:

- input ทุกตัวใช้ `asp-for="Checkout.*"` ต้องตรงกับ `CartCheckoutViewModel`
- ถ้าเพิ่ม checkout field ต้องเพิ่มทั้ง view, ViewModel, controller และ review modal/JS ถ้าต้องโชว์ก่อนยืนยัน
- ถ้าปรับ promo/points flow ต้องระวัง `formnovalidate`

## `OrderStatus.cshtml`

หน้าที่:

- แสดงสรุป order หลัง checkout
- แสดง progress/status step
- แสดง receipt items
- แสดง discount/points benefit
- link ไป My Orders และ Shop

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `OrderNumber`, `CreatedAt` | header/order identity |
| `CurrentStatusLabel`, `CurrentStatusDescription` | current status |
| `StatusSteps` | timeline/progress |
| `Items` | receipt line |
| `AppliedBenefits` | promotion/points |
| `Subtotal`, `DeliveryFee`, `TotalSavings`, `Total` | summary |

Flow:

```text
GET /Home/OrderStatus?orderNumber=...
  -> controller load order
  -> BuildOrderStatusPageModel(order)
  -> view render receipt/progress
```

## `MyOrders.cshtml`

หน้าที่:

- แสดง order history ของ user
- แสดง summary เช่นจำนวน order, total spend, last order
- link ไป order detail

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `Orders` | order cards |
| `HasOrders` | empty state |
| `OrderCount` | summary |
| `TotalSpendLabel` | summary ยอดรวม |
| `LastOrderLabel` | summary order ล่าสุด |

link detail:

```html
asp-action="OrderStatus"
asp-route-orderNumber="@order.OrderNumber"
```

## `About.cshtml`

หน้าที่:

- แสดง story ของร้าน
- แสดง values/service features
- link ไป Contact

จุดข้อมูล:

| Model property | ใช้ทำอะไร |
| --- | --- |
| `StoryTitle` | heading |
| `StoryParagraphs` | paragraphs |
| `Values` | feature/value cards |

## `Contact.cshtml`

หน้าที่:

- แสดง contact cards
- แสดง contact form

form:

```html
<form asp-action="Contact" method="post" class="contact-form-grid">
```

ใช้ `ContactFormViewModel` ผ่าน:

```text
ContactPageViewModel.Form
```

จุดสำคัญ:

- include `_ValidationScriptsPartial`
- ถ้า validation fail controller ส่ง `ContactPageViewModel` ที่มี `Form` เดิมกลับมา

## `Privacy.cshtml`

หน้า template สั้นๆ ยังไม่มี model และไม่ได้อยู่ใน flow หลักของร้าน

## ข้อควรระวังเวลาแก้ Home views

- `AddToCart` forms บน `Index` และ `Shop` ต้องมี `data-login-required-form`
- product card ต้องรักษา hidden `productId` ที่ controller ใช้
- `Shop` filter พึ่ง `data-name` และ `data-category`
- `Cart` เป็นหน้าที่เชื่อมหลาย action มากที่สุด ต้องระวังปุ่มใน form ที่ override `asp-action`
- checkout fields ต้องตรงกับ `CartCheckoutViewModel.Checkout`
- ถ้าเปลี่ยนชื่อ class/layout grid ต้องตรวจ CSS ใน `wwwroot/css/site.css`
