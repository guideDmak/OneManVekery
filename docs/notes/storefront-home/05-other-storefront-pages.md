# 05 Other Storefront Pages

ไฟล์นี้เติมรายละเอียดหน้า storefront อื่น ๆ ที่ไม่ใช่หน้าแรกและหน้าสินค้า ได้แก่ About, Contact, Cart, MyOrders และ OrderStatus

## ไฟล์ที่เกี่ยวข้อง

| หน้า | View | Action | ViewModel |
| --- | --- | --- | --- |
| About | `Views/Home/About.cshtml` | `HomeController.About()` | `AboutPageViewModel` |
| Contact | `Views/Home/Contact.cshtml` | `HomeController.Contact()` GET/POST | `ContactPageViewModel`, `ContactFormViewModel` |
| Cart | `Views/Home/Cart.cshtml` | `Cart`, `ChangeCartQuantity`, `RemoveFromCart`, `Checkout`, `ApplyPromoCode` | `CartPageViewModel` |
| My Orders | `Views/Home/MyOrders.cshtml` | `MyOrders()` | `MyOrdersPageViewModel` |
| Order Status | `Views/Home/OrderStatus.cshtml` | `OrderStatus(orderNumber)` | `OrderStatusPageViewModel` |

## About page

### Flow

```text
GET /Home/About
  -> HomeController.About()
  -> read _storefrontContent.About
  -> filter empty story paragraphs
  -> map values into ServiceFeatureViewModel
  -> Views/Home/About.cshtml
```

### Controller

ตำแหน่ง: `Controllers/HomeController.cs:293-315`

| บรรทัด | อธิบาย |
| --- | --- |
| 293 | action รับ GET |
| 296 | อ่าน config `About` ถ้าไม่มีให้ใช้ object ว่าง |
| 297 | ดึง story paragraphs |
| 298 | ดึง values |
| 300 | return view พร้อม `AboutPageViewModel` |
| 302 | set title เรื่องร้าน |
| 303-305 | ตัด paragraph ว่างออก |
| 306-314 | map values เป็น `ServiceFeatureViewModel` |

### View

ตำแหน่ง: `Views/Home/About.cshtml`

| ส่วน | อธิบาย |
| --- | --- |
| `@model AboutPageViewModel` | view รับข้อมูลเรื่องร้าน |
| `about-story-section` | แสดงหัวเรื่องและ paragraph ของร้าน |
| `foreach Model.StoryParagraphs` | render ทุก paragraph ที่ controller filter แล้ว |
| `about-values-section` | แสดง value/feature ของร้าน |
| `foreach Model.Values` | render card แต่ละ value |
| ปุ่ม `asp-action="Contact"` | link ไปหน้าติดต่อร้าน |

## Contact page

### Flow GET

```text
GET /Home/Contact
  -> HomeController.Contact()
  -> BuildContactPageModel()
     -> read _storefrontContent.Contact
     -> ApplySignedInContactDefaults()
  -> Views/Home/Contact.cshtml
```

### Flow POST

```text
POST /Home/Contact
  -> validate ContactFormViewModel
  -> if invalid: return Contact view with same form
  -> create ContactMessage
  -> trim text fields
  -> Status = "new"
  -> CreatedAt = DateTime.UtcNow
  -> SaveChanges()
  -> TempData notice
  -> redirect Contact
```

### Controller GET/POST

ตำแหน่ง: `Controllers/HomeController.cs:318-347`

| บรรทัด | อธิบาย |
| --- | --- |
| 318-321 | GET แค่ return view จาก `BuildContactPageModel()` |
| 324-326 | POST รับ `ContactFormViewModel` และตรวจ anti-forgery |
| 328-331 | ถ้า validation fail ให้กลับ view เดิมพร้อม form ที่ user กรอก |
| 333-342 | สร้าง `ContactMessage` ลง database |
| 343 | save changes |
| 345-346 | แจ้งสำเร็จและ redirect |

### `BuildContactPageModel()`

ตำแหน่ง: `Controllers/Home/PageModels.cs:15-34`

หน้าที่:

- ดึง config contact
- สร้าง form model
- ถ้า user login อยู่ เติมชื่อ อีเมล เบอร์โทรให้ form
- map contact cards สำหรับ view

### View

ตำแหน่ง: `Views/Home/Contact.cshtml`

| ส่วน | อธิบาย |
| --- | --- |
| `@model ContactPageViewModel` | รับ heading, form, contact cards |
| `contact-page-section` | layout หลักของหน้า contact |
| `foreach Model.ContactCards` | แสดงช่องทางติดต่อ |
| `<form asp-action="Contact" method="post">` | form ส่ง POST กลับ action เดิม |
| `asp-for="Form.Name"` และ field อื่น | bind กับ `ContactFormViewModel` ที่อยู่ใน page model |
| validation spans | แสดง error จาก DataAnnotations/ModelState |

## Cart page

### Flow เปิดหน้า cart

```text
GET /Home/Cart
  -> login guard
  -> BuildCartPageModel()
     -> GetCartItems()
     -> ApplySignedInCheckoutDefaults()
     -> BuildPricingSummary()
     -> map items / benefits / payment options
  -> Views/Home/Cart.cshtml
```

### View sections

ตำแหน่ง: `Views/Home/Cart.cshtml`

