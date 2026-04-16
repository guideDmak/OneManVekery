# Binding and Validation Flow

เอกสารนี้อธิบายขบวนการทำงานของ ViewModel ตอน request เข้ามา ตอน controller ส่งข้อมูลไป view และตอน form POST กลับมา

## Flow แบบ GET

GET คือ controller สร้าง ViewModel เพื่อให้ view แสดงผล

```text
Browser request GET
  -> Routing เลือก Controller.Action
  -> Controller query database หรืออ่าน session
  -> Controller helper map entity เป็น ViewModel
  -> return View(viewModel)
  -> Razor view อ่าน property ผ่าน Model
  -> HTML ถูกส่งกลับ browser
```

ตัวอย่างหน้าแรก:

```text
GET /
  -> HomeController.Index()
  -> load products
  -> BuildCategoryCards()
  -> BuildBestSellingProducts()
  -> new HomeIndexViewModel { Categories, Products, NewArrivals }
  -> Views/Home/Index.cshtml
```

ตัวอย่าง admin orders:

```text
GET /Admin/Orders
  -> AdminController.OnActionExecuting()
  -> Orders()
  -> BuildOrdersModel()
  -> new AdminOrdersViewModel { Orders, AddForm, EditForm, Options }
  -> Views/Admin/Orders.cshtml
```

## Flow แบบ POST

POST คือ browser ส่ง form กลับมา แล้ว ASP.NET Core bind ค่าเข้า ViewModel

```text
Browser submit form
  -> Routing เลือก POST action
  -> Model binder อ่าน input name/value
  -> สร้าง form ViewModel
  -> DataAnnotations เติม ModelState errors ถ้ามี
  -> Controller เช็ก ModelState.IsValid
  -> valid: update database/session
  -> invalid: rebuild page model พร้อม form เดิม
```

ตัวอย่าง login:

```text
POST /Account/Login
  -> bind LoginViewModel
  -> Required/EmailAddress/StringLength validation
  -> if invalid return View(model)
  -> Authenticate()
  -> set session
  -> redirect
```

ตัวอย่าง checkout:

```text
POST /Home/Checkout
  -> bind CartCheckoutViewModel
  -> validate customer/payment/address
  -> calculate cart/promo/points
  -> if invalid return CartPageViewModel with same Checkout form
  -> create order
  -> redirect OrderStatus
```

## Prefix binding

บางหน้าใช้ page model ใหญ่ที่มี form ย่อยอยู่ข้างใน เช่น:

```csharp
public class AdminOrdersViewModel
{
    public AdminOrderCreateViewModel AddForm { get; init; } = new();
    public AdminOrderEditorViewModel EditForm { get; init; } = new();
}
```

ใน view input จะมีชื่อประมาณ:

```text
AddForm.UserId
AddForm.Phone
AddForm.Items[0].ProductId
EditForm.OrderStatus
```

ใน controller จึงใช้:

```csharp
public IActionResult AddOrder([Bind(Prefix = "AddForm")] AdminOrderCreateViewModel form)
```

ความหมาย:

- model binder จะมองเฉพาะ input ที่ขึ้นต้นด้วย `AddForm.`
- แล้ว map เข้า property ของ `AdminOrderCreateViewModel`

ถ้า prefix ไม่ตรง:

- ค่าใน form จะเป็น default
- validation อาจ fail แบบดูเหมือน user ไม่กรอก
- หรือ controller update ข้อมูลผิด/ไม่ครบ

## Validation จาก DataAnnotations

DataAnnotations ทำงานตอน model binding

ตัวอย่าง:

```csharp
[Required(ErrorMessage = "กรุณากรอกชื่อสินค้า")]
[StringLength(80, ErrorMessage = "ชื่อสินค้าต้องไม่เกิน 80 ตัวอักษร")]
public string Name { get; set; } = string.Empty;
```

ผล:

```text
input Name ว่าง
  -> ModelState มี error "กรุณากรอกชื่อสินค้า"
  -> ModelState.IsValid = false
```

```text
input Name ยาวเกิน 80
  -> ModelState มี error "ชื่อสินค้าต้องไม่เกิน 80 ตัวอักษร"
```

## Validation เชิงธุรกิจ

บาง rule เขียนด้วย DataAnnotations ไม่พอ ต้องตรวจใน controller

ตัวอย่าง:

| Rule | อยู่ที่ |
| --- | --- |
| email ซ้ำไหม | Account/Admin controller |
| SKU ซ้ำไหม | `Controllers/Admin/Items.cs` |
| stock พอกับ order ไหม | `Controllers/Admin/Orders.cs`, checkout flow |
| promo code หมดอายุไหม | promo helpers/controller |
| protected admin account ห้ามปิด | `Controllers/Admin/Accounts.cs` |
| image upload extension/size ถูกไหม | `Controllers/Admin/ItemImages.cs` |

Pattern:

```csharp
if (EmailExists(form.Email, accountId))
{
    ModelState.AddModelError(ProfileEditField(nameof(form.Email)), "อีเมลนี้ถูกใช้งานแล้ว");
}
```

ข้อสำคัญ:

- `ModelState.AddModelError()` ควรใส่ key ให้ตรง field
- ถ้าเป็น nested form ต้องใช้ helper สร้าง field key เช่น `EditForm.Email`
- ถ้าใส่ key ผิด error จะไม่แสดงใต้ input ที่ถูกต้อง

## ModelState กับ modal

หลายหน้าใช้ modal เช่น profile, items, orders, codes, accounts

ปัญหา:

