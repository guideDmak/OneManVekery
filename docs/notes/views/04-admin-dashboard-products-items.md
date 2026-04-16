# Admin Views: Dashboard, Products, Items, Staff, Profile

โฟลเดอร์หลัก: `Views/Admin/`

เอกสารนี้อธิบายหน้า admin กลุ่ม dashboard/product inventory และหน้าที่ไม่ซับซ้อนเท่า Orders/Codes/Accounts

## Shared admin pattern

ทุกหน้า admin ใช้:

```csharp
Layout = "_AdminLayout";
ViewData["AdminSection"] = "...";
```

ผล:

- `_AdminLayout.cshtml` ครอบหน้า
- sidebar highlight ตาม `AdminSection`
- topbar ใช้ `ViewData["Title"]`
- ทุกหน้าโหลด `site.js`

## `Index.cshtml`

Model: `AdminDashboardViewModel`

Action: `AdminController.Index()`

หน้าที่:

- แสดง dashboard metrics
- แสดง trend chart
- แสดง fulfillment summary
- แสดง top products
- แสดง latest orders

Model ที่ใช้:

| Property | ใช้ทำอะไร |
| --- | --- |
| `Metrics` | metric cards |
| `TrendLabel`, `TrendValue`, `TrendDelta` | summary graph |
| `TrendChart` | bars/points graph |
| `SummaryItems` | fulfillment summary |
| `TopProducts` | top product list |
| `LatestOrders` | latest order table/list |

Flow:

```text
GET /Admin
  -> Admin guard
  -> BuildDashboardModel()
  -> Views/Admin/Index.cshtml
```

จุดสำคัญ:

- เป็นหน้า read-only ไม่มี POST form
- ใช้ข้อมูลที่ controller format มาแล้วเป็น label ส่วนใหญ่

## `Products.cshtml`

Model: `AdminProductsViewModel`

Action:

- GET `AdminController.Products()`
- POST `AdminController.SetProductVisibility()`

หน้าที่:

- คุมว่าสินค้าชิ้นไหน publish/hide บนหน้าร้าน
- แสดง sales/revenue/stock summary
- ถ้าจะ hide ต้องกรอก reason ผ่าน modal

root:

```html
<section class="admin-orders-section" data-admin-products>
```

cards:

```html
data-product-card
data-product-id
data-product-name
data-product-notes
```

visibility form:

```html
<form asp-action="SetProductVisibility" method="post" class="admin-product-visibility-form" data-product-visibility-form>
```

hidden inputs:

| Input | ใช้ทำอะไร |
| --- | --- |
| `productId` | id สินค้า |
| `visibilityAction` | `publish` หรือ `hide` |
| `visibilityNote` | เหตุผล/notes |

`data-*` สำคัญ:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-admin-products` | root JS |
| `data-product-card` | card สินค้า |
| `data-product-visibility-form` | form publish/hide |
| `data-visibility-action-input` | hidden action |
| `data-visibility-note-input` | hidden note |
| `data-visibility-submit` | ปุ่ม publish/hide |
| `data-product-hide-reason-form` | modal form สำหรับ hide reason |
| `data-product-hide-target` | ชื่อสินค้าที่กำลัง hide |
| `data-product-hide-reason-input` | textarea reason |
| `data-product-hide-validation` | error ฝั่ง client |
| `data-product-hide-confirm` | ยืนยัน hide |

Flow hide:

```text
user กด Hide From Store
  -> site.js เปิด modal reason
  -> user กรอกเหตุผล
  -> JS ใส่ reason ลง visibilityNote
  -> submit form SetProductVisibility
  -> controller update IsPublished/Notes
```

Flow publish:

```text
user กด Publish
  -> form submit ได้ทันที
  -> controller set published
```

## `Items.cshtml`

Model: `AdminItemsPageViewModel`

Actions:

- GET `AdminController.Items()`
- POST `AddItem`
- POST `UpdateItem`
- POST `AdjustItemStock`
- POST `AddCategory`

หน้าที่:

- แสดงสินค้า inventory เป็น cards
- filter/search/sort สินค้า
- เพิ่ม/แก้สินค้า
- upload/เลือกภาพสินค้า
- เพิ่ม category จาก modal ซ้อน
- เพิ่ม/ลด stock แบบ AJAX

root:

```html
<section class="admin-items-page" data-admin-items data-active-modal="@Model.ActiveModal">
```

### Toolbar

`data-*`:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-items-results-label` | จำนวนผลลัพธ์ |
| `data-items-search` | search input |
| `data-items-category-filter` | filter category |
| `data-items-sort` | sort |
| `data-open-category-modal` | เปิด modal เพิ่ม category |
| `data-items-empty-state` | empty state |
| `data-items-grid` | container cards |

### Item cards

card มี:

```html
data-item-card
data-item-id
data-name
data-category
data-sku
data-price
data-stock
data-updated-at
```