| ช่วง | ส่วน | อธิบาย |
| --- | --- | --- |
| 7 | `cart-page-section` | container หลัก |
| 9-18 | empty cart | ถ้าไม่มี item แสดงปุ่มกลับไปเลือกสินค้า |
| 33-95 | cart item list | loop `Model.Items` และแสดงสินค้าในตะกร้า |
| 61-78 | quantity forms | ปุ่มลด/เพิ่มจำนวนเรียก `ChangeCartQuantity` |
| 83-90 | remove form | ลบสินค้าเรียก `RemoveFromCart` |
| 101-146 | summary/benefits | subtotal, delivery, discount, promotion benefits |
| 149-281 | checkout form | form หลักเรียก `Checkout` |
| 166-167 | promo field | field promo และปุ่ม `ApplyPromoCode` |
| 214-215 | points field | field ใช้แต้มและปุ่ม preview |
| 242-252 | payment options | radio options จาก `PaymentOptions` |
| 258-276 | customer fields | ชื่อ เบอร์ ที่อยู่ note |
| 290-423 | review modal | modal สรุปก่อนยืนยัน checkout |

### Form action สำคัญ

| Form | Action | หน้าที่ |
| --- | --- | --- |
| quantity minus/plus | `ChangeCartQuantity` | เพิ่ม/ลดจำนวนใน session |
| remove | `RemoveFromCart` | ลบสินค้าออกจาก session |
| promo preview | `ApplyPromoCode` | preview promo/points โดยยังไม่ checkout |
| checkout submit | `Checkout` | สร้าง order จริง |

### `data-*` ที่ JS ใช้

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-checkout-form` | ระบุ form checkout หลัก |
| `data-checkout-preview-submit` | ปุ่ม preview promo/points ไม่เปิด review modal |
| `data-checkout-submit` | ปุ่ม checkout ที่ต้องเปิด review modal |
| `data-checkout-review-modal` | modal review ก่อน submit จริง |
| `data-checkout-review-confirm` | ปุ่มยืนยันใน modal |
| `data-checkout-promo-field` | sync promo ไป modal |
| `data-checkout-points-field` | sync points ไป modal |
| `data-checkout-payment-field` | อ่าน payment method ที่เลือก |
| `data-checkout-customer-field` | sync customer name |
| `data-checkout-phone-field` | sync phone |
| `data-checkout-address-field` | sync address |
| `data-checkout-notes-field` | sync note |

### Process ตอนกด checkout

```text
user กด "ยืนยันการชำระเงิน"
  -> site.js intercept submit
  -> ถ้าไม่ใช่ปุ่ม preview:
       update checkout review modal
       show modal
       block submit ชั่วคราว
  -> user กด "ใช่"
       set allowSubmit = true
       submit form
  -> HomeController.Checkout()
```

## MyOrders page

### Flow

```text
GET /Home/MyOrders
  -> login guard
  -> BuildMyOrdersPageModel()
     -> get current account id
     -> query orders for user
     -> order by CreatedAt desc
     -> map each order with MapMyOrderCard()
  -> Views/Home/MyOrders.cshtml
```

### View

ตำแหน่ง: `Views/Home/MyOrders.cshtml`

| ส่วน | อธิบาย |
| --- | --- |
| page banner | breadcrumb และหัวข้อ |
| summary cards | จำนวน order, ยอดใช้จ่ายรวม, order ล่าสุด |
| empty state | ถ้า user ยังไม่มี order |
| order cards | loop `Model.Orders` |
| promo label | แสดงเฉพาะเมื่อ `PromoLabel` มีค่า |
| link รายละเอียด | ไป `OrderStatus` พร้อม `orderNumber` |

### `MapMyOrderCard()`

ตำแหน่ง: `Controllers/Home/PageModels.cs:205-235`

หน้าที่:

- resolve status label/description
- สร้าง product summary จาก item แรกและจำนวน item ที่เหลือ
- sum จำนวนสินค้า
- format total amount
- format payment method
- ใส่ promo/points summary

## OrderStatus page

### Flow

```text
GET /Home/OrderStatus?orderNumber=...
  -> login guard
  -> GetOrderForCurrentUser(orderNumber)
     -> include OrderItems
     -> include OrderPromotions
     -> include PromoCode
     -> filter UserId = current user
  -> BuildOrderStatusPageModel(order)
  -> Views/Home/OrderStatus.cshtml
```

### View

ตำแหน่ง: `Views/Home/OrderStatus.cshtml`

| ส่วน | อธิบาย |
| --- | --- |
| page banner | เลข order และ breadcrumb |
| success hero | แสดงสถานะหลักของ order |
| status steps | loop `Model.StatusSteps` |
| receipt items | loop `Model.Items` |
| bill summary | subtotal, delivery, discount, total |
| applied benefits | promotion/points ที่ใช้ |
| delivery/payment details | ข้อมูลลูกค้าและวิธีชำระเงิน |
| action buttons | กลับ MyOrders หรือ Shop |

### `BuildOrderStatusPageModel()`

ตำแหน่ง: `Controllers/Home/PageModels.cs:132-177`

หน้าที่:

- resolve current order status
- หา latest points ledger
- หา promo benefit ที่เกี่ยวข้อง
- map order items เป็น receipt lines
- build status progress steps
- map order promotions เป็น benefit cards
- คำนวณ total จาก subtotal/delivery/discount

## ข้อควรระวังเวลาแก้ storefront pages เหล่านี้

- Cart/Checkout ต้องรักษา `AntiForgeryToken` ทุก form POST
- `ApplyPromoCode` ต้องไม่สร้าง order
- `Checkout` ต้อง validate stock ซ้ำก่อนสร้าง order
- MyOrders/OrderStatus ต้อง filter ด้วย current user เสมอ
- Contact POST ต้อง trim input ก่อน save
- Modal ที่เปิดหลัง validation fail ใช้ `ActiveModal` หรือ data attribute จาก model อย่าลบโดยไม่ตั้ง fallback