```text
POST modal form invalid
  -> controller return view
  -> browser reload
  -> modal ปิด
  -> user ไม่เห็น error
```

วิธีที่โปรเจกต์ใช้:

```text
controller invalid
  -> BuildPageModel(form: form, activeModal: "...")
  -> page model มี ActiveModal
  -> view render data-active-modal
  -> JavaScript เปิด modal เดิม
```

ตัวอย่าง:

| หน้า | Page model | Form | Active modal |
| --- | --- | --- | --- |
| Profile | `AccountProfileViewModel` | `EditForm` | `profile-edit` |
| Profile | `AccountProfileViewModel` | `AddressForm` | `address-edit` |
| Orders | `AdminOrdersViewModel` | `AddForm` | `order-add` |
| Codes | `AdminCodesViewModel` | `CreateForm` | `code-create` |
| Items | `AdminItemsPageViewModel` | `AddForm`/`EditForm` | item modal |
| Accounts | `AdminAccountsViewModel` | `AddForm`/`EditForm` | account modal |

## init-only display models

Display models ส่วนใหญ่ใช้ `init`

```csharp
public string Name { get; init; } = string.Empty;
```

ความหมาย:

- controller set ค่าได้ตอนสร้าง object
- หลังจากสร้างแล้วไม่ควรแก้
- view อ่านอย่างเดียว

เหมาะกับ:

- card
- table row
- summary
- chart point
- option list

## settable form models

Form models ใช้ `set`

```csharp
public string Email { get; set; } = string.Empty;
```

ความหมาย:

- model binder set ค่าได้จาก request
- controller อาจแก้ค่าก่อนส่งกลับ view ได้

เหมาะกับ:

- login/register form
- checkout form
- admin add/edit forms
- modal forms

## รายการที่ต้องแก้เมื่อเพิ่ม field ใหม่

### เพิ่ม field แสดงผลบน card/table

เช็กลำดับนี้:

1. เพิ่ม property ใน record/card ViewModel
2. แก้ controller mapper ให้ set ค่า
3. แก้ `.cshtml` ให้ render property
4. ถ้า JS ใช้ filter/sort/edit modal ให้เพิ่ม `data-*`
5. ถ้า CSS ต้องรองรับ layout ใหม่ ให้แก้ CSS

### เพิ่ม field ใน form

เช็กลำดับนี้:

1. เพิ่ม property ใน form ViewModel
2. ใส่ DataAnnotations ถ้าต้อง validate
3. เพิ่ม input ใน `.cshtml` โดยใช้ prefix ให้ถูก
4. แก้ POST action ให้ใช้ค่าจริง
5. แก้ invalid path ให้ rebuild page model พร้อม form เดิม
6. ถ้า field อยู่ใน modal edit ต้องแก้ JS เติมค่าเดิม
7. ถ้า field map database ต้องแก้ entity/database migration ถ้าจำเป็น

### เพิ่ม dropdown/select

เช็กลำดับนี้:

1. เพิ่ม options ใน page model
2. สร้าง options ใน controller builder
3. render select ใน view
4. ตรวจ validation ว่าค่าที่ submit อยู่ใน allowed values
5. ถ้าเป็น custom combobox ต้องแก้ JS data attributes

## ตัวอย่างเพิ่ม field แบบปลอดภัย

สมมติจะเพิ่ม `AllergenNote` ให้สินค้าใน admin item

ต้องแก้:

```text
ViewModel/AdminViewModels.cs
  -> AdminInventoryItemViewModel.AllergenNote
  -> AdminItemEditorViewModel.AllergenNote

Controllers/Admin/InventoryData.cs หรือ Items mapper
  -> map Product.AllergenNote เป็น ViewModel

Views/Admin/Items.cshtml
  -> แสดง note บน card
  -> เพิ่ม input ใน AddForm/EditForm
  -> เพิ่ม data-* ให้ edit modal เติมค่า

Controllers/Admin/Items.cs
  -> AddItem()/UpdateItem() save value

wwwroot/js/site.js
  -> ถ้า edit modal ใช้ JS fill ค่า ต้องเพิ่ม field
```

## Debug checklist

ถ้า form submit แล้วค่าไม่เข้า controller:

- input `name` ตรงกับ property ไหม
- ถ้าใช้ `[Bind(Prefix)]` prefix ตรงไหม
- checkbox มี hidden fallback ไหม
- list ใช้ index ถูกไหม เช่น `Items[0].ProductId`
- field ถูก disabled อยู่ไหม เพราะ disabled input ไม่ submit

ถ้า validation error ไม่ขึ้น:

- view มี `asp-validation-for` หรือ validation summary ไหม
- error key ใน `ModelState.AddModelError` ตรง field ไหม
- ถ้าอยู่ใน modal มี `ActiveModal` ไหม
- page model ส่ง form เดิมกลับไปไหม

ถ้าหน้า GET พังหลังเพิ่ม property:

- controller builder set ค่าให้ property ที่จำเป็นหรือยัง
- view ใช้ property null ได้ไหม
- list default เป็น `[]` แล้วหรือยัง
- object form default เป็น `new()` แล้วหรือยัง

## สรุปหลักคิด

- ViewModel คือ contract ระหว่าง controller กับ view
- Display model ควรอ่านง่ายและมีข้อมูลพร้อมแสดง
- Form model ควร validate ได้ชัดและ bind จาก request ได้ตรง
- Controller เป็นคน map, validate เชิงธุรกิจ และ save database
- View ควร render จาก ViewModel โดยไม่ต้องรู้ database rule เยอะเกินไป
