# Account Views

โฟลเดอร์หลัก: `Views/Account/`

กลุ่มนี้ดูแล login, register, register address และ profile ลูกค้า

## แผนที่ไฟล์

| View | Model | Action | หน้าที่ |
| --- | --- | --- | --- |
| `Login.cshtml` | `LoginViewModel` | `AccountController.Login()` | เข้าสู่ระบบ |
| `Register.cshtml` | `RegisterViewModel` | `AccountController.Register()` | สมัคร step 1 |
| `RegisterAddress.cshtml` | `RegisterAddressViewModel` | `RegisterAddress()`, `CompleteRegistration()` | สมัคร step 2 ที่อยู่ |
| `Profile.cshtml` | `AccountProfileViewModel` | `Profile()`, `UpdateProfile()`, `SaveAddress()` | profile, points, addresses, modals |

## Auth layout state

หน้า auth ทั้ง 3 หน้า:

- `Login.cshtml`
- `Register.cshtml`
- `RegisterAddress.cshtml`

ตั้ง:

```csharp
ViewData["HideChrome"] = true;
```

ผล:

- `_Layout.cshtml` ซ่อน navbar/footer
- body ได้ class auth
- เหมาะกับหน้าฟอร์มเต็มจอ

## `Login.cshtml`

หน้าที่:

- แสดง form email/password
- link ไป register
- include validation scripts

form:

```html
<form asp-action="Login" method="post" class="auth-form">
```

field:

| Input | ViewModel |
| --- | --- |
| email | `LoginViewModel.Email` |
| password | `LoginViewModel.Password` |

Flow:

```text
GET /Account/Login
  -> return View(new LoginViewModel())

POST /Account/Login
  -> bind LoginViewModel
  -> validate
  -> Authenticate()
  -> redirect by role
```

## `Register.cshtml`

หน้าที่:

- สมัครสมาชิก step 1
- รับชื่อ email เบอร์ password confirm password
- ส่งต่อไป register address เมื่อผ่าน validation

form:

```html
<form asp-action="Register" method="post" class="auth-form auth-form-register">
```

field:

| Input | ViewModel |
| --- | --- |
| ชื่อ | `RegisterViewModel.FullName` |
| email | `RegisterViewModel.Email` |
| เบอร์ | `RegisterViewModel.PhoneNumber` |
| password | `RegisterViewModel.Password` |
| confirm password | `RegisterViewModel.ConfirmPassword` |

Flow:

```text
POST /Account/Register
  -> bind RegisterViewModel
  -> duplicate email check
  -> write pending registration to session
  -> redirect RegisterAddress
```

## `RegisterAddress.cshtml`

หน้าที่:

- สมัครสมาชิก step 2
- แสดงข้อมูลบัญชีจาก step 1
- รับที่อยู่จัดส่งเริ่มต้น
- มี link กลับไปแก้ข้อมูลบัญชีด้วย `restore=true`

form:

```html
<form asp-action="CompleteRegistration" method="post" class="auth-form auth-form-register auth-form-register-address">
```

field:

| Input | ViewModel |
| --- | --- |
| label | `RegisterAddressViewModel.Label` |
| recipient | `RegisterAddressViewModel.RecipientName` |
| phone | `RegisterAddressViewModel.PhoneNumber` |
| address | `RegisterAddressViewModel.AddressLine` |
| postal code | `RegisterAddressViewModel.PostalCode` |

link กลับ:

```html
<a asp-action="Register" asp-route-restore="true">
```

ความหมาย:

- กลับไปหน้า register
- controller อ่าน pending registration ใน session แล้วเติม form เดิม

## `Profile.cshtml`

หน้าที่:

- แสดงข้อมูลบัญชี
- แสดง points
- แสดง address cards
- modal แก้ profile
- modal เพิ่ม/แก้ address
- inline script สำหรับเปิด modal และเติม address data

root:

```html
<section class="account-profile-section" data-account-profile data-active-modal="@Model.ActiveModal">
```

ความหมาย:

- `data-account-profile` เป็น root ให้ inline script หา section
- `data-active-modal` บอกว่าหลัง validation fail ต้องเปิด modal ไหน

### ข้อมูลบัญชี

ใช้ property:

| Model property | แสดงอะไร |
| --- | --- |
| `FullName` | ชื่อ |
| `Email` | email |
| `PhoneNumber` | เบอร์ |
| `RoleLabel` | role |
| `StatusLabel` | status |
| `AccountCode` | code |
| `LastActiveAt` | active ล่าสุด |

### Points card

ใช้:

| Model property | แสดงอะไร |
| --- | --- |
| `CurrentPoints` | แต้มปัจจุบัน |
| `LifetimeEarnedPoints` | แต้มสะสม |
| `LifetimeRedeemedPoints` | แต้มที่ใช้ไป |

มี link:

- `Home/MyOrders`
- `Home/Shop`

### Address cards

ใช้:

```text
Model.Addresses
  -> AccountAddressCardViewModel
```

ถ้าไม่มี address จะแสดง empty state พร้อมปุ่มเพิ่ม

ปุ่ม edit address ใส่ข้อมูลผ่าน `data-address-*`:

| Attribute | ค่า |
| --- | --- |
| `data-address-id` | address id |
| `data-address-label` | label |
| `data-address-recipient` | recipient |
| `data-address-phone` | phone |
| `data-address-line` | address |
| `data-address-postal` | postal |
| `data-address-default` | true/false |

### Edit profile modal

form:

```html
<form asp-action="UpdateProfile" method="post" class="account-profile-edit-form">
```

fields ใช้:

```text
EditForm.FullName
EditForm.Email
EditForm.PhoneNumber
```

controller รับ:

```csharp
[Bind(Prefix = "EditForm")] AccountProfileEditViewModel form
```

### Edit address modal

form:

```html
<form asp-action="SaveAddress" method="post" class="account-profile-edit-form">
```

fields ใช้:

```text
AddressForm.AddressId
AddressForm.Label
AddressForm.RecipientName
AddressForm.PhoneNumber
AddressForm.AddressLine
AddressForm.PostalCode
AddressForm.IsDefault
```

controller รับ:

```csharp
[Bind(Prefix = "AddressForm")] AccountAddressEditViewModel form
```

### Inline script

อยู่ใน `@section Scripts`

หน้าที่:

- include validation scripts
- อ่าน `data-active-modal`
- ถ้า `profile-edit` เปิด profile modal
- ถ้า `address-edit` เปิด address modal
- ตอนเปิด address modal อ่าน `data-address-*` จากปุ่มที่กด
- เติมค่าไปที่ field ที่มี hook เช่น `data-address-id-field`, `data-address-default-field`

Flow address edit:

```text
user กดแก้ไข address
  -> Bootstrap show modal
  -> event show.bs.modal
  -> script อ่าน button.dataset
  -> set value into AddressForm fields
```

## ข้อควรระวังเวลาแก้ Account views

- หน้า auth ต้องคง `ViewData["HideChrome"] = true` ถ้าต้องการหน้าฟอร์มเต็มจอ
- ถ้าเพิ่ม field register ต้องแก้ทั้ง `Register.cshtml`, `RegisterAddress.cshtml` ถ้าต้องแสดง summary, ViewModel และ controller flow
- ถ้าเปลี่ยนชื่อ `EditForm`/`AddressForm` ต้องแก้ `[Bind(Prefix)]`
- ถ้าเพิ่ม field address modal ต้องเพิ่ม `data-address-*` และ inline script fill ค่าเดิม
- ถ้า validation fail แล้ว modal ไม่เปิด ให้เช็ก `Model.ActiveModal` และ `data-active-modal`
- อย่าลบ `_ValidationScriptsPartial` จาก form pages
