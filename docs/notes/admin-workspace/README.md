# Admin Workspace Notes

เอกสารนี้อธิบายหลังบ้านทั้งหมด: dashboard, orders, products, items, promo codes, staff, accounts, profile และ JS/data attribute ที่ผูกกับ view

## ไฟล์หลัก

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Controllers/AdminController.cs` | constructor, shared fields และ `OnActionExecuting` guard ของหลังบ้าน |
| `Controllers/Admin/Dashboard.cs` | dashboard action และ metric builders |
| `Controllers/Admin/Orders.cs` | orders page, add order, update status, order helpers |
| `Controllers/Admin/Items.cs` | inventory page, category/item actions, stock adjust helpers |
| `Controllers/Admin/InventoryData.cs` | query inventory records ที่ใช้ร่วมกันระหว่าง Items/Products |
| `Controllers/Admin/ItemImages.cs` | validate/upload/list รูปสินค้า |
| `Controllers/Admin/Products.cs` | storefront product visibility |
| `Controllers/Admin/Codes.cs` | promo code actions และ code dashboard |
| `Controllers/Admin/PromotionRows.cs` | helper สร้างแถว/label ของ promotions |
| `Controllers/Admin/Accounts.cs` | account management actions และ permission helpers |
| `Controllers/Admin/Staff.cs` | staff directory |
| `Controllers/Admin/Profile.cs` | admin profile |
| `Controllers/Admin/Common.cs` | helper session/admin role ที่ใช้ร่วมกัน |
| `ViewModel/AdminViewModels.cs` | view model สำหรับ admin pages |
| `Views/Admin/Index.cshtml` | dashboard |
| `Views/Admin/Orders.cshtml` | จัดการ order และสร้าง order |
| `Views/Admin/Products.cshtml` | คุม visibility หน้าร้าน |
| `Views/Admin/Items.cshtml` | จัดการสินค้า stock category image |
| `Views/Admin/Codes.cshtml` | จัดการ promo code |
| `Views/Admin/Accounts.cshtml` | จัดการ accounts |
| `Views/Admin/Staff.cshtml` | staff directory |
| `Views/Admin/Profile.cshtml` | profile ของ admin |
| `Views/Shared/_AdminLayout.cshtml` | layout หลังบ้าน |
| `wwwroot/js/site.js` | interactivity ของ admin pages |

## Admin request guard

ตำแหน่ง: `Controllers/AdminController.cs:27-43`

`AdminController` override `OnActionExecuting` เพื่อเช็กสิทธิ์ก่อนทุก action

Flow:

```text
request เข้า AdminController
  -> OnActionExecuting()
  -> read role key from session
  -> AdminPortalAuth.CanAccessAdmin(roleKey)?
       false -> redirect Account/Login
       true  -> set ViewData signed-in name/role
  -> action ทำงานต่อ
