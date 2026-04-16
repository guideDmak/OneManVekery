# ViewModel Notes

เอกสารหมวดนี้อธิบาย `ViewModel` ของโปรเจกต์ One Man Vekery ว่าแต่ละ class เป็นข้อมูลสำหรับหน้าไหน รับข้อมูลจาก controller ตรงไหน และ view เอาไปแสดงหรือ bind form อย่างไร

## ViewModel คืออะไรในโปรเจกต์นี้

`ViewModel` คือ object กลางระหว่าง controller กับ `.cshtml`

```text
Database entity / calculated data
  -> Controller helper สร้าง ViewModel
  -> return View(model)
  -> Razor view อ่าน property ไปแสดงผล
  -> ถ้าเป็น form POST จะ bind กลับเข้า ViewModel อีกตัว
  -> Controller validate และบันทึกลง database
```

ในโปรเจกต์นี้ `ViewModel` ไม่ใช่ database table โดยตรง และไม่ควรใส่ database query ลงใน class เหล่านี้ หน้าที่หลักคือจัดรูปข้อมูลให้ view ใช้ง่าย

## ไฟล์ ViewModel ที่มี

| ไฟล์ | กลุ่มหน้า | หน้าที่หลัก |
| --- | --- | --- |
| `ViewModel/StorefrontViewModels.cs` | หน้าร้าน | หน้าแรก, shop, cart, checkout, order status, my orders, about, contact |
| `ViewModel/AuthViewModels.cs` | account/auth | login, register, register address, profile, address form |
| `ViewModel/AdminViewModels.cs` | หลังบ้าน | dashboard, orders, products, items, promo codes, accounts, staff, profile |

## วิธีอ่านหมวดนี้

| ไฟล์โน้ต | อ่านเมื่อ |
| --- | --- |
| `01-storefront-viewmodels.md` | ต้องการเข้าใจข้อมูลที่ส่งให้หน้า Home/Shop/Cart/Checkout |
| `02-auth-viewmodels.md` | ต้องการเข้าใจ login/register/profile/address form |
| `03-admin-page-viewmodels.md` | ต้องการเข้าใจ view model ระดับหน้าของ admin |
| `04-admin-record-form-viewmodels.md` | ต้องการเข้าใจ record/form/shared model ย่อยของ admin |
| `05-binding-validation-flow.md` | ต้องการเพิ่ม field ใหม่หรือ debug validation/binding |

## Pattern ที่ใช้บ่อย

### 1. Page model

เป็น model ใหญ่ประจำหน้า เช่น `CartPageViewModel`, `AdminOrdersViewModel`, `AccountProfileViewModel`

หน้าที่:

- รวม list ที่ต้องแสดง
- รวม summary/metric/options
- รวม form ย่อยที่หน้านั้นต้องใช้
- เก็บ `ActiveModal` เพื่อเปิด modal เดิมหลัง validation fail

ตัวอย่าง:

```csharp
public class AdminOrdersViewModel
{
    public IReadOnlyList<AdminOrderRecordViewModel> Orders { get; init; } = [];
    public AdminOrderCreateViewModel AddForm { get; init; } = new();
    public AdminOrderEditorViewModel EditForm { get; init; } = new();
    public string ActiveModal { get; init; } = string.Empty;
}
```

### 2. Record/card model

เป็น item ย่อยที่ใช้ render ซ้ำ เช่น product card, order row, account row

หน้าที่:

- เก็บข้อมูลที่ formatted แล้ว เช่น label, status key, price label
- ลด logic ใน `.cshtml`
- ทำให้ view ใช้ `foreach` ได้ตรงๆ

ตัวอย่าง:

```text
AdminOrdersViewModel.Orders
  -> IReadOnlyList<AdminOrderRecordViewModel>
  -> Views/Admin/Orders.cshtml foreach render table/modal
```

### 3. Form/editor model

เป็น model ที่รับค่าจาก form POST เช่น `CartCheckoutViewModel`, `AdminItemEditorViewModel`, `LoginViewModel`

หน้าที่:

- มี `set;` เพื่อให้ model binder ใส่ค่าจาก request ได้
- ใช้ DataAnnotations เช่น `[Required]`, `[StringLength]`, `[Range]`
- controller ตรวจ `ModelState.IsValid`

ตัวอย่าง:

```csharp
[Required(ErrorMessage = "กรุณากรอกชื่อสินค้า")]
public string Name { get; set; } = string.Empty;
```

### 4. Display/computed property

บาง model มี property ที่คำนวณจากข้อมูลอื่น เพื่อให้ view ไม่ต้องคำนวณเอง

ตัวอย่างใน `CartPageViewModel`:

```csharp
public decimal TotalSavings => DiscountAmount + ShippingDiscountAmount;
public decimal Total => Math.Max(0, Subtotal + DeliveryFee - TotalSavings);
public bool HasItems => Items.Count > 0;
```

ผลคือ view แค่ถาม `Model.HasItems` หรือแสดง `Model.Total` ได้ทันที

## ความต่างระหว่าง `init` กับ `set`

| รูปแบบ | ใช้กับ | เหตุผล |
| --- | --- | --- |
| `get; init;` | model สำหรับแสดงผล | controller set ตอนสร้าง object แล้ว view อ่านอย่างเดียว |
| `get; set;` | form/editor model | ASP.NET Core model binder ต้อง set ค่าจาก POST request |

ถ้า property อยู่ใน form แล้วเผลอใช้ `init` อาจ bind ค่า POST ไม่ได้ตามที่คาด ถ้า property เป็น display-only แล้วใช้ `set` ก็ไม่ผิดเสมอไป แต่ทำให้ object ถูกแก้ได้ง่ายเกินจำเป็น

## DataAnnotations ที่ใช้บ่อย

| Attribute | ใช้ทำอะไร | ตัวอย่าง |
| --- | --- | --- |
| `[Required]` | บังคับกรอก | email, name, address |
| `[EmailAddress]` | ตรวจรูปแบบ email | login/register/account |
| `[Phone]` | ตรวจรูปแบบเบอร์โทร | profile, checkout, order |
| `[StringLength]` | จำกัดความยาว | SKU, note, title |
| `[Range]` | จำกัดตัวเลข | price, stock, points, quantity |
| `[Compare]` | เทียบสอง field | password กับ confirm password |
| `[Display]` | ชื่อ label สำหรับ field | promo code, point options |
| `[RegularExpression]` | pattern เฉพาะ | password optional ใน admin account |

## จุดที่ควรระวัง

- ViewModel ไม่ควรรู้จัก database context
- ViewModel ไม่ควร save database
- ถ้าเพิ่ม field ใหม่ ต้องแก้ครบ controller builder, view model, `.cshtml`, และ POST handler
- ถ้า field อยู่ใน modal ต้องดู `ActiveModal` ด้วย ไม่งั้น validation error จะถูกซ่อนไว้หลัง reload
- ถ้าใช้ `[Bind(Prefix = "...")]` ชื่อ property ใน page model ต้องตรงกับ prefix
- `IReadOnlyList<T>` ใช้กับข้อมูลแสดงผลเพื่อสื่อว่า view ไม่ควรแก้ list
- Label ที่มีคำว่า `Label`, `Copy`, `Description`, `Key` มักเป็นข้อมูลที่ผ่านการ format แล้ว ไม่ควรเอาไปคำนวณต่อ
