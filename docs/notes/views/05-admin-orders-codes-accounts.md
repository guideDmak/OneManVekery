# Admin Views: Orders, Codes, Accounts

โฟลเดอร์หลัก: `Views/Admin/`

เอกสารนี้อธิบายหน้า admin ที่มี form/modal และ JavaScript หนักที่สุด: Orders, Codes, Accounts

## `Orders.cshtml`

Model: `AdminOrdersViewModel`

Actions:

- GET `AdminController.Orders()`
- POST `AddOrder`
- POST `UpdateOrderStatus`

หน้าที่:

- แสดง metric/order chart/summary
- แสดง table orders
- เปิด receipt modal ต่อ order
- add order จากหลังบ้าน
- edit order/payment status

root:

```html
<section class="admin-orders-section" data-admin-orders data-active-modal="@Model.ActiveModal">
```

### Orders table

ใช้:

```text
Model.Orders
  -> AdminOrderRecordViewModel
```

ปุ่มสำคัญ:

| ปุ่ม | ทำอะไร |
| --- | --- |
| receipt | เปิด `#orderReceiptModal-{orderId}` |
| edit | เปิด `#editOrderModal` และส่งข้อมูลผ่าน `data-order-*` |

ปุ่ม edit มีข้อมูล:

```html
data-order-id
data-order-number
data-customer-name
data-created-at
data-item-summary
data-total-amount
data-payment-method
data-order-status
data-payment-status
data-phone
data-address
data-note
```

JS ใช้เติม edit modal

### Add order modal

form:

```html
<form asp-action="AddOrder" method="post" class="admin-item-form" data-add-order-form>
```

ใช้ `AddForm.*`

customer picker:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-order-customer-id` | hidden user id |
| `data-order-customer-combobox` | root customer picker |
| `data-order-customer-search` | search input |
| `data-order-customer-suggestions` | suggestions list |
| `data-order-customer-option` | customer option |
| `data-customer-id` | id |
| `data-customer-name` | ชื่อ |
| `data-customer-email` | email |
| `data-customer-phone` | เบอร์ |
| `data-order-customer-selected` | label selected |
| `data-order-customer-phone` | phone field ที่ auto fill |

product picker:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-order-product-picker-combobox` | root product picker |
| `data-order-product-picker-id` | hidden selected product |
| `data-order-product-picker-search` | search input |
| `data-order-product-picker-suggestions` | suggestions list |
| `data-order-product-picker-option` | product option |
| `data-product-id` | product id |
| `data-product-name` | name |
| `data-product-meta` | category/sku/อื่นๆ |
| `data-product-stock` | stock |
| `data-product-price` | price |
| `data-order-product-picker-qty` | quantity |
| `data-order-product-add` | ปุ่ม add line |
| `data-order-product-picker-meta` | hint/meta selected |

order lines:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-order-lines` | tbody lines |
| `data-order-summary-empty` | row empty |
| `data-order-line-row` | row สินค้าที่เลือก |
| `data-order-line-product-id` | hidden ProductId |
| `data-order-line-qty` | quantity |
| `data-order-line-unit-price` | unit price label |
| `data-order-line-total` | line total |
| `data-remove-order-line` | remove row |
| `data-order-subtotal` | subtotal |
| `data-order-delivery-fee` | delivery fee |
| `data-order-grand-total` | grand total |

template:

```html
<template id="orderLineTemplate">
```

JS clone template เพื่อเพิ่มสินค้าใหม่ใน order

Flow add order:

```text
เลือก customer
  -> JS เติม UserId/phone
เลือก product + qty
  -> JS เพิ่ม row ใน data-order-lines
  -> input name = AddForm.Items[index].ProductId / Quantity
submit
  -> controller bind AdminOrderCreateViewModel
  -> validate stock/customer
  -> create order
```

### Edit order modal

form:

```html
<form asp-action="UpdateOrderStatus" method="post" class="admin-item-form">
```

ใช้ `EditForm.*`

Flow:

```text
กด Edit
  -> JS อ่าน data-order-*
  -> เติม EditForm fields
  -> submit UpdateOrderStatus
  -> controller update order/payment status/note
```

### Receipt modals

สร้างหนึ่ง modal ต่อ order

ใช้:

- `order.Items`
- `order.Benefits`
- totals labels
- payment/status labels

## `Codes.cshtml`

Model: `AdminCodesViewModel`

Actions:

- GET `AdminController.Codes()`
- POST `CreatePromoCode`
- POST `UpdatePromoCodeStatus`

หน้าที่:

- แสดง promo/codes dashboard
- แสดง table promotions
- update status ของ promo code
- create promo code ผ่าน modal

root:

```html
<div class="admin-codes-page" data-admin-codes data-active-modal="@Model.ActiveModal">
```

### Promotion rows

ใช้:

```text
Model.Promotions
  -> AdminPromotionRecordViewModel