```

ผลคือทุกหน้า admin ถูกป้องกันโดย default ไม่ต้องเขียน guard ซ้ำทุก action

## Admin actions map

| Action | ไฟล์ | Method | View/ผลลัพธ์ | หน้าที่ |
| --- | --- | --- | --- | --- |
| Dashboard | `Controllers/Admin/Dashboard.cs` | `Index()` | `Index.cshtml` | metrics, trends, top products, latest orders |
| Orders | `Controllers/Admin/Orders.cs` | `Orders()` | `Orders.cshtml` | list order และ modal จัดการ order |
| Add order | `Controllers/Admin/Orders.cs` | `AddOrder()` | redirect Orders | สร้าง order จากหลังบ้าน |
| Update order | `Controllers/Admin/Orders.cs` | `UpdateOrderStatus()` | redirect Orders | เปลี่ยน order/payment status |
| Customers | `Controllers/Admin/Staff.cs` | `Customers()` | redirect Staff | alias เก่าไป Staff |
| Staff | `Controllers/Admin/Staff.cs` | `Staff()` | `Staff.cshtml` | staff directory เฉพาะ role ที่ดูได้ |
| Products | `Controllers/Admin/Products.cs` | `Products()` | `Products.cshtml` | storefront visibility |
| Codes | `Controllers/Admin/Codes.cs` | `Codes()` | `Codes.cshtml` | promo code dashboard |
| Create promo | `Controllers/Admin/Codes.cs` | `CreatePromoCode()` | redirect Codes | สร้าง promo code |
| Update promo status | `Controllers/Admin/Codes.cs` | `UpdatePromoCodeStatus()` | redirect Codes | active/paused/expired |
| Set visibility | `Controllers/Admin/Products.cs` | `SetProductVisibility()` | JSON หรือ redirect | publish/hide product |
| Items | `Controllers/Admin/Items.cs` | `Items()` | `Items.cshtml` | inventory management |
| Add category | `Controllers/Admin/Items.cs` | `AddCategory()` | JSON หรือ view/redirect | เพิ่มหมวดสินค้า |
| Add item | `Controllers/Admin/Items.cs` | `AddItem()` | redirect Items | เพิ่มสินค้า |
| Update item | `Controllers/Admin/Items.cs` | `UpdateItem()` | redirect Items | แก้สินค้า |
| Adjust stock | `Controllers/Admin/Items.cs` | `AdjustItemStock()` | JSON หรือ redirect | เพิ่ม/ลด stock |
| Accounts | `Controllers/Admin/Accounts.cs` | `Accounts()` | `Accounts.cshtml` | account management |
| Add account | `Controllers/Admin/Accounts.cs` | `AddAccount()` | redirect Accounts | เพิ่มบัญชี |
| Update account | `Controllers/Admin/Accounts.cs` | `UpdateAccount()` | redirect Accounts | แก้บัญชี |
| Close account | `Controllers/Admin/Accounts.cs` | `CloseAccount()` | redirect Accounts | ปิดบัญชี |
| Profile | `Controllers/Admin/Profile.cs` | `Profile()` | `Profile.cshtml` | profile admin |

## Dashboard

### Flow

```text
GET /Admin
  -> Index()
  -> BuildDashboardModel()
     -> load orders + items + customers
     -> BuildDashboardMetrics()
     -> BuildDashboardTrendChart()
     -> BuildOrderFulfillmentSummary()
     -> BuildDashboardTopProducts()
     -> latest orders
  -> Views/Admin/Index.cshtml
```

### `BuildDashboardModel()`

ตำแหน่ง: `Controllers/Admin/Dashboard.cs:23`

หน้าที่:

- โหลด orders พร้อม items/promotions
- โหลด products
- โหลด customers role user
- คำนวณ revenue 7 วัน
- สร้าง dashboard view model

View model: `AdminDashboardViewModel` ที่ `ViewModel/AdminViewModels.cs:6-25`

| Property | ใช้ทำอะไร |
| --- | --- |
| `DateRangeLabel` | วันที่ sync บน topbar |
| `Metrics` | card metrics |
| `TrendLabel/TrendValue/TrendDelta` | summary graph |
| `TrendChart` | จุดกราฟรายวัน |
| `SummaryItems` | fulfillment summary |
| `TopProducts` | สินค้าขายดีหลังบ้าน |
| `LatestOrders` | order ล่าสุด 8 รายการ |

## Orders page

### Flow list

```text
GET /Admin/Orders
  -> BuildOrdersModel()
     -> load orders + order items
     -> build metrics/chart/summary
     -> build status/payment options
     -> build customer/product picker options
  -> Views/Admin/Orders.cshtml
