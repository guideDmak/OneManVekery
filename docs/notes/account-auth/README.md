# Account and Auth Notes

เอกสารนี้อธิบายหน้า login, register, register address, profile และ address management ของ user ฝั่งหน้าร้าน

## ไฟล์ที่เกี่ยวข้อง

| ไฟล์ | หน้าที่ |
| --- | --- |
| `Controllers/AccountController.cs` | constructor และ public actions ของ login/register/profile/address |
| `Controllers/Account/Credentials.cs` | helper login, password check, account lookup |
| `Controllers/Account/ProfileModels.cs` | helper สร้าง profile view model และ address cards |
| `Controllers/Account/RegistrationFlow.cs` | helper pending registration, session restore, complete registration transaction |
| `ViewModel/AuthViewModels.cs` | view model และ validation attribute สำหรับ auth/profile |
| `Views/Account/Login.cshtml` | หน้าเข้าสู่ระบบ |
| `Views/Account/Register.cshtml` | register step 1 |
| `Views/Account/RegisterAddress.cshtml` | register step 2 |
| `Views/Account/Profile.cshtml` | profile, points, addresses, edit modals |
| `Views/Shared/_Layout.cshtml` | navbar, login/logout/profile links |

## ภาพรวม flow

```text
Login
  -> AccountController.Login()
  -> Authenticate()
  -> write session keys
  -> if admin role: Admin/Index
  -> else: Home/Index

Register
  -> POST Register()
  -> write pending registration into session
  -> GET RegisterAddress()
  -> POST CompleteRegistration()
  -> create user + default address inside transaction
  -> clear pending registration
  -> Login

Profile
  -> GET Profile()
  -> guard signed-in user
  -> redirect admin roles to Admin/Profile
  -> BuildProfileViewModel()
  -> Profile.cshtml
```

## Session keys

| Key | ใช้เก็บ |
| --- | --- |
| `AdminPortalAuth.SessionAccountIdKey` | account id ที่ login อยู่ |
| `AdminPortalAuth.SessionAccountNameKey` | ชื่อที่แสดงบน navbar |
| `AdminPortalAuth.SessionAccountRoleKey` | role key เช่น `user`, `staff`, `admin`, `owner` |
| `AdminPortalAuth.SessionAccountRoleLabelKey` | role label สำหรับแสดงผล |
| `account-register-pending` | ข้อมูล register step 1 ก่อนกรอกที่อยู่ |

## Login GET

ตำแหน่ง: `Controllers/AccountController.cs:22-31`

| บรรทัด | อธิบาย |
| --- | --- |
| 21 | รับ GET |
| 24 | ถ้า session role ปัจจุบันเข้า admin ได้ |
| 26 | redirect ไป Admin dashboard |
| 29 | ถ้ายังไม่ login หรือเป็น user ปกติ แสดง Login view |

## Login POST

ตำแหน่ง: `Controllers/AccountController.cs:34-68`

ลำดับ:

```text
POST /Account/Login
  -> ModelState valid?
  -> Authenticate(email, password)
  -> account exists?
  -> account active?
  -> write session
  -> admin role? redirect Admin/Index
  -> user role? redirect Home/Index
```

อธิบายบรรทัดสำคัญ:

| บรรทัด | อธิบาย |
| --- | --- |
| 36-39 | ถ้า validation fail ให้กลับ view พร้อม model |
| 41 | ตรวจ email/password |
| 42-46 | ถ้า login fail เพิ่ม error กลาง form |
| 48-52 | ถ้าบัญชีไม่ Active ห้ามเข้า |
| 54-57 | เขียน session id, name, role key, role label |
| 59-63 | role staff/admin/owner ไปหลังบ้าน |
| 65-66 | role user กลับหน้าแรก |

## `Authenticate()`

ตำแหน่ง: `Controllers/Account/Credentials.cs:22`

หน้าที่:

- normalize email/password
- compute legacy SHA256 hash เพื่อรองรับ password เก่าที่เคย hash ไว้
- query user พร้อม role
- เช็ก password ด้วย `PasswordMatches`
- ถ้า password ยังเป็น legacy hash จะ migrate เป็น plain normalized password ตาม logic ปัจจุบัน
- update `LastActiveAt`
- return `AccountRecord`

