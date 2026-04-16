# Auth and Account ViewModels

ไฟล์หลัก: `ViewModel/AuthViewModels.cs`

กลุ่มนี้ใช้กับ login, สมัครสมาชิก, สมัครที่อยู่, profile ลูกค้า และ address modal

## แผนที่ class

| Class | บรรทัด | ใช้กับ | Controller |
| --- | --- | --- | --- |
| `LoginViewModel` | 5 | `Views/Account/Login.cshtml` | `AccountController.Login()` |
| `RegisterViewModel` | 17 | `Views/Account/Register.cshtml` | `AccountController.Register()` |
| `RegisterAddressViewModel` | 41 | `Views/Account/RegisterAddress.cshtml` | `RegisterAddress()`, `CompleteRegistration()` |
| `AccountProfileViewModel` | 64 | `Views/Account/Profile.cshtml` | `Profile()`, `BuildProfileViewModel()` |
| `AccountProfileEditViewModel` | 95 | edit profile modal | `UpdateProfile()` |
| `AccountAddressCardViewModel` | 112 | address cards | `BuildAddressCard()` |
| `AccountAddressEditViewModel` | 129 | add/edit address modal | `SaveAddress()` |

## LoginViewModel

หน้าที่: รับ email/password จากหน้า login

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `Email` | `[Required]`, `[EmailAddress]` | email ที่ใช้ login |
| `Password` | `[Required]`, `[StringLength(100, MinimumLength = 8)]`, `[DataType(DataType.Password)]` | password |

Flow:

```text
GET /Account/Login
  -> return View(new LoginViewModel())

POST /Account/Login
  -> bind LoginViewModel
  -> ModelState.IsValid?
  -> Authenticate(email, password)
  -> write session keys
  -> redirect by role
```

จุดสำคัญ:

- validation แรกเป็นรูปแบบ input เท่านั้น
- การตรวจว่ารหัสผ่านถูกไหมอยู่ใน `Authenticate()` ไม่ใช่ใน ViewModel
- `DataType.Password` ช่วยบอก UI helper ว่า field นี้เป็น password

## RegisterViewModel

หน้าที่: form สมัครสมาชิก step 1

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `FullName` | `[Required]` | ชื่อบัญชี |
| `Email` | `[Required]`, `[EmailAddress]` | email |
| `PhoneNumber` | `[Required]`, `[Phone]` | เบอร์โทร |
| `Password` | `[Required]`, `[StringLength]`, `[DataType(DataType.Password)]` | password |
| `ConfirmPassword` | `[Required]`, `[DataType(DataType.Password)]`, `[Compare(nameof(Password))]` | ยืนยัน password |

Flow:

```text
POST /Account/Register
  -> bind RegisterViewModel
  -> check duplicate email
  -> ModelState.IsValid?
  -> write PendingRegistrationRecord into session
  -> redirect RegisterAddress
```

เหตุผลที่ยังไม่เขียน database:

ระบบต้องให้ user กรอกที่อยู่เริ่มต้นให้ครบก่อน จึงเก็บข้อมูล step 1 ไว้ใน session ชั่วคราว

## RegisterAddressViewModel

หน้าที่: form สมัครสมาชิก step 2 และแสดง summary จาก step 1

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `AccountFullName` | ไม่มี | แสดงชื่อจาก step 1 |
| `AccountEmail` | ไม่มี | แสดง email จาก step 1 |
| `AccountPhoneNumber` | ไม่มี | แสดงเบอร์จาก step 1 |
| `Label` | default `บ้าน` | ชื่อที่อยู่ |
| `RecipientName` | `[Required]` | ชื่อผู้รับ |
| `PhoneNumber` | `[Required]`, `[Phone]` | เบอร์จัดส่ง |
| `AddressLine` | `[Required]` | ที่อยู่ |
| `PostalCode` | ไม่มี | รหัสไปรษณีย์ |

Flow:

```text
GET /Account/RegisterAddress
  -> ReadPendingRegistration()
  -> BuildRegisterAddressViewModel(pending)
  -> view

POST /Account/CompleteRegistration
  -> bind RegisterAddressViewModel
  -> ReadPendingRegistration()
  -> ModelState.IsValid?
  -> CompleteStorefrontRegistration()
  -> create user + default address
```

จุดสำคัญ:

- `AccountFullName`, `AccountEmail`, `AccountPhoneNumber` ไม่ได้มาจาก form หลักเป็น source of truth แต่ rebuild จาก pending session
- ถ้า validation fail controller ส่ง model กลับไปพร้อมข้อมูล account summary เดิม

## AccountProfileViewModel

หน้าที่: model หลักของหน้า profile ลูกค้า

Property:

| Property | ความหมาย |
| --- | --- |
| `FullName` | ชื่อบัญชี |
| `Email` | email |
| `PhoneNumber` | เบอร์โทร |
| `RoleLabel` | role ที่แสดงบนหน้า |
| `StatusLabel` | สถานะบัญชี |
| `AccountCode` | รหัสบัญชี display |
| `LastActiveAt` | เวลา active ล่าสุด |
| `CurrentPoints` | แต้มคงเหลือ |
| `LifetimeEarnedPoints` | แต้มสะสมตลอด |
| `LifetimeRedeemedPoints` | แต้มที่ใช้ไปตลอด |
| `Addresses` | list address cards |
| `EditForm` | form แก้ profile |
| `AddressForm` | form เพิ่ม/แก้ที่อยู่ |
| `ActiveModal` | modal ที่ต้องเปิดหลัง reload |

