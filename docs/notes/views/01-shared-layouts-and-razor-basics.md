# Shared Layouts and Razor Basics

ไฟล์หลัก:

- `Views/_ViewStart.cshtml`
- `Views/_ViewImports.cshtml`
- `Views/Shared/_Layout.cshtml`
- `Views/Shared/_AdminLayout.cshtml`
- `Views/Shared/_ValidationScriptsPartial.cshtml`
- `Views/Shared/Error.cshtml`

## `_ViewStart.cshtml`

```csharp
@{
    Layout = "_Layout";
}
```

หน้าที่:

- กำหนด layout default ของทุก view เป็น `Views/Shared/_Layout.cshtml`
- view ที่ไม่ override `Layout` จะถูกห่อด้วย `_Layout`
- หน้า Home และ Account ใช้ default นี้

ข้อยกเว้น:

- หน้า Admin ตั้ง `Layout = "_AdminLayout";` เองในแต่ละไฟล์

## `_ViewImports.cshtml`

```csharp
@using OneManVekery
@using OneManVekery.Models
@using OneManVekery.ViewModel
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

หน้าที่:

- ทำให้ view ใช้ class จาก `OneManVekery.Models` และ `OneManVekery.ViewModel` ได้โดยไม่ต้องเขียน namespace เต็ม
- เปิด Tag Helpers เช่น `asp-action`, `asp-controller`, `asp-for`, `asp-validation-for`, `asp-route-*`

ผล:

```csharp
@model CartPageViewModel
```

ใช้ได้ทันที ไม่ต้องเขียน:

```csharp
@model OneManVekery.ViewModel.CartPageViewModel
```

## `_Layout.cshtml`

หน้าที่: layout หลักของหน้าร้านและ auth pages

ส่วนสำคัญ:

| ส่วน | หน้าที่ |
| --- | --- |
| top Razor block | อ่าน content options, session cart, session account, points |
| `<head>` | โหลด fonts, Bootstrap, `site.css`, generated CSS |
| header/navbar | เมนูหน้าร้าน, cart, orders, profile, logout/login |
| `TempData["SiteNotice"]` | toast/notice กลางหน้า |
| `RenderBody()` | จุดที่ view จริงถูกแทรก |
| footer | footer content จาก options |
| confirm modal | modal logout confirmation |
| login prompt modal | modal บังคับ login ก่อน add to cart |
| scripts | jQuery, Bootstrap, `site.js`, `RenderSectionAsync("Scripts")` |

### HideChrome

Auth pages ตั้ง:

```csharp
ViewData["HideChrome"] = true;
```

layout อ่าน:

```csharp
var hideChrome = ViewData["HideChrome"] is bool value && value;
```

ผล:

- ไม่ render navbar
- ไม่ render footer
- body ได้ class `auth-body`
- main ได้ class `site-main-auth`
- เหมาะกับหน้า login/register ที่ต้องแสดงแบบ full auth screen

### Session ที่ layout อ่าน

| Session key | ใช้ทำอะไร |
| --- | --- |
| `one-man-vekery-cart` | คำนวณจำนวนสินค้าใน cart badge |
| `AdminPortalAuth.SessionAccountIdKey` | เช็ก login |
| `AdminPortalAuth.SessionAccountNameKey` | แสดงชื่อ/avatar |
| `AdminPortalAuth.SessionAccountRoleKey` | แยก user กับ admin |
| `AdminPortalAuth.SessionAccountRoleLabelKey` | label role |

### Navbar role behavior

```text
ยังไม่ login
  -> แสดง Login/Register

login เป็น user
  -> แสดง Cart, My Orders, Points, Profile, Logout

login เป็น staff/admin/owner
  -> profile link ไป Admin/Profile
  -> ไม่แสดง cart user tools
```

### Points ใน navbar

ถ้าเป็น storefront user layout query:

```text
LoyaltyWallets
  -> CurrentPoints
  -> แสดงใน bakery-points-pill