```

แต่ละ row แสดง:

- code/title
- discount
- rule
- usage
- expiry
- status
- note

### Update status form

form:

```html
<form asp-action="UpdatePromoCodeStatus" method="post" class="admin-inline-form">
```

ส่ง:

- `promoCodeId`
- `targetStatus`

ใช้กับ promo code row ที่เปลี่ยนสถานะได้

### Create promo code modal

form:

```html
<form asp-action="CreatePromoCode" method="post" class="admin-item-form admin-code-create-form">
```

ใช้ `CreateForm.*`

hooks:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-promo-campaign-select` | dropdown campaign |
| `data-status` | status ของ campaign option |
| `data-rule` | rule ของ campaign option |
| `data-benefit` | benefit label |
| `data-promo-campaign-hint` | hint ตาม campaign ที่เลือก |
| `data-promo-code-input` | uppercase promo code input |
| `data-promo-discount-type` | discount type dropdown |
| `data-help` | help text ของ option |
| `data-promo-discount-help` | help text แสดงใต้ dropdown |

Flow:

```text
เปิด create modal
  -> เลือก campaign
  -> JS update campaign hint
  -> กรอก code
  -> JS uppercase code
  -> เลือก discount type
  -> JS update help text
  -> submit CreatePromoCode
```

ถ้า validation fail:

```text
controller BuildCodesModel(createForm: form, activeModal: "code-create")
  -> view render data-active-modal
  -> site.js เปิด create modal
```

## `Accounts.cshtml`

Model: `AdminAccountsViewModel`

Actions:

- GET `AdminController.Accounts()`
- POST `AddAccount`
- POST `UpdateAccount`
- POST `CloseAccount`

หน้าที่:

- แสดง account table
- search/filter/sort accounts
- add account
- edit account
- close account
- จัดการ protected admin state

root:

```html
<section data-admin-accounts data-active-modal="@Model.ActiveModal" data-can-change-roles="...">
```

### Search/filter/sort

form:

```html
<form class="admin-account-search-bar" data-account-search-form>
```

hooks:

| Hook | ใช้ทำอะไร |
| --- | --- |
| `data-account-search-input` | search text |
| `data-account-role-filter` | role filter |
| `data-account-status-filter` | status filter |
| `data-account-sort` | sort |
| `data-account-search-reset` | reset |
| `data-account-results-label` | result count |
| `data-account-search-empty` | empty state |
| `data-account-table-body` | tbody |

row data:

```html
data-account-row
data-account-search
data-account-name
data-account-role
data-account-status
data-account-last-active
data-account-protected-admin
data-account-id
```

JS ใช้ search/filter/sort และ hide/show rows

### Edit button data

ปุ่ม edit ใส่:

```html
data-account-id
data-account-code
data-full-name
data-email
data-phone-number
data-role
data-status
data-last-active
data-notes
data-protected-admin
```

JS อ่านไปเติม edit modal

### Close account form

form:

```html
<form asp-action="CloseAccount" method="post" class="admin-inline-form">
```

ส่ง `accountId`

controller เป็นคนกัน protected admin อีกชั้น

### Add account modal

form:

```html
<form asp-action="AddAccount" method="post" class="admin-item-form">
```

ใช้ `AddForm.*`

fields:

- full name
- email
- phone
- role
- status
- password
- notes

role options มาจาก `Model.AddRoleOptions`

### Edit account modal

form:

```html
<form asp-action="UpdateAccount" method="post" class="admin-item-form">
```

ใช้ `EditForm.*`

fields:

- account id/code
- full name
- email
- phone
- role
- status
- password optional
- notes
- last active display

protected admin note:

```html
data-protected-admin-note
```

ถ้า account เป็น protected admin:

- JS แสดง note
- role/status บางส่วนถูกล็อกใน UI
- controller ยังตรวจซ้ำเพื่อกัน bypass

## ข้อควรระวังเวลาแก้กลุ่มนี้

- `Orders.cshtml` มี dynamic list binding: input ต้องเป็น `AddForm.Items[index].ProductId` และ `AddForm.Items[index].Quantity`
- ถ้าเปลี่ยน product/customer picker ต้องแก้ `site.js` ส่วน `data-admin-orders`
- `Codes.cshtml` ใช้ hint จาก `data-*` บน `<option>` ถ้าเปลี่ยน options ต้องรักษา data เหล่านี้
- `Accounts.cshtml` ใช้ `data-account-search` รวมหลาย field สำหรับค้นหา อย่าลืมเพิ่ม field ใหม่เข้า search string ถ้าต้องค้นหาได้
- protected admin ต้องกันทั้ง view/JS/controller ไม่พึ่ง frontend อย่างเดียว
- ทุก POST form ต้องรักษา anti-forgery behavior จาก MVC form tag helper
