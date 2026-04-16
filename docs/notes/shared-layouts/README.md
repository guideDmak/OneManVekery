# Shared Layouts and Frontend Glue Notes

เอกสารนี้อธิบาย layout กลางและ JavaScript ที่เชื่อม form/modal/search/filter ระหว่างหลายหน้า

## ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Views/Shared/_Layout.cshtml` | layout หน้าร้านและ account/auth |
| `Views/Shared/_AdminLayout.cshtml` | layout หลังบ้าน |
| `Views/Shared/Error.cshtml` | หน้า error |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | include validation scripts |
| `wwwroot/js/site.js` | JavaScript หลักของทั้ง storefront และ admin |
| `wwwroot/css/site.css` | style รวม |

## Storefront layout

ไฟล์: `Views/Shared/_Layout.cshtml`

### ส่วนเตรียมข้อมูล

ตำแหน่ง: บรรทัด 1-75

สิ่งที่ layout ทำก่อน render HTML:

1. inject `StorefrontContentOptions`
2. inject `OneManVekeryDBContext`
3. อ่าน `ViewData["HideChrome"]`
4. อ่าน route ปัจจุบัน เพื่อ active nav
5. อ่าน cart จาก session
6. parse cart JSON เพื่อคำนวณจำนวนสินค้า
7. อ่าน content สำหรับ footer
8. อ่าน session account id/name/role
9. ตรวจว่าเป็น storefront user หรือ admin
10. ถ้าเป็น storefront user อ่าน current points จาก database

### `HideChrome`

`hideChrome` ใช้ซ่อน:

- header
- footer
- logout modal
- login prompt modal

เหมาะกับหน้า login/register ที่ต้องเป็น auth screen แบบเต็มหน้า

### Navbar

ตำแหน่ง: บรรทัด 92-208

ลักษณะ:

- ถ้า `hideChrome` เป็น false จึง render navbar
- link active ตาม `currentAction`
- ถ้า user เป็น storefront user จะแสดง cart, my orders, points
- ถ้า signed in จะแสดง avatar profile และ logout icon
- ถ้ายังไม่ signed in จะแสดง login/register buttons

### Cart count

ตำแหน่ง: บรรทัด 12-28 และ 127-140

Flow:

```text
read session "one-man-vekery-cart"
  -> JsonSerializer.Deserialize<List<CartSessionItem>>
  -> if parse fail remove session key
  -> cartCount = sum Quantity
  -> render .bakery-cart-count
```

### Points pill

ตำแหน่ง: บรรทัด 65-74 และ 158-160

เฉพาะ storefront user:

- query `LoyaltyWallets`
- read current points
- render `P {currentPoints}` บน navbar

### TempData notice

ตำแหน่ง: บรรทัด 210-217

ถ้า controller set `TempData["SiteNotice"]` layout จะ render notice อัตโนมัติ

`site.js` ใช้ `data-site-notice-dismiss-ms="5000"` เพื่อ auto dismiss

### Footer

ตำแหน่ง: บรรทัด 223-331

footer ใช้:

- brand tagline จาก config
- footer address จาก config
- feature pills จาก config
- contact cards จาก config
- help items จาก config
- links ตามสถานะ login

### Shared modals

ตำแหน่ง: บรรทัด 333-382

| Modal | ใช้ทำอะไร |
| --- | --- |
| `siteConfirmModal` | confirm logout หรือ action ที่มี `data-confirm-message` |
| `siteLoginPromptModal` | เตือนให้ login ก่อน add-to-cart |

## Admin layout

ไฟล์: `Views/Shared/_AdminLayout.cshtml`

### เตรียมข้อมูล

ตำแหน่ง: บรรทัด 1-8

อ่านจาก `ViewData` ที่ `AdminController.OnActionExecuting()` set ไว้:

- `AdminSection`
- `AdminDateRange`
- `AdminSignedInName`
- `AdminSignedInRole`
- `AdminSignedInRoleKey`

จากนั้นคำนวณ `canManageStaffDirectory` เพื่อซ่อน/แสดงเมนู Staff

