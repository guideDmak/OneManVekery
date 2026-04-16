# Views Notes

เอกสารหมวดนี้อธิบายหน้า `.cshtml` ในโฟลเดอร์ `Views` ว่าแต่ละหน้าใช้ `@model` อะไร อยู่ภายใต้ layout ไหน ส่ง form ไป action ไหน และมี `data-*` จุดไหนที่ JavaScript/CSS ใช้จับ

## View คืออะไรในโปรเจกต์นี้

View คือไฟล์ Razor `.cshtml` ที่รับข้อมูลจาก controller ผ่าน ViewModel แล้ว render HTML กลับไปให้ browser

```text
Controller action
  -> สร้าง ViewModel
  -> return View(model)
  -> Razor view อ่าน @Model
  -> Tag Helper สร้าง URL/form/input
  -> HTML ส่งกลับ browser
```

ถ้าเป็น form:

```text
Razor view
  -> <form asp-action="...">
  -> input asp-for="..."
  -> browser submit POST
  -> Controller action รับ ViewModel/form model
```

## โครงสร้าง Views

| โฟลเดอร์/ไฟล์ | หน้าที่ |
| --- | --- |
| `Views/_ViewStart.cshtml` | กำหนด layout default เป็น `_Layout` |
| `Views/_ViewImports.cshtml` | import namespace และเปิด MVC Tag Helpers |
| `Views/Home/` | หน้าร้าน เช่น หน้าแรก, shop, cart, checkout, order status |
| `Views/Account/` | login, register, register address, profile |
| `Views/Admin/` | หน้าหลังบ้านทั้งหมด |
| `Views/Shared/_Layout.cshtml` | layout หน้าร้านและ auth |
| `Views/Shared/_AdminLayout.cshtml` | layout หลังบ้าน |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | client validation scripts |
| `Views/Shared/Error.cshtml` | error page |

## จำนวนไฟล์หลัก

| กลุ่ม | ไฟล์ |
| --- | --- |
| Home | `Index`, `Shop`, `Cart`, `OrderStatus`, `MyOrders`, `About`, `Contact`, `Privacy` |
| Account | `Login`, `Register`, `RegisterAddress`, `Profile` |
| Admin | `Index`, `Orders`, `Items`, `Products`, `Codes`, `Accounts`, `Staff`, `Profile` |
| Shared | `_Layout`, `_AdminLayout`, `_ValidationScriptsPartial`, `Error` |

## วิธีอ่านโน้ตชุดนี้

| ไฟล์ | อ่านเมื่อ |
| --- | --- |
| `01-shared-layouts-and-razor-basics.md` | ต้องการเข้าใจ layout, Tag Helpers, ViewData, RenderBody/RenderSection |
| `02-home-storefront-views.md` | ต้องการเข้าใจหน้า Home/Shop/Cart/OrderStatus/MyOrders/About/Contact |
| `03-account-views.md` | ต้องการเข้าใจ Login/Register/Profile และ modal profile/address |
| `04-admin-dashboard-products-items.md` | ต้องการเข้าใจ Dashboard, Products, Items, Staff, Admin Profile |
| `05-admin-orders-codes-accounts.md` | ต้องการเข้าใจ Orders, Codes, Accounts และ modal/form หนักๆ |
| `06-view-js-form-hooks.md` | ต้องการแก้ `data-*`, form binding, modal, validation, JavaScript hooks |

## แผนที่ View กับ ViewModel

| View | `@model` |
| --- | --- |
| `Views/Home/Index.cshtml` | `HomeIndexViewModel` |
| `Views/Home/Shop.cshtml` | `ShopPageViewModel` |
| `Views/Home/Cart.cshtml` | `CartPageViewModel` |
| `Views/Home/OrderStatus.cshtml` | `OrderStatusPageViewModel` |
| `Views/Home/MyOrders.cshtml` | `MyOrdersPageViewModel` |
| `Views/Home/About.cshtml` | `AboutPageViewModel` |
| `Views/Home/Contact.cshtml` | `ContactPageViewModel` |
| `Views/Account/Login.cshtml` | `LoginViewModel` |
| `Views/Account/Register.cshtml` | `RegisterViewModel` |
| `Views/Account/RegisterAddress.cshtml` | `RegisterAddressViewModel` |
| `Views/Account/Profile.cshtml` | `AccountProfileViewModel` |
| `Views/Admin/Index.cshtml` | `AdminDashboardViewModel` |
| `Views/Admin/Orders.cshtml` | `AdminOrdersViewModel` |
| `Views/Admin/Items.cshtml` | `AdminItemsPageViewModel` |
| `Views/Admin/Products.cshtml` | `AdminProductsViewModel` |
| `Views/Admin/Codes.cshtml` | `AdminCodesViewModel` |
| `Views/Admin/Accounts.cshtml` | `AdminAccountsViewModel` |
| `Views/Admin/Staff.cshtml` | `AdminStaffViewModel` |
| `Views/Admin/Profile.cshtml` | `AdminProfileViewModel` |

## Pattern หลักที่ Views ใช้

### 1. `@model`

กำหนด type ของ `Model` ในหน้า

```csharp
@model CartPageViewModel
```

ผลคือในไฟล์นั้นใช้ `Model.Items`, `Model.Total`, `Model.Checkout.CustomerName` ได้แบบ strongly typed

### 2. `ViewData["Title"]`

กำหนด title ของหน้า แล้ว layout เอาไปใช้ใน `<title>` และ header บางจุด

```csharp
ViewData["Title"] = "ตะกร้า";
```

### 3. Layout

หน้าร้านใช้ default `_Layout`

```csharp
Views/_ViewStart.cshtml
  -> Layout = "_Layout";
```

หลังบ้าน override เป็น `_AdminLayout`

```csharp
Layout = "_AdminLayout";
```

Auth pages ใช้ `_Layout` เหมือนกัน แต่ตั้ง:

```csharp
ViewData["HideChrome"] = true;
```

เพื่อซ่อน navbar/footer แล้วใช้หน้า auth แบบเต็มจอ

### 4. Tag Helpers

โปรเจกต์เปิด MVC Tag Helpers ใน `_ViewImports.cshtml`

```csharp
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

จึงใช้ syntax แบบนี้ได้:

```html
<form asp-action="Checkout" method="post">
<input asp-for="Checkout.CustomerName" />
<span asp-validation-for="Checkout.CustomerName"></span>
<a asp-controller="Home" asp-action="Shop">สินค้า</a>
```

### 5. `data-*`

ใช้เป็นสัญญาระหว่าง HTML กับ `wwwroot/js/site.js`

ตัวอย่าง:

```html
<section data-admin-items data-active-modal="@Model.ActiveModal">
<form data-stock-adjust-form>
<input data-stock-amount />
```

ถ้าเปลี่ยนชื่อ `data-*` ต้องแก้ JavaScript ที่ query selector ด้วย

## ข้อควรระวัง

- อย่าเปลี่ยน `asp-for` โดยไม่แก้ ViewModel/POST action
- อย่าเปลี่ยน `asp-action` โดยไม่ตรวจ controller action
- อย่าเปลี่ยน `data-*` โดยไม่ตรวจ `wwwroot/js/site.js`
- หน้า admin ที่มี modal ต้องรักษา `data-active-modal`
- form POST ต้องมี validation summary/message ที่ตรงกับ field
- ถ้า input อยู่ใน nested form เช่น `AddForm.Name` ต้องให้ prefix ตรงกับ `[Bind(Prefix = "AddForm")]`
- layout `_Layout.cshtml` มี query database สำหรับ points ใน navbar ถ้าแก้ session/role ต้องระวังส่วนนี้