```

### AddOrder POST

ตำแหน่ง: `Controllers/Admin/Orders.cs:25`

ลำดับ:

1. ensure `form.Items` ไม่ null
2. load customer จาก `form.UserId`
3. validate customer
4. group requested items ตาม product id
5. validate มีสินค้าอย่างน้อย 1 รายการ
6. load products
7. validate product active และ stock พอ
8. ถ้า invalid กลับ `Orders` พร้อม `activeModal = "order-add"`
9. คำนวณ subtotal
10. เปิด transaction
11. สร้าง `Order`
12. สร้าง `OrderItems`
13. ลด stock
14. save และ commit

### UpdateOrderStatus POST

ตำแหน่ง: `Controllers/Admin/Orders.cs:141`

หน้าที่:

- หา order
- validate edit form
- normalize order/payment status
- update note
- save changes

### Orders view

ไฟล์: `Views/Admin/Orders.cshtml`

โครงสร้างหลัก:

- page banner + add order button
- metrics grid
- chart/summary
- orders table
- order receipt modals
- add order modal
- edit order status modal

`data-*` สำคัญ:

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-admin-orders` | root สำหรับ JS order management |
| `data-add-order-form` | form สร้าง order |
| `data-order-customer-*` | combobox เลือกลูกค้า |
| `data-order-product-picker-*` | combobox เลือกสินค้า |
| `data-order-lines` | tbody ของ order line |
| `data-order-line-row` | row สินค้าใน order ที่ JS clone/แก้ |
| `data-order-subtotal` | แสดง subtotal |
| `data-order-delivery-fee` | แสดงค่าส่ง |
| `data-order-grand-total` | แสดงยอดรวม |

## Items page

### Flow list

```text
GET /Admin/Items
  -> BuildItemsModel()
     -> GetAllInventoryItems()
     -> BuildInventorySummary()
     -> BuildCategories()
     -> BuildImageOptions()
  -> Views/Admin/Items.cshtml
```

### AddCategory

ตำแหน่ง: `Controllers/Admin/Items.cs:25`

รองรับทั้ง normal POST และ AJAX:

- normalize category name
- validate not empty
- ถ้ามีอยู่แล้ว return success พร้อม categoryName
- ถ้ายังไม่มี สร้าง `Category`
- ถ้า AJAX return JSON
- ถ้า normal redirect Items

### AddItem

ตำแหน่ง: `Controllers/Admin/Items.cs:92`

ลำดับ:

1. เช็ก SKU ซ้ำ
2. validate image upload
3. ถ้า invalid เปิด modal add กลับมา
4. apply uploaded image ถ้ามี
5. create inventory input
6. add product

### UpdateItem

ตำแหน่ง: `Controllers/Admin/Items.cs:119`

คล้าย AddItem แต่:

- ต้องมี `ItemId`
- เช็ก SKU ซ้ำโดย excluding current item
- update product เดิม

### AdjustItemStock

ตำแหน่ง: `Controllers/Admin/Items.cs:152`

ลำดับ:

- หา item
- validate amount > 0
- direction `< 0` คือ decrement, ไม่งั้น increment
- ถ้าลดเกิน stock ปัจจุบัน return error
- เรียก `AdjustInventoryStock`
- return ผ่าน `HandleItemStockResult`

`HandleItemStockResult` รองรับทั้ง AJAX และ normal post ทำให้หน้า Items ปรับ stock ได้แบบไม่ reload ถ้า request มาจาก JS

### Items view

ไฟล์: `Views/Admin/Items.cshtml`

ส่วนสำคัญ:

| ส่วน | อธิบาย |
| --- | --- |
| summary grid | สรุปจำนวนสินค้า/stock/category |
| category chips | แสดงหมวดที่มี |
| toolbar | search/filter/sort |
| item cards | cards สินค้า |
| add item modal | เพิ่มสินค้า |
| edit item modal | แก้สินค้า |
| add category modal | เพิ่มหมวดระหว่างอยู่ใน modal item |

`data-*` สำคัญ:

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-admin-items` | root JS |
| `data-items-grid` | grid ที่ JS reorder cards |
| `data-item-card` | card สินค้าแต่ละใบ |
| `data-items-search` | search input |
| `data-items-category-filter` | filter หมวด |
| `data-items-sort` | sort |
| `data-stock-adjust-form` | form เพิ่ม/ลด stock |
| `data-stock-direction` | ปุ่ม + หรือ - |
| `data-stock-count` | จำนวน stock ที่ update หลัง AJAX |
| `data-item-image-picker` | image picker ใน modal |
| `data-open-category-modal` | เปิด modal เพิ่มหมวด |

## Products page

หน้า Products ไม่ได้แก้สินค้าโดยตรง แต่คุมว่า item ไหนเปิดขายบนหน้าร้าน

### Flow

```text
GET /Admin/Products
  -> BuildProductsModel()
     -> GetAllInventoryItems()
     -> sales/revenue summary
     -> MapProductShowcase()
  -> Views/Admin/Products.cshtml