หมายเหตุเชิงคุณภาพ:

ระบบ password ตอนนี้ไม่ได้ใช้ password hasher แบบ production-grade เช่น ASP.NET Identity PasswordHasher. ถ้าโปรเจกต์จะใช้จริง ควรยกระดับส่วนนี้เป็น password hashing ที่มี salt และ work factor

## Register step 1

### GET Register

ตำแหน่ง: `Controllers/AccountController.cs:70-84`

- ถ้า `restore=true` จะอ่าน pending registration จาก session
- ถ้ามี pending data จะ rebuild form กลับมา
- ถ้าไม่ restore จะ clear pending registration และเริ่ม form ใหม่

### POST Register

ตำแหน่ง: `Controllers/AccountController.cs:257-278`

ลำดับ:

1. เช็ก email ซ้ำ
2. เช็ก `ModelState`
3. เขียนข้อมูลลง session เป็น `PendingRegistrationRecord`
4. redirect ไป `RegisterAddress`

เหตุผลที่ยังไม่ create user ทันที:

- ระบบบังคับให้สมัครเสร็จพร้อมที่อยู่เริ่มต้น
- ถ้า user กรอก account แต่ยังไม่กรอก address จะยังไม่มี user record ค้างใน database

## Register step 2: address

### GET RegisterAddress

ตำแหน่ง: `Controllers/AccountController.cs:281-291`

- อ่าน pending registration จาก session
- ถ้าไม่มี แสดง notice และกลับ Register
- ถ้ามี สร้าง `RegisterAddressViewModel`

### POST CompleteRegistration

ตำแหน่ง: `Controllers/AccountController.cs:295-320`

ลำดับ:

```text
ReadPendingRegistration()
  -> if null redirect Register
  -> check email exists again
  -> validate address model
  -> CompleteStorefrontRegistration()
  -> ClearPendingRegistration()
  -> redirect Login
```

### `CompleteStorefrontRegistration()`

ตำแหน่ง: `Controllers/Account/RegistrationFlow.cs:42`

ทำงานใน transaction:

1. สร้าง user ด้วย role `User`
2. สร้าง `UserAddress` default
3. `SaveChanges`
4. commit transaction

## Profile GET

ตำแหน่ง: `Controllers/AccountController.cs:98-129`

ลำดับ:

1. อ่าน role จาก session
2. ถ้าไม่มี role คือยังไม่ login
3. ถ้าเป็น admin/staff/owner redirect ไป `Admin/Profile`
4. อ่าน current account id
5. query user พร้อม role และ addresses
6. return `BuildProfileViewModel(user)`

## UpdateProfile POST

ตำแหน่ง: `Controllers/AccountController.cs:132-175`

ลำดับ:

```text
guard login
  -> guard admin role redirect
  -> load current user
  -> email duplicated?
  -> if invalid: return Profile with activeModal = "profile-edit"
  -> update FullName/Email/Phone/LastActiveAt
  -> SaveChanges()
  -> update session display name
  -> redirect Profile
```

จุดสำคัญ:

- ใช้ `[Bind(Prefix = "EditForm")]` เพราะ form ใน view อยู่ใต้ `AccountProfileViewModel.EditForm`
- ถ้า validation fail ต้องส่ง `activeModal` เพื่อเปิด modal เดิมหลัง reload

## SaveAddress POST

ตำแหน่ง: `Controllers/AccountController.cs:178-254`

ลำดับ:

1. guard login
2. admin roles redirect ไป Admin/Profile
3. load current user พร้อม addresses
4. ถ้า `AddressId > 0` หา address เดิม
5. ถ้าไม่เจอ address ที่จะแก้ เพิ่ม ModelState error
6. ถ้า invalid return Profile พร้อม `activeModal = "address-edit"`
7. ถ้า address ใหม่ สร้าง `UserAddress`
8. normalize field
9. set default address rule
10. save changes

กติกา default address:

