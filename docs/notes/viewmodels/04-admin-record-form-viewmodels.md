# Admin Record and Form ViewModels

ไฟล์หลัก: `ViewModel/AdminViewModels.cs`

เอกสารนี้อธิบาย model ย่อยของ admin ที่ไม่ได้เป็น `@model` หลักของหน้า แต่ถูก page model รวมไปใช้ เช่น record ใน table, card, modal form, dropdown option และ chart point

## กลุ่ม record/display models

| Class | บรรทัด | ใช้ที่ไหน | หน้าที่ |
| --- | --- | --- | --- |
| `AdminPromotionRecordViewModel` | 103 | Codes table | แถว promotion/promo code |
| `AdminProductShowcaseViewModel` | 171 | Products cards | card สินค้าสำหรับ publish/hide |
| `AdminDashboardTopProductViewModel` | 235 | Dashboard | top product summary |
| `AdminStaffRecordViewModel` | 244 | Staff page | แถว staff |
| `AdminInventoryItemViewModel` | 326 | Items cards | card สินค้า inventory |
| `AdminAccountRecordViewModel` | 404 | Accounts table | แถว account |
| `AdminOrderRecordViewModel` | 497 | Orders/dashboard | แถว order และ receipt modal |
| `AdminOrderItemRecordViewModel` | 548 | Order receipt | สินค้าใน order |
| `AdminOrderBenefitRecordViewModel` | 559 | Order receipt | promotion/points benefit |

## กลุ่ม form/editor models

| Class | บรรทัด | Bind prefix | ใช้กับ |
| --- | --- | --- | --- |
| `AdminPromoCodeEditorViewModel` | 130 | `CreateForm` | สร้าง promo code |
| `AdminCategoryEditorViewModel` | 315 | `CategoryForm` | เพิ่ม category |
| `AdminItemEditorViewModel` | 363 | `AddForm`, `EditForm` | เพิ่ม/แก้ item |
| `AdminAccountEditorViewModel` | 431 | `AddForm`, `EditForm` | เพิ่ม/แก้ account |
| `AdminOrderEditorViewModel` | 568 | `EditForm` | แก้ order status/payment |
| `AdminOrderCreateViewModel` | 598 | `AddForm` | สร้าง order |
| `AdminOrderLineEditorViewModel` | 631 | `AddForm.Items[index]` | สินค้าแต่ละบรรทัดใน order |

## กลุ่ม shared UI models

| Class | บรรทัด | ใช้ทำอะไร |
| --- | --- | --- |
| `AdminInfoItemViewModel` | 464 | summary item สั้นๆ |
| `AdminMetricCardViewModel` | 475 | metric card |
| `AdminChartPointViewModel` | 488 | จุดกราฟ |
| `AdminSelectOptionViewModel` | 640 | dropdown/combobox option |

## AdminPromotionRecordViewModel

หน้าที่: แถวในหน้า Codes ที่รวมทั้ง promotion campaign และ promo code

Property สำคัญ:

| Property | ความหมาย |
| --- | --- |
| `PromoCodeId` | id ของ promo code ถ้าแถวนี้เป็น code |
| `IsPromoCode` | แยกว่าคือ code หรือ campaign |
| `CreatedAtSort` | ค่า sort สำหรับ JS/table |
| `Code` | โค้ดหรือชื่อ campaign |
| `Title` | ชื่อแสดงผล |
| `DiscountLabel` | label ส่วนลด |
| `RuleLabel` | เงื่อนไข |
| `Status` | label สถานะ |
| `StatusKey` | key สำหรับ class/filter |
| `UsageLabel` | จำนวนการใช้ |
| `ExpiryLabel` | วันหมดอายุ |
| `Note` | note |

สร้างจาก:

```text
Controllers/Admin/PromotionRows.cs
  -> BuildPromotionRows()
  -> BuildPromotionCodeLabel()
  -> BuildPromotionDiscountLabel()
  -> ...
```

## AdminPromoCodeEditorViewModel

หน้าที่: form สร้าง promo code

Validation:

| Property | Validation |
| --- | --- |
| `PromotionId` | optional |
| `Code` | `[Required]`, `[StringLength(40)]` |
| `Title` | `[Required]`, `[StringLength(120)]` |
| `Description` | `[StringLength(255)]` |
| `DiscountType` | `[Required]` |
| `DiscountValue` | `[Range(0, 100000)]` |
| `MinOrderAmount` | `[Range(0, 1000000)]` |
| `MaxDiscountAmount` | `[Range(0, 1000000)]` |
| `UsageLimit` | `[Range(1, 100000)]` |
| `StartsAt` | optional |
| `ExpiresAt` | optional |
| `Status` | `[Required]` |
| `Note` | `[StringLength(255)]` |

Flow:

```text
POST /Admin/CreatePromoCode
  -> bind CreateForm
  -> ValidatePromoCodeForm(form)
  -> CreatePromoCodeRecord(form)
  -> redirect Codes
```

หมายเหตุ:

- validation บางอย่างอยู่ใน DataAnnotations
- validation เชิงธุรกิจ เช่น type/status/date อยู่ใน controller helper

## AdminProductShowcaseViewModel

หน้าที่: card สินค้าในหน้า Products สำหรับคุมเปิด/ปิดหน้าร้าน

Property สำคัญ:

| Property | ความหมาย |
| --- | --- |
| `ProductId`, `ProductCode` | id/code |
| `Name`, `Category`, `Tagline`, `Notes` | รายละเอียดสินค้า |
| `ImagePath` | รูป |
| `PriceAmount`, `PriceLabel` | ราคา numeric และ label |
| `StockQuantity`, `StockLabel`, `ReorderLevel` | stock |
| `SalesLabel`, `RevenueLabel`, `UnitsSold`, `RevenueAmount` | ยอดขาย |
| `VisibilityLabel`, `VisibilityKey`, `PublishedCopy`, `IsPublished` | สถานะ publish/hide |

จุดสำคัญ:

- `PriceAmount`, `UnitsSold`, `RevenueAmount` เป็นข้อมูล numeric
- `PriceLabel`, `SalesLabel`, `RevenueLabel` เป็นข้อมูล formatted สำหรับแสดงผล
- `VisibilityKey` มักใช้กับ CSS/JS

## AdminInventoryItemViewModel

หน้าที่: card สินค้าในหน้า Items

Property สำคัญ:

| Property | ความหมาย |
| --- | --- |
| `ItemId`, `ItemCode`, `Sku` | id/code/sku |
| `Name`, `Category`, `Tagline`, `Notes` | ข้อมูลสินค้า |
| `ImagePath` | รูป |
| `PriceAmount`, `PriceLabel` | ราคา |
| `StockQuantity`, `ReorderLevel` | stock และจุดเตือน |
| `StatusLabel`, `StatusKey` | สถานะ stock |
| `UpdatedAtLabel`, `UpdatedAtSort` | วันที่ update สำหรับแสดง/sort |
| `IsPublished` | เปิดขายอยู่ไหม |

ใช้กับ:

- card inventory
- filter/sort ใน JS ผ่าน data attributes
- modal edit item โดยเติมค่าเดิมจาก card

## AdminItemEditorViewModel

หน้าที่: form เพิ่ม/แก้สินค้า

Validation:

| Property | Validation | หมายเหตุ |
| --- | --- | --- |
| `ItemId` | ไม่มี | 0 สำหรับเพิ่ม, มากกว่า 0 สำหรับแก้ |
| `ItemCode` | ไม่มี | display |
| `Name` | `[Required]`, `[StringLength(80)]` | ชื่อสินค้า |
| `Category` | `[Required]`, `[StringLength(40)]` | หมวด |
| `Sku` | `[Required]`, `[StringLength(24, MinimumLength = 3)]` | SKU |
| `Price` | `[Range(typeof(decimal), "1", "99999")]` | ราคา |
| `StockQuantity` | `[Range(0, 5000)]` | stock |
| `ReorderLevel` | `[Range(0, 1000)]` | จุดเตือน stock |
| `Tagline` | `[StringLength(120)]` | คำอธิบายสั้น |
| `Notes` | `[StringLength(240)]` | note |
| `ImagePath` | `[StringLength(160)]` | path รูป |
| `ImageFile` | `IFormFile?` | upload รูป |
| `IsPublished` | bool | เปิดขาย |