```

### SetProductVisibility

ตำแหน่ง: `Controllers/Admin/Products.cs:25`

รองรับ AJAX และ normal POST

ลำดับ:

1. หา product
2. validate action ต้องเป็น `publish` หรือ `hide`
3. ถ้า hide ต้องมีเหตุผล
4. เรียก `SetPublishedState`
5. reload product ล่าสุด
6. ถ้า AJAX return JSON payload
7. ถ้า normal redirect Products

### Products view

ไฟล์: `Views/Admin/Products.cshtml`

`data-*` สำคัญ:

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-admin-products` | root JS |
| `data-product-card` | card สินค้า |
| `data-product-visibility-form` | form publish/hide |
| `data-visibility-action-input` | hidden input action |
| `data-visibility-note-input` | hidden input note |
| `data-visibility-submit` | button publish/hide |
| `data-product-hide-reason-form` | modal form ขอเหตุผลก่อน hide |
| `data-product-hide-reason-input` | textarea เหตุผล |

## Codes page

### Flow

```text
GET /Admin/Codes
  -> BuildCodesModel()
     -> load promotions + promo codes
     -> build metrics
     -> build promotion rows
     -> build create form options
  -> Views/Admin/Codes.cshtml
```

### CreatePromoCode

ตำแหน่ง: `Controllers/Admin/Codes.cs:25`

ลำดับ:

1. `ValidatePromoCodeForm(form)`
2. ถ้า invalid กลับ Codes พร้อม `activeModal = "code-create"`
3. สร้าง record ผ่าน `CreatePromoCodeRecord`
4. redirect Codes

### UpdatePromoCodeStatus

ตำแหน่ง: `Controllers/Admin/Codes.cs:42`

ลำดับ:

- normalize status
- validate status อยู่ใน allowed values
- หา promo code
- ถ้าเปิด active แต่หมดอายุแล้ว ห้ามเปิด
- update status

### Codes view

ไฟล์: `Views/Admin/Codes.cshtml`

`data-*` สำคัญ:

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-admin-codes` | root JS |
| `data-active-modal` | เปิด modal create หลัง validation fail |
| `data-promo-campaign-select` | dropdown campaign |
| `data-promo-campaign-hint` | hint ของ campaign ที่เลือก |
| `data-promo-code-input` | input code ที่ JS uppercase |
| `data-promo-discount-type` | dropdown type |
| `data-promo-discount-help` | help text ตาม type |

## Accounts page

### Flow

```text
GET /Admin/Accounts
  -> BuildAccountsModel()
     -> GetAllAccounts()
     -> build role/status options
     -> determine current admin permissions
  -> Views/Admin/Accounts.cshtml
