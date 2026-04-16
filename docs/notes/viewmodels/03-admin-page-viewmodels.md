# Admin Page ViewModels

ไฟล์หลัก: `ViewModel/AdminViewModels.cs`

ไฟล์นี้รวม ViewModel ของหลังบ้านทั้งหมด หน้านึงมักมี page model หลัก และ page model จะรวม record/form/options หลายตัวไว้ด้วยกัน

## Page model คืออะไร

Page model คือ class ที่ view ใช้เป็น `@model` บรรทัดแรก

ตัวอย่าง:

```csharp
@model AdminOrdersViewModel
```

แล้ว controller สร้าง model นี้ก่อนส่งไป view:

```text
GET /Admin/Orders
  -> AdminController.Orders()
  -> BuildOrdersModel()
  -> return View(model)
```

## แผนที่ page models

| Class | บรรทัด | View | Controller builder |
| --- | --- | --- | --- |
| `AdminDashboardViewModel` | 6 | `Views/Admin/Index.cshtml` | `BuildDashboardModel()` |
| `AdminOrdersViewModel` | 27 | `Views/Admin/Orders.cshtml` | `BuildOrdersModel()` |
| `AdminStaffViewModel` | 62 | `Views/Admin/Staff.cshtml` | `BuildStaffModel()` |
| `AdminProductsViewModel` | 71 | `Views/Admin/Products.cshtml` | `BuildProductsModel()` |
| `AdminCodesViewModel` | 82 | `Views/Admin/Codes.cshtml` | `BuildCodesModel()` |
| `AdminProfileViewModel` | 214 | `Views/Admin/Profile.cshtml` | `BuildProfileModel()` |
| `AdminAccountsViewModel` | 269 | `Views/Admin/Accounts.cshtml` | `BuildAccountsModel()` |
| `AdminItemsPageViewModel` | 294 | `Views/Admin/Items.cshtml` | `BuildItemsModel()` |

## AdminDashboardViewModel

หน้าที่: model หน้า dashboard หลังบ้าน

Property:

| Property | ความหมาย |
| --- | --- |
| `DateRangeLabel` | label ช่วงข้อมูล เช่นวันที่ sync |
| `Metrics` | card metric ด้านบน |
| `TrendLabel` | ชื่อกราฟ/summary |
| `TrendValue` | ค่าหลักของ trend |
| `TrendDelta` | การเปลี่ยนแปลง |
| `TrendChart` | จุดกราฟ |
| `SummaryItems` | summary ย่อย |
| `TopProducts` | สินค้าขายดี |
| `LatestOrders` | order ล่าสุด |

Flow:

```text
GET /Admin
  -> BuildDashboardModel()
  -> load orders/products/customers
  -> BuildDashboardMetrics()
  -> BuildDashboardTrendChart()
  -> BuildOrderFulfillmentSummary()
  -> BuildDashboardTopProducts()
  -> AdminDashboardViewModel
  -> Index.cshtml
```

## AdminOrdersViewModel

หน้าที่: model หน้า order management

Property:

| กลุ่ม | Property |
| --- | --- |
| summary | `DateRangeLabel`, `Metrics`, `UpdateLabel`, `UpdateValue`, `UpdateDelta`, `UpdateChart`, `FulfillmentSummary` |
| records | `Orders` |
| dropdown options | `OrderStatusOptions`, `PaymentStatusOptions`, `PaymentMethodOptions`, `CustomerOptions`, `ProductOptions` |
| forms | `AddForm`, `EditForm` |
| modal state | `ActiveModal` |

Flow list:

```text
GET /Admin/Orders
  -> BuildOrdersModel()
  -> query orders
  -> map AdminOrderRecordViewModel
  -> build dropdown options
  -> build AddForm/EditForm
  -> AdminOrdersViewModel
```

Flow validation fail:

```text
POST AddOrder()
  -> bind AdminOrderCreateViewModel from AddForm
  -> invalid
  -> BuildOrdersModel(addForm: form, activeModal: "order-add")
  -> Orders.cshtml opens add modal
```

จุดสำคัญ:

- `CustomerOptions` และ `ProductOptions` ใช้กับ custom combobox ใน JS
- `AddForm` เป็น form สำหรับสร้าง order
- `EditForm` เป็น form สำหรับแก้ status order
- `Orders` ใช้ render table และ receipt modal

## AdminStaffViewModel

หน้าที่: model หน้า staff directory

Property:

| Property | ความหมาย |
| --- | --- |
| `DateRangeLabel` | label sync |
| `Metrics` | metric ของ staff |
| `StaffMembers` | รายชื่อ staff/admin/owner ที่แสดง |

Flow:

```text
GET /Admin/Staff
  -> guard permission
  -> BuildStaffModel()
  -> AdminStaffViewModel
  -> Staff.cshtml
```

## AdminProductsViewModel

หน้าที่: model หน้า Products สำหรับคุมเปิด/ปิดสินค้าบนหน้าร้าน

