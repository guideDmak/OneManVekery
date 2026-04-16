# View, Form, and JavaScript Hooks

เอกสารนี้สรุปจุดเชื่อมระหว่าง `.cshtml`, ViewModel, controller และ `wwwroot/js/site.js`

## หลักคิดของ `data-*`

`data-*` ใน view ทำหน้าที่เป็น contract ให้ JavaScript

```text
Razor view render HTML + data attributes
  -> site.js querySelector/querySelectorAll
  -> attach event listeners
  -> read/write values
  -> submit form หรือ update UI
```

ถ้าเปลี่ยนชื่อ `data-*` ฝั่ง view โดยไม่แก้ JS:

- search/filter อาจไม่ทำงาน
- modal อาจไม่เติมข้อมูล
- AJAX stock อาจไม่ update
- checkout review อาจไม่เปิด
- login guard อาจไม่บล็อก add to cart

## Root hooks

| Hook | View | ใช้ทำอะไร |
| --- | --- | --- |
| `data-storefront-user` | `_Layout.cshtml` | บอกว่า login เป็น user หน้าร้านไหม |
| `data-product-search` | `Shop.cshtml` | search/filter products |
| `data-checkout-form` | `Cart.cshtml` | checkout review modal |
| `data-account-profile` | `Account/Profile.cshtml` | profile/address modal flow |
| `data-admin-items` | `Admin/Items.cshtml` | items filter, image picker, stock AJAX, category modal |
| `data-admin-products` | `Admin/Products.cshtml` | publish/hide product |
| `data-admin-orders` | `Admin/Orders.cshtml` | add/edit order, combobox, dynamic lines |
| `data-admin-codes` | `Admin/Codes.cshtml` | promo code modal hints |
| `data-admin-accounts` | `Admin/Accounts.cshtml` | account search/filter/edit modal |

## Login-required add to cart

Views:

- `Views/Home/Index.cshtml`
- `Views/Home/Shop.cshtml`

form:

```html
<form asp-action="AddToCart" method="post" data-login-required-form>
```

layout:

```html
<body data-storefront-user="true/false">
```

Flow:

```text
user submit add to cart
  -> site.js checks body data-storefront-user
  -> true: submit form
  -> false: prevent submit and open siteLoginPromptModal
```

## Product search/filter

View: `Views/Home/Shop.cshtml`

hooks:

```text
data-product-search
data-search-input
data-category-input
data-product-list
data-product-card
data-name
data-category
data-results-label
data-empty-state
```

Flow:

```text
input/search changes
  -> compare search text with data-name
  -> compare selected category with data-category
  -> hide/show data-product-card
  -> update results label
```

## Checkout review modal

View: `Views/Home/Cart.cshtml`

input hooks:

```text
data-checkout-customer-field
data-checkout-phone-field
data-checkout-payment-field
data-checkout-address-field
data-checkout-notes-field
```

review hooks:

```text
data-checkout-review-modal
data-checkout-review-customer
data-checkout-review-phone
data-checkout-review-payment
data-checkout-review-address
data-checkout-review-notes
data-checkout-review-promo
data-checkout-review-reward
data-checkout-review-points
data-checkout-review-confirm
```

Flow:

```text
กด submit checkout
  -> JS prevent default
  -> อ่าน field values
  -> เติม review modal
  -> เปิด modal
  -> กดยืนยัน
  -> submit form จริง
```

ข้อควรระวัง:

- ถ้าเพิ่ม checkout field ที่ต้องโชว์ใน review modal ต้องเพิ่ม hook และ JS
- ปุ่ม promo/points ใช้ `formnovalidate` เพื่อไม่เปิด review modal แบบ checkout จริง

## Account profile modals

View: `Views/Account/Profile.cshtml`

root:

```html
data-account-profile
data-active-modal
```

address button data:

```text
data-address-id
data-address-label
data-address-recipient
data-address-phone
data-address-line
data-address-postal
data-address-default
```

field hooks:

```text
data-address-id-field
data-address-default-field
```

Flow:

```text
เปิด address modal
  -> read trigger.dataset
  -> fill AddressForm fields
```

Validation fail:

```text
controller returns ActiveModal
  -> view writes data-active-modal
  -> script opens modal after load
```

## Admin items hooks

View: `Views/Admin/Items.cshtml`

กลุ่ม search/filter:

```text
data-items-search
data-items-category-filter
data-items-sort
data-item-card
data-name
data-category
data-sku
data-price
data-stock
data-updated-at
```

กลุ่ม stock AJAX:

```text
data-stock-adjust-form
data-stock-direction-input
data-stock-amount
data-stock-direction
data-stock-count
data-stock-status-badge
data-stock-updated
```

กลุ่ม image picker:

```text
data-item-image-picker
data-item-image-upload
data-item-image-path
data-item-image-preview
data-item-image-label
data-item-image-strip
data-item-image-option
```