### Sidebar

ตำแหน่ง: บรรทัด 21-77

เมนู:

- Dashboard
- Orders
- Staff เฉพาะ role ที่มีสิทธิ์
- Products
- Promotions/Codes
- Items
- Accounts
- Profile
- Logout

active state ใช้ `adminSection`

### Topbar

ตำแหน่ง: บรรทัด 85-98

แสดง:

- kicker `One Man Vekery Admin`
- page title จาก `ViewData["Title"]`
- date pill ถ้ามี
- link กลับหน้าร้าน

### Body

ตำแหน่ง: บรรทัด 100-112

- `RenderBody()` แสดง view เฉพาะหน้า
- โหลด jQuery, Bootstrap, `site.js`
- เปิด optional Scripts section

## `site.js` map

ไฟล์: `wwwroot/js/site.js`

| Feature | Selector | หน้าที่ |
| --- | --- | --- |
| storefront shop filter | `data-product-search` | filter card ใน `/Home/Shop` |
| site confirm modal | `a[data-confirm-message]` | confirm logout |
| login prompt | `form[data-login-required-form]` | block add-to-cart ถ้ายังไม่ login |
| checkout review | `data-checkout-form` | เปิด modal review ก่อน submit checkout |
| admin items | `data-admin-items` | filter/sort item cards, stock AJAX, image picker |
| admin products | `data-admin-products` | publish/hide product AJAX |
| admin accounts | `data-admin-accounts` | search/filter/sort account table |
| admin codes | `data-admin-codes` | promo code uppercase, hints |
| admin orders | `data-admin-orders` | add order modal, combobox, line items |

## Login-required form flow

```text
form has data-login-required-form
  -> body has data-storefront-user=false
  -> site.js intercepts submit
  -> preventDefault()
  -> show siteLoginPromptModal
  -> user can go to Login
```

ใช้กับ product cards เพื่อไม่ให้ anonymous user submit add-to-cart แล้วโดน redirect แบบงง ๆ

## Confirm modal flow

```text
link has data-confirm-message
  -> site.js intercepts click
  -> copy href into confirm action
  -> set title/message/button label
  -> show modal
  -> user confirms
  -> browser navigates to original href
```

ใช้กับ logout ใน layout

## Checkout review flow

```text
checkout form submit
  -> if submit came from preview promo/points: allow normal submit
  -> otherwise prevent submit
  -> copy current form fields into review modal
  -> show modal
  -> confirm button submits form
```

ช่วยให้ user เห็นรายการ/ราคา/ที่อยู่ก่อนสร้าง order จริง

## Admin AJAX conventions

หน้า admin บางจุดรองรับ AJAX:

| หน้า | Action | Response |
| --- | --- | --- |
| Items | `AdjustItemStock` | JSON stock payload |
| Items | `AddCategory` | JSON categoryName/message |
| Products | `SetProductVisibility` | JSON product visibility payload |

Pattern:

1. view ใส่ `data-*` ให้ JS หา element ได้
2. JS submit form ด้วย fetch
3. controller ตรวจ `IsAjaxRequest()`
4. controller return JSON
5. JS update DOM และ notice

## ข้อควรระวังเวลาแก้ layout/JS

- ถ้าเปลี่ยนชื่อ `data-*` ต้องแก้ทั้ง view และ `site.js`
- ถ้าเปลี่ยน session key cart/account ต้องแก้ layout ด้วย
- `_Layout.cshtml` inject DbContext เพื่ออ่าน points ถ้า performance มีปัญหาในอนาคต ควรย้ายเป็น view component หรือ cached service
- `_AdminLayout.cshtml` พึ่ง `ViewData` จาก `AdminController.OnActionExecuting()` ถ้าเพิ่ม admin controller ใหม่ ต้อง set ค่าเหมือนกันหรือใช้ base controller
- modal ids ต้องไม่ซ้ำกันใน page เดียว
- form ที่ใช้ POST ควรมี anti-forgery token เสมอ