Flow:

```text
GET /Account/Profile
  -> guard login
  -> redirect admin roles to Admin/Profile
  -> load user + role + addresses
  -> BuildProfileViewModel(user)
  -> AccountProfileViewModel
  -> Views/Account/Profile.cshtml
```

เหตุผลที่มีทั้งข้อมูลแสดงผลและ form ใน model เดียว:

หน้า profile มีทั้ง card แสดงข้อมูลและ modal edit ในหน้าเดียวกัน ถ้า validation fail ต้อง render หน้าเดิมพร้อม form เดิมและ error เดิม

## AccountProfileEditViewModel

หน้าที่: รับค่าจาก modal แก้ profile

Property:

| Property | Validation |
| --- | --- |
| `FullName` | `[Required]`, `[StringLength(120)]` |
| `Email` | `[Required]`, `[EmailAddress]`, `[StringLength(120)]` |
| `PhoneNumber` | `[Required]`, `[Phone]`, `[StringLength(20)]` |

Binding:

```csharp
public IActionResult UpdateProfile([Bind(Prefix = "EditForm")] AccountProfileEditViewModel form)
```

ความหมาย:

- input ใน view อยู่ใต้ชื่อ `EditForm.FullName`, `EditForm.Email`, `EditForm.PhoneNumber`
- `[Bind(Prefix = "EditForm")]` ทำให้ ASP.NET Core bind เข้า `AccountProfileEditViewModel` ได้ถูก

ถ้า validation fail:

```text
UpdateProfile()
  -> ModelState invalid
  -> BuildProfileViewModel(user, form, activeModal: "profile-edit")
  -> Profile.cshtml
  -> inline script เปิด modal profile-edit
```

## AccountAddressCardViewModel

หน้าที่: ข้อมูลที่อยู่แต่ละใบที่แสดงใน profile

Property:

| Property | ความหมาย |
| --- | --- |
| `AddressId` | id ที่ใช้ส่งไป edit/delete |
| `Label` | ชื่อที่อยู่ เช่น บ้าน/ที่ทำงาน |
| `RecipientName` | ชื่อผู้รับ |
| `PhoneNumber` | เบอร์จัดส่ง |
| `AddressLine` | ที่อยู่เต็ม |
| `PostalCode` | รหัสไปรษณีย์ |
| `IsDefault` | เป็นที่อยู่หลักไหม |

ใช้กับปุ่ม edit:

view จะใส่ข้อมูลเหล่านี้ลง `data-address-*` แล้ว JavaScript เอาไปเติมใน modal ตอนกดแก้ไข

## AccountAddressEditViewModel

หน้าที่: รับค่าจาก modal เพิ่ม/แก้ที่อยู่

Property:

| Property | Validation | ความหมาย |
| --- | --- | --- |
| `AddressId` | ไม่มี | 0 คือเพิ่มใหม่, มากกว่า 0 คือแก้ของเดิม |
| `Label` | `[StringLength(40)]` | ชื่อที่อยู่ |
| `RecipientName` | `[Required]`, `[StringLength(100)]` | ชื่อผู้รับ |
| `PhoneNumber` | `[Required]`, `[Phone]`, `[StringLength(20)]` | เบอร์จัดส่ง |
| `AddressLine` | `[Required]` | ที่อยู่ |
| `PostalCode` | `[StringLength(12)]` | รหัสไปรษณีย์ |
| `IsDefault` | bool | ตั้งเป็นที่อยู่หลัก |

Binding:

```csharp
public IActionResult SaveAddress([Bind(Prefix = "AddressForm")] AccountAddressEditViewModel form)
```

Flow:

```text
POST /Account/SaveAddress
  -> bind AddressForm
  -> guard login
  -> load user + addresses
  -> if AddressId > 0 find existing address
  -> validate ModelState
  -> create/update UserAddress
  -> enforce default address rule
  -> SaveChanges()
```

## ActiveModal pattern

ใช้กับหน้า profile เพื่อให้ validation fail แล้วเปิด modal เดิม

```text
POST modal form
  -> validation fail
  -> controller returns AccountProfileViewModel with ActiveModal
  -> Profile.cshtml renders data-active-modal
  -> inline script opens matching modal
```

ค่า modal ที่ใช้:

| ค่า | เปิด modal |
| --- | --- |
| `profile-edit` | modal แก้ข้อมูลบัญชี |
| `address-edit` | modal เพิ่ม/แก้ที่อยู่ |

## ข้อควรระวังเวลาแก้ Auth ViewModel

- ถ้าเพิ่ม field สมัครสมาชิก step 1 ต้องเพิ่มใน `RegisterViewModel`, pending registration record, register view และ complete registration logic
- ถ้าเพิ่ม field address ต้องเพิ่มใน `RegisterAddressViewModel`, `AccountAddressEditViewModel`, view register address, profile modal และ save logic
- ถ้าเปลี่ยนชื่อ `EditForm` หรือ `AddressForm` ต้องเปลี่ยน `[Bind(Prefix = "...")]` ด้วย
- ถ้าเพิ่ม validation ใน ViewModel แล้ว POST fail ต้องตรวจว่า view แสดง validation message อยู่หรือไม่
- อย่าใส่ password checking logic ใน ViewModel ให้เก็บไว้ใน controller/helper หรือ service