```

ข้อควรระวัง:

- Layout นี้ inject `OneManVekeryDBContext`
- ถ้าแก้ auth/session role logic ต้องตรวจจุดนี้ด้วย
- ถ้า performance เริ่มเป็นปัญหา อาจย้าย points ไป preload ใน controller/base layout model ภายหลัง

### `data-storefront-user`

body มี:

```html
data-storefront-user="true/false"
```

ใช้บอก JavaScript ว่า user ฝั่งหน้าร้าน login อยู่หรือไม่

ส่วนนี้สัมพันธ์กับ:

- form ที่มี `data-login-required-form`
- login prompt modal
- add to cart behavior

## `_AdminLayout.cshtml`

หน้าที่: layout หลังบ้าน

ส่วนสำคัญ:

| ส่วน | หน้าที่ |
| --- | --- |
| top Razor block | อ่าน `ViewData["AdminSection"]`, signed-in admin data |
| sidebar | nav admin |
| staff nav guard | ซ่อน Staff ถ้า role ไม่มีสิทธิ์ |
| topbar | title และ date pill |
| `RenderBody()` | จุดแทรกหน้า admin จริง |
| footer | admin footer |
| scripts | jQuery, Bootstrap, `site.js`, `RenderSectionAsync("Scripts")` |

### AdminSection

หน้า admin แต่ละหน้าตั้ง:

```csharp
ViewData["AdminSection"] = "Orders";
```

layout ใช้เพื่อ highlight nav:

```text
adminSection == "Orders"
  -> Orders nav active
```

ค่าที่ใช้:

| ค่า | หน้า |
| --- | --- |
| `Dashboard` | Admin/Index |
| `Orders` | Admin/Orders |
| `Staff` | Admin/Staff |
| `Products` | Admin/Products |
| `Codes` | Admin/Codes |
| `Items` | Admin/Items |
| `Accounts` | Admin/Accounts |
| `Profile` | Admin/Profile |

### Signed-in admin data

`AdminController.OnActionExecuting()` set:

```text
ViewData["AdminSignedInName"]
ViewData["AdminSignedInRole"]
ViewData["AdminSignedInRoleKey"]
```

layout เอาไปแสดงใน sidebar account และใช้ตรวจสิทธิ์ Staff nav

### AdminDateRange

ถ้า controller หรือ view set:

```csharp
ViewData["AdminDateRange"] = ...
```

layout จะแสดงเป็น pill ด้านขวาของ topbar

## `_ValidationScriptsPartial.cshtml`

```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

หน้าที่:

- เปิด client-side validation จาก DataAnnotations
- view ที่มี form จะ include ผ่าน:

```csharp
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

ถ้าไม่ include:

- server-side validation ยังทำงาน
- แต่ browser จะไม่ validate แบบ unobtrusive ก่อน submit

## `Error.cshtml`

ใช้ `ErrorViewModel`

หน้าที่:

- แสดง error page
- แสดง `RequestId` ถ้ามี
- ไม่เกี่ยวกับ business flow หลัก

## Razor/Tag Helper ที่เจอบ่อย

### `asp-action`

```html
<form asp-action="AddToCart" method="post">
```

สร้าง action URL ไป controller ปัจจุบัน ถ้าไม่ระบุ `asp-controller`

### `asp-controller`

```html
<a asp-controller="Home" asp-action="Shop">สินค้า</a>
```

ใช้ข้าม controller

### `asp-route-*`

```html
<a asp-action="OrderStatus" asp-route-orderNumber="@order.OrderNumber">
```

สร้าง route/query value เช่น `orderNumber`

### `asp-for`

```html
<input asp-for="Checkout.CustomerName" />
```

สร้าง `name`, `id`, `value` ตาม model expression

### `asp-validation-for`

```html
<span asp-validation-for="Checkout.CustomerName"></span>
```

แสดง validation error ของ field นั้น

## ข้อควรระวังเวลาแก้ layout

- `_Layout.cshtml` กระทบทุกหน้า Home/Account
- `_AdminLayout.cshtml` กระทบทุกหน้า Admin
- ถ้าเพิ่ม script global ต้องเช็กว่าไม่ชนกับ auth pages ที่ hide chrome
- ถ้าเปลี่ยน nav role logic ต้องเช็กทั้ง user และ admin session
- ถ้าเปลี่ยน `RenderSectionAsync("Scripts")` อาจทำให้ validation scripts ของ form pages ไม่โหลด
- ถ้าเปลี่ยนชื่อ modal id เช่น `siteConfirmModal` ต้องแก้ `site.js`