```

### Role guard

บัญชีหลังบ้านมี logic สิทธิ์:

- Staff สร้าง account ได้จำกัด
- Admin/Owner จัดการ role ได้มากกว่า
- protected admin account ห้ามปิดหรือเปลี่ยน status ออกจาก Active

### AddAccount

ตำแหน่ง: `Controllers/Admin/Accounts.cs:25`

- validate password
- validate role ที่ current admin assign ได้
- validate email ซ้ำ
- create account

### UpdateAccount

ตำแหน่ง: `Controllers/Admin/Accounts.cs:59`

- หา account
- ถ้า current role เปลี่ยน role ไม่ได้ ให้คง role เดิม
- ถ้า protected admin ไม่ให้เปลี่ยน role/status
- validate email ซ้ำ
- update account

### CloseAccount

ตำแหน่ง: `Controllers/Admin/Accounts.cs:111`

- หา account
- protected admin ห้ามปิด
- close account record

### Accounts view

ไฟล์: `Views/Admin/Accounts.cshtml`

`data-*` สำคัญ:

| data attribute | ใช้ทำอะไร |
| --- | --- |
| `data-admin-accounts` | root JS |
| `data-can-change-roles` | บอก JS ว่า current admin เปลี่ยน role ได้ไหม |
| `data-account-search-form` | search/filter form |
| `data-account-row` | row account |
| `data-account-search` | string สำหรับค้นหา |
| `data-account-role` | filter role |
| `data-account-status` | filter status |
| `data-account-last-active` | sort |
| `data-protected-admin-note` | note แสดงตอน account protected |

## Staff page

ตำแหน่ง action: `Controllers/Admin/Staff.cs:24`

เงื่อนไข:

- ต้องผ่าน admin guard ก่อน
- ต้องผ่าน `AdminPortalAuth.CanManageStaffDirectory`
- ถ้าไม่มีสิทธิ์ redirect dashboard

หน้าที่:

- แสดง staff directory
- ใช้ `BuildStaffModel()`
- view คือ `Views/Admin/Staff.cshtml`

## Admin profile

ตำแหน่ง action: `Controllers/Admin/Profile.cs:18`

หน้าที่:

- แสดง account ปัจจุบันของ admin
- ใช้ `BuildProfileModel()`
- view คือ `Views/Admin/Profile.cshtml`

## Admin view models

ไฟล์: `ViewModel/AdminViewModels.cs`

| Class | บรรทัด | ใช้หน้า |
| --- | --- | --- |
| `AdminDashboardViewModel` | 6 | Dashboard |
| `AdminOrdersViewModel` | 27 | Orders |
| `AdminStaffViewModel` | 62 | Staff |
| `AdminProductsViewModel` | 71 | Products |
| `AdminCodesViewModel` | 82 | Codes |
| `AdminPromoCodeEditorViewModel` | 130 | Create promo code modal |
| `AdminProductShowcaseViewModel` | 171 | Product visibility cards |
| `AdminProfileViewModel` | 214 | Admin profile |
| `AdminAccountsViewModel` | 269 | Accounts |
| `AdminItemsPageViewModel` | 294 | Items |
| `AdminItemEditorViewModel` | 363 | Add/edit item modal |
| `AdminAccountEditorViewModel` | 431 | Add/edit account modal |
| `AdminOrderRecordViewModel` | 497 | Orders table/dashboard |
| `AdminOrderEditorViewModel` | 568 | Edit order modal |
| `AdminOrderCreateViewModel` | 598 | Add order modal |

## Admin JS sections in `wwwroot/js/site.js`

| Root selector | ช่วงโดยประมาณ | หน้า | หน้าที่ |
| --- | --- | --- | --- |
| `data-admin-items` | 259 เป็นต้นไป | Items | filter/sort cards, image picker, AJAX stock, add category modal, edit modal fill |
| `data-admin-products` | 860 เป็นต้นไป | Products | AJAX publish/hide, hide reason modal, update card |
| `data-admin-accounts` | 1066 เป็นต้นไป | Accounts | search/filter/sort rows, fill edit modal, protected admin state |
| `data-admin-codes` | 1273 เป็นต้นไป | Codes | uppercase promo code, campaign hint, discount help |
| `data-admin-orders` | 1332 เป็นต้นไป | Orders | customer combobox, product picker, add/remove line, sync subtotal |

## ข้อควรระวังเวลาแก้ Admin

- `OnActionExecuting` เป็น guard หลัก อย่า bypass โดยไม่ตั้งใจ
- POST ทุกตัวควรมี `[ValidateAntiForgeryToken]`
- action ที่รองรับ AJAX ต้องรักษา response shape เดิม เพราะ `site.js` ใช้ payload
- Items และ Products ใช้ product table เดียวกัน แต่หน้า Products คุม publish/hide ส่วน Items คุมข้อมูลจริง
- การเพิ่ม order จาก admin ลด stock ด้วย transaction เหมือน checkout ควรรักษาไว้
- การปิด account ต้องกัน protected admin เสมอ