ใช้สำหรับ:

- search
- filter
- sort
- fill edit modal

### Stock adjust

form:

```html
<form asp-action="AdjustItemStock" method="post" class="admin-stock-adjust-form" data-stock-adjust-form>
```

hooks:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-stock-adjust-form` | JS intercept AJAX |
| `data-stock-direction-input` | hidden direction |
| `data-stock-amount` | จำนวนที่ปรับ |
| `data-stock-direction` | ปุ่ม -/+ |
| `data-stock-count` | update ตัวเลข stock หลังสำเร็จ |
| `data-stock-status-badge` | update badge status |
| `data-stock-updated` | update label เวลา |

Flow:

```text
user กด + หรือ -
  -> JS set quantityDirection
  -> submit AJAX
  -> controller returns stock payload
  -> JS update count/status/updated label
```

### Add item modal

form:

```html
<form asp-action="AddItem" method="post" enctype="multipart/form-data" class="admin-item-form">
```

ใช้ `AddForm.*`

field สำคัญ:

| Field | ViewModel |
| --- | --- |
| name | `AddForm.Name` |
| category | `AddForm.Category` |
| sku | `AddForm.Sku` |
| price | `AddForm.Price` |
| stock | `AddForm.StockQuantity` |
| reorder level | `AddForm.ReorderLevel` |
| tagline | `AddForm.Tagline` |
| notes | `AddForm.Notes` |
| image path | `AddForm.ImagePath` |
| image file | `AddForm.ImageFile` |
| published | `AddForm.IsPublished` |

### Edit item modal

form:

```html
<form asp-action="UpdateItem" method="post" enctype="multipart/form-data" class="admin-item-form">
```

ใช้ `EditForm.*`

ปุ่ม edit บน card เก็บข้อมูลเดิมผ่าน:

```html
data-item-id
data-item-code
data-name
data-category
data-sku
data-price
data-stock-quantity
data-reorder-level
data-tagline
data-notes
data-image-path
data-is-published
```

JS อ่านค่าเหล่านี้ไปเติมใน modal

### Image picker

hooks:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-item-image-picker` | root image picker |
| `data-item-image-upload` | file input |
| `data-item-image-path` | hidden path |
| `data-item-image-preview` | preview image |
| `data-item-image-label` | label path |
| `data-item-image-strip` | options strip |
| `data-item-image-option` | image option |

### Add category modal

form:

```html
<form asp-action="AddCategory" method="post" class="admin-item-form" data-add-category-form>
```

hooks:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-add-category-form` | JS intercept AJAX |
| `data-category-name-input` | category name |
| `data-category-validation` | validation message |
| `data-category-submit` | submit button |
| `data-category-target` | select field ที่จะเติม category |
| `data-return-modal-id` | modal ที่จะเปิดกลับ |

Flow:

```text
user เปิด category modal จาก add/edit item
  -> button ส่ง target field และ return modal id
  -> submit AddCategory
  -> controller returns JSON
  -> JS เพิ่ม option ใน category selects
  -> กลับไป add/edit item modal
```

## `Staff.cshtml`

Model: `AdminStaffViewModel`

Action: `AdminController.Staff()`

หน้าที่:

- แสดง staff directory
- แสดง metrics ของ staff
- read-only เป็นหลัก

เงื่อนไข:

- ต้องผ่าน admin guard
- ต้องมีสิทธิ์ `CanManageStaffDirectory`
- ถ้าไม่มีสิทธิ์ controller redirect กลับ dashboard

## `Profile.cshtml`

Model: `AdminProfileViewModel`

Action: `AdminController.Profile()`

หน้าที่:

- แสดงข้อมูล admin ปัจจุบัน
- แสดง summary items
- read-only

ข้อมูล:

| Property | ใช้ทำอะไร |
| --- | --- |
| `FullName`, `AccountCode` | identity |
| `Role`, `Status` | role/status |
| `Email`, `Phone` | contact |
| `LastActiveLabel` | active ล่าสุด |
| `Notes` | note |
| `SummaryItems` | summary cards |

## ข้อควรระวังเวลาแก้กลุ่มนี้

- `Items.cshtml` มี form และ JS hooks เยอะที่สุดในกลุ่มนี้ ถ้าเปลี่ยน `data-*` ต้องเช็ก `site.js`
- `Products.cshtml` ใช้ modal hide reason ที่ไม่ได้ submit ตรง แต่ให้ JS เอา reason ไปใส่ form หลัก
- `Items` add/edit ใช้ `enctype="multipart/form-data"` เพื่อ upload รูป อย่าลบ
- stock adjust รองรับ AJAX ต้องรักษา payload/controller behavior
- หน้า admin ทุกหน้าต้องตั้ง `ViewData["AdminSection"]` ให้ตรง sidebar