- ถ้า user ติ๊ก `IsDefault` จะ unset default ของ address อื่น
- ถ้า address ใหม่เป็น address แรก จะ default อัตโนมัติ
- ถ้า user พยายามทำให้ไม่มี default เลย ระบบตั้ง address ปัจจุบันเป็น default

## Auth view models

ไฟล์: `ViewModel/AuthViewModels.cs`

| Class | บรรทัด | ใช้กับหน้า | หน้าที่ |
| --- | --- | --- | --- |
| `LoginViewModel` | 5-15 | Login | email/password validation |
| `RegisterViewModel` | 17-39 | Register step 1 | ชื่อ, email, phone, password, confirm password |
| `RegisterAddressViewModel` | 41-62 | Register step 2 | ที่อยู่เริ่มต้น |
| `AccountProfileViewModel` | 64-93 | Profile | ข้อมูลหน้า profile รวม form ย่อย |
| `AccountProfileEditViewModel` | 95-110 | Edit profile modal | ข้อมูลแก้ profile |
| `AccountAddressCardViewModel` | 112-127 | Address card | ข้อมูลที่อยู่แต่ละใบ |
| `AccountAddressEditViewModel` | 129-152 | Address modal | ข้อมูลเพิ่ม/แก้ที่อยู่ |

## Profile view

ไฟล์: `Views/Account/Profile.cshtml`

โครงสร้าง:

```text
page-banner
account-profile-section
  -> account-profile-grid
     -> account-profile-card
     -> account-points-card
  -> account-addresses-head
  -> account-address-grid
editProfileModal
editAddressModal
Scripts
```

จุดสำคัญ:

| ช่วงบรรทัด | อธิบาย |
| --- | --- |
| 8-21 | banner และ breadcrumb |
| 23 | root มี `data-account-profile` และ `data-active-modal` |
| 26-63 | card ข้อมูลบัญชี |
| 65-84 | card points และ links ไป MyOrders/Shop |
| 87-106 | header ที่อยู่ + ปุ่มเพิ่มที่อยู่ |
| 108-155 | grid ที่อยู่ ถ้าไม่มีแสดง empty card |
| 159-199 | modal แก้ profile |
| 201-262 | modal เพิ่ม/แก้ address |
| 264-312 | inline script เปิด modal หลัง validation fail และเติมข้อมูล address |

## Profile JS ใน view

ตำแหน่ง: `Views/Account/Profile.cshtml:264-312`

หน้าที่:

- อ่าน `data-active-modal`
- ถ้าเป็น `profile-edit` เปิด profile modal ทันที
- ถ้าเป็น `address-edit` เปิด address modal ทันที
- ตอน modal address กำลังเปิด อ่าน `data-address-*` จากปุ่มที่กด
- เอาค่าไปใส่ field ใน modal

## Login/Register views

| View | หน้าที่ |
| --- | --- |
| `Views/Account/Login.cshtml` | form email/password ส่ง POST `Login` |
| `Views/Account/Register.cshtml` | form step 1 ส่ง POST `Register` |
| `Views/Account/RegisterAddress.cshtml` | form step 2 ส่ง POST `CompleteRegistration` |

หน้ากลุ่มนี้มักตั้ง `ViewData["HideChrome"] = true` หรือใช้ auth layout state ผ่าน shared layout เพื่อซ่อน navbar/footer และแสดงหน้า auth แบบเต็มจอ

## ข้อควรระวังเวลาแก้ Account

- อย่าลบ anti-forgery token ใน POST forms
- ถ้าเพิ่ม field ใน profile ต้องเพิ่มทั้ง view model, view, controller update, และ default fill logic
- ถ้าเปลี่ยน session key ต้องแก้ทั้ง layout, HomeController, AdminController และ AccountController partial files
- ถ้าแก้ role logic ต้องตรวจ `AdminPortalAuth` เพราะกำหนดสิทธิ์ admin/staff/owner อยู่ตรงนั้น
- ถ้า validation fail ใน modal ต้องส่ง `ActiveModal` กลับไป ไม่งั้น user จะไม่เห็น error ทันที