Flow:

```text
POST AddItem()
  -> bind AddForm
  -> validate SKU duplicate
  -> validate/upload image
  -> create Product

POST UpdateItem()
  -> bind EditForm
  -> validate current item
  -> validate SKU excluding current item
  -> validate/upload image
  -> update Product
```

จุดสำคัญ:

- `ImageFile` ทำให้ไฟล์นี้ต้อง import `Microsoft.AspNetCore.Http`
- validation upload จริงอยู่ใน `Controllers/Admin/ItemImages.cs`

## AdminCategoryEditorViewModel

หน้าที่: form เพิ่มหมวดในหน้า Items

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `Name` | `[Required]`, `[StringLength(40)]` | ชื่อหมวด |
| `TargetFieldId` | ไม่มี | field ที่จะเติมค่า category กลับไป |
| `ReturnModalId` | ไม่มี | modal ที่จะกลับไปเปิดหลังเพิ่มสำเร็จ |

ใช้กับ AJAX:

```text
AddCategory()
  -> return JSON categoryName/targetFieldId/returnModalId
  -> site.js เติม option และกลับ modal เดิม
```

## AdminAccountRecordViewModel

หน้าที่: แถว account ในหน้า Accounts

Property สำคัญ:

| Property | ความหมาย |
| --- | --- |
| `AccountId`, `AccountCode` | id/code |
| `FullName`, `Email`, `PhoneNumber` | ข้อมูลบัญชี |
| `Role`, `Status`, `StatusKey` | role/status |
| `IsProtectedAdminAccount` | account พิเศษที่ห้ามปิด/ลดสิทธิ์ |
| `LastActive`, `LastActiveSort` | last active สำหรับแสดง/sort |
| `Notes` | note |

ใช้กับ:

- table row
- filter/search/sort
- edit modal fill
- protected admin UI state

## AdminAccountEditorViewModel

หน้าที่: form เพิ่ม/แก้ account

Validation:

| Property | Validation |
| --- | --- |
| `FullName` | `[Required]`, `[StringLength(80)]` |
| `Email` | `[Required]`, `[EmailAddress]` |
| `PhoneNumber` | `[Required]`, `[Phone]` |
| `Role` | `[Required]` |
| `Status` | `[Required]` |
| `Notes` | `[StringLength(160)]` |
| `Password` | `[RegularExpression(@"^$|^.{8,100}$")]` |

เหตุผลของ password regex:

- ค่าว่างแปลว่าไม่เปลี่ยน password ตอน edit
- ถ้ากรอก ต้องยาว 8-100 ตัวอักษร

## AdminOrderRecordViewModel

หน้าที่: record order ที่ใช้ทั้งหน้า Orders และ dashboard latest orders

Property กลุ่มหลัก:

| กลุ่ม | Property |
| --- | --- |
| identity | `OrderId`, `OrderNumber` |
| summary | `ProductSummary`, `ItemCountLabel`, `CreatedAtLabel`, `CustomerName` |
| totals | `TotalAmountLabel`, `SubtotalLabel`, `DeliveryFeeLabel`, `DiscountAmountLabel`, `ShippingDiscountAmountLabel` |
| benefits | `DiscountCode`, `PointsEarnedLabel`, `PointsRedeemedLabel`, `Benefits` |
| payment/status | `PaymentMethodLabel`, `PaymentStatus`, `PaymentStatusKey`, `OrderStatus`, `OrderStatusKey` |
| delivery | `Phone`, `Address`, `Note` |
| items | `Items` |

จุดสำคัญ:

- `Items` ใช้ใน receipt modal
- `Benefits` ใช้โชว์ promotion/points ใน order
- status มีทั้ง label และ key เพื่อแยก display กับ CSS/filter

## AdminOrderEditorViewModel