กลุ่ม category modal:

```text
data-open-category-modal
data-category-target
data-return-modal-id
data-add-category-form
data-category-name-input
data-category-validation
data-category-submit
```

Flow stock:

```text
submit stock form
  -> AJAX POST AdjustItemStock
  -> controller returns JSON
  -> JS update card values
```

Flow image:

```text
เลือก image option หรือ upload file
  -> JS update preview/label/hidden ImagePath
```

## Admin products hooks

View: `Views/Admin/Products.cshtml`

hooks:

```text
data-admin-products
data-product-card
data-product-id
data-product-name
data-product-notes
data-product-visibility-form
data-visibility-action-input
data-visibility-note-input
data-visibility-submit
data-product-hide-reason-form
data-product-hide-target
data-product-hide-reason-input
data-product-hide-validation
data-product-hide-confirm
```

Flow:

```text
hide action
  -> show reason modal
  -> validate reason
  -> copy reason into hidden input
  -> submit visibility form
```

## Admin orders hooks

View: `Views/Admin/Orders.cshtml`

customer picker:

```text
data-order-customer-combobox
data-order-customer-search
data-order-customer-suggestions
data-order-customer-option
data-customer-id
data-customer-name
data-customer-email
data-customer-phone
```

product picker:

```text
data-order-product-picker-combobox
data-order-product-picker-search
data-order-product-picker-suggestions
data-order-product-picker-option
data-product-id
data-product-name
data-product-meta
data-product-stock
data-product-price
data-order-product-add
```

dynamic lines:

```text
data-order-lines
data-order-line-row
data-order-line-product-id
data-order-line-qty
data-order-line-unit-price
data-order-line-total
data-remove-order-line
```

totals:

```text
data-order-subtotal
data-order-delivery-fee
data-order-grand-total
```

edit modal:

```text
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

ข้อควรระวัง:

- dynamic input name ต้องเป็น `AddForm.Items[index].ProductId` และ `AddForm.Items[index].Quantity`
- ถ้า remove row ต้อง reindex ก่อน submit หรือ JS ต้องจัดการ index ให้ model binder อ่านได้

## Admin codes hooks

View: `Views/Admin/Codes.cshtml`

hooks:

```text
data-admin-codes
data-active-modal
data-promo-campaign-select
data-promo-campaign-hint
data-promo-code-input
data-promo-discount-type
data-promo-discount-help
```

option data:

```text
data-status
data-rule
data-benefit
data-help
```

Flow:

```text
เลือก campaign
  -> update hint from data-rule/data-benefit/data-status

พิมพ์ code
  -> uppercase

เลือก discount type
  -> update help text from data-help
```

## Admin accounts hooks

View: `Views/Admin/Accounts.cshtml`

search/filter:

```text
data-account-search-form
data-account-search-input
data-account-role-filter
data-account-status-filter
data-account-sort
data-account-search-reset
data-account-results-label
data-account-search-empty
```

row data:

```text
data-account-row
data-account-search
data-account-name
data-account-role
data-account-status
data-account-last-active
data-account-protected-admin
data-account-id
```

edit modal data:

```text
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
data-protected-admin-note
```

Flow:

```text
search/filter/sort changes
  -> JS reads row data
  -> hide/show/sort rows

click edit
  -> JS reads button data
  -> fill EditForm fields
  -> adjust protected admin UI
```

## Form binding checklist

เวลาแก้ input ใน view ให้เช็ก:

1. `asp-for` ตรงกับ ViewModel property ไหม
2. ถ้าเป็น nested form prefix ตรงกับ `[Bind(Prefix = "...")]` ไหม
3. ถ้าเป็น list มี index ถูกไหม เช่น `AddForm.Items[0].ProductId`
4. submit button ส่ง `asp-action` ถูกไหม
5. มี `asp-validation-for` หรือ validation summary ไหม
6. ถ้า field ใช้ JS ต้องมี `data-*` ครบไหม
7. ถ้า field อยู่ใน edit modal มี data attribute สำหรับเติมค่าเดิมไหม

## Modal checklist

เวลาแก้ modal ให้เช็ก:

1. `id` modal ตรงกับ `data-bs-target` ไหม
2. form action ตรงกับ controller POST ไหม
3. `ActiveModal` ถูก set ตอน validation fail ไหม
4. root view มี `data-active-modal` ไหม
5. `_ValidationScriptsPartial` ถูก include ไหม
6. JS fill ค่าเดิมครบไหม
7. ปุ่ม cancel มี `data-bs-dismiss="modal"` ไหม

## สรุป

- `asp-*` คือ contract กับ controller routing/model binding
- `data-*` คือ contract กับ `site.js`
- `@model` คือ contract กับ ViewModel
- `ViewData` คือข้อมูลข้าม view-layout
- modal/form ที่ซับซ้อนต้องรักษา contract ทั้ง 4 จุดพร้อมกัน