Property:

| Property | ความหมาย |
| --- | --- |
| `DateRangeLabel` | label sync |
| `Metrics` | metric visibility/sales |
| `SummaryItems` | summary publish/hidden/stock |
| `Products` | cards สินค้าแบบ `AdminProductShowcaseViewModel` |

Flow:

```text
GET /Admin/Products
  -> GetAllInventoryItems()
  -> MapProductShowcase()
  -> BuildProductMetrics()
  -> BuildProductSummary()
  -> AdminProductsViewModel
```

จุดต่างจาก Items:

- Products ใช้คุม `IsPublished`
- Items ใช้แก้ข้อมูลสินค้า/stock/category/image

## AdminCodesViewModel

หน้าที่: model หน้า promo codes

Property:

| Property | ความหมาย |
| --- | --- |
| `DateRangeLabel` | label sync |
| `Metrics` | metric promotion/code |
| `SummaryItems` | summary ย่อย |
| `Promotions` | rows ที่แสดงใน table |
| `PromotionOptions` | campaign dropdown |
| `DiscountTypeOptions` | type dropdown เช่น percent/fixed |
| `StatusOptions` | active/paused/expired |
| `CreateForm` | form สร้าง promo code |
| `ActiveModal` | modal ที่เปิดหลัง validation fail |

Flow:

```text
GET /Admin/Codes
  -> BuildCodesModel()
  -> load promotions + promo codes
  -> BuildPromotionRows()
  -> BuildPromotionSelectOptions()
  -> AdminCodesViewModel
```

## AdminAccountsViewModel

หน้าที่: model หน้า account management

Property:

| กลุ่ม | Property |
| --- | --- |
| summary | `DateRangeLabel`, `SummaryItems` |
| records | `Accounts` |
| options | `AddRoleOptions`, `EditRoleOptions`, `StatusOptions` |
| permissions | `CanCreateStaffAccounts`, `CanChangeRoles` |
| forms | `AddForm`, `EditForm` |
| modal state | `ActiveModal` |

จุดสำคัญ:

- `CanCreateStaffAccounts` และ `CanChangeRoles` ไม่ใช่แค่ display แต่ view/JS ใช้เปิดปิด UI บางส่วน
- role options ถูกสร้างตามสิทธิ์ของ current admin
- `AddForm` กับ `EditForm` ใช้ class เดียวกันคือ `AdminAccountEditorViewModel`

## AdminItemsPageViewModel

หน้าที่: model หน้า inventory/items

Property:

| Property | ความหมาย |
| --- | --- |
| `DateRangeLabel` | label sync |
| `SummaryItems` | summary inventory |
| `Items` | cards สินค้า inventory |
| `Categories` | category options/filter |
| `ImageOptions` | image picker options |
| `CategoryForm` | form เพิ่ม category |
| `AddForm` | form เพิ่ม item |
| `EditForm` | form แก้ item |
| `ActiveModal` | modal ที่เปิดหลัง validation fail |

Flow:

```text
GET /Admin/Items
  -> GetAllInventoryItems()
  -> BuildInventorySummary()
  -> BuildCategories()
  -> BuildImageOptions()
  -> AdminItemsPageViewModel
```

Flow เพิ่ม category แบบ AJAX:

```text
POST AddCategory()
  -> bind AdminCategoryEditorViewModel
  -> if ajax return JSON
  -> JS เพิ่ม option/category ใน modal
```

## ActiveModal ใน admin

หลายหน้า admin มี modal form ถ้า validation fail ต้องเปิด modal เดิม

| Page model | Form | ค่า `ActiveModal` โดยประมาณ |
| --- | --- | --- |
| `AdminOrdersViewModel` | `AddForm` | `order-add` |
| `AdminCodesViewModel` | `CreateForm` | `code-create` |
| `AdminAccountsViewModel` | `AddForm`/`EditForm` | account modal |
| `AdminItemsPageViewModel` | `AddForm`/`EditForm`/`CategoryForm` | item/category modal |

Pattern:

```text
POST form
  -> ModelState invalid
  -> BuildPageModel(form: oldInput, activeModal: "...")
  -> view renders data-active-modal
  -> site.js opens modal
```

## ข้อควรระวังเวลาแก้ Admin page models

- Page model ที่มี dropdown ต้อง rebuild options ตอน validation fail ด้วย ไม่งั้น view ไม่มีรายการให้เลือก
- Page model ที่มี modal ต้องตั้ง `ActiveModal` ให้ตรงกับ JS/view
- ถ้าเปลี่ยนชื่อ property form เช่น `AddForm` ต้องแก้ `[Bind(Prefix = "AddForm")]`
- ถ้าเพิ่ม field ใหม่ใน card ต้องแก้ mapper ใน `Controllers/Admin/*.cs` ที่สร้าง record นั้น
- ถ้าเพิ่ม permission flag ต้องเช็กทั้ง view และ `wwwroot/js/site.js`