หน้าที่: form แก้ order/payment status

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `OrderId` | ไม่มี | order ที่แก้ |
| display fields | ไม่มี | `OrderNumber`, `CustomerName`, `CreatedAtLabel`, etc. |
| `OrderStatus` | `[Required]` | สถานะ order |
| `PaymentStatus` | `[Required]` | สถานะชำระเงิน |
| `Note` | `[StringLength(200)]` | note |

หมายเหตุ:

display fields มี `set;` เพราะ JS/controller อาจเติมค่าเข้า form แต่ไม่ได้เป็นข้อมูลหลักที่ใช้ update database

## AdminOrderCreateViewModel และ AdminOrderLineEditorViewModel

`AdminOrderCreateViewModel` คือ form สร้าง order จากหลังบ้าน

Property:

| Property | Validation |
| --- | --- |
| `UserId` | `[Range(1, int.MaxValue)]` |
| `Phone` | `[Required]`, `[Phone]` |
| `Address` | `[Required]` |
| `PaymentMethod` | `[Required]` |
| `PaymentStatus` | `[Required]` |
| `OrderStatus` | `[Required]` |
| `DeliveryFee` | `[Range(typeof(decimal), "0", "9999")]` |
| `Note` | `[StringLength(200)]` |
| `Items` | list ของ `AdminOrderLineEditorViewModel` |

`AdminOrderLineEditorViewModel`:

| Property | Validation |
| --- | --- |
| `ProductId` | `[Range(1, int.MaxValue)]` |
| `Quantity` | `[Range(1, 500)]` |

Flow:

```text
POST AddOrder()
  -> bind AddForm.Items[0].ProductId / Quantity
  -> group duplicate product lines
  -> validate stock
  -> create Order + OrderItems
  -> decrement stock in transaction
```

## Shared UI models

### AdminInfoItemViewModel

ใช้กับ summary row/card ขนาดเล็ก

| Property | ความหมาย |
| --- | --- |
| `Label` | ชื่อ metric |
| `Value` | ค่าหลัก |
| `Detail` | รายละเอียด |
| `AccentKey` | theme/status key |

### AdminMetricCardViewModel

ใช้กับ metric cards

| Property | ความหมาย |
| --- | --- |
| `Label` | ชื่อ metric |
| `Value` | ค่าหลัก |
| `Delta` | การเปลี่ยนแปลง |
| `PositiveTrend` | trend บวกไหม |
| `AccentKey` | theme/status key |

### AdminChartPointViewModel

ใช้กับกราฟแบบง่าย

| Property | ความหมาย |
| --- | --- |
| `Label` | label แกน/วัน |
| `Value` | ค่า |
| `IsHighlighted` | จุดนี้ถูก highlight ไหม |

### AdminSelectOptionViewModel

ใช้กับ dropdown/combobox

| Property | ความหมาย |
| --- | --- |
| `Value` | value ที่ submit |
| `Label` | text หลัก |
| `SecondaryLabel` | text รอง |
| `DataValue` | data attribute เพิ่มเติม |
| `DataExtra` | data attribute เพิ่มเติมอีกชุด |

ตัวอย่าง:

```text
ProductOptions
  -> Value = product id
  -> Label = product name
  -> SecondaryLabel = price/stock
  -> DataValue = unit price
  -> DataExtra = stock quantity
```

## ข้อควรระวังเวลาแก้ record/form models

- ถ้าเพิ่ม field ใน record ต้องแก้ mapper และ data attributes ใน view ถ้า JS ใช้ field นั้น
- ถ้าเพิ่ม field ใน form ต้องแก้ validation, input name, bind prefix และ POST action
- ถ้าเพิ่ม dropdown option ต้องดูว่าเป็น `string` list หรือ `AdminSelectOptionViewModel`
- ถ้าข้อมูลใช้ sort ฝั่ง JS ควรมี sort value แยก เช่น `UpdatedAtSort`, `LastActiveSort`, `CreatedAtSort`
- อย่าใช้ label ที่ format แล้วไปคำนวณ เช่น `PriceLabel` ไม่ควร parse กลับเป็น decimal
