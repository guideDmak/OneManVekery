# Notes for Report Section 4

ไฟล์นี้ใช้เป็นโน้ตประกอบรายงานหัวข้อ `4.1 การจัดการข้อมูล ส่วนประมวลผลข้อมูล ส่วนควบคุม และส่วนแสดงผล`

วิธีใช้:

- เปิดไฟล์นี้แล้วใช้ `Cmd+F` หาคำค้นของหัวข้อ เช่น `CMD-F: LOGIN-REPORT`
- คัดลอกข้อความรายงานไปใส่ใน Word
- ถ้าต้องใส่รูปโค้ด ให้เปิดไฟล์ที่ระบุไว้ แล้วค้นชื่อ class/action ตาม `คำค้นในไฟล์โค้ด`

---

## CMD-F: LOGIN-REPORT

### 1. หน้า Login (เข้าสู่ระบบ)

หน้า Login เป็นหน้าสำหรับให้ผู้ใช้เข้าสู่ระบบ One Man Vekery โดยผู้ใช้ต้องกรอกอีเมลและรหัสผ่าน จากนั้นระบบจะตรวจสอบข้อมูลกับฐานข้อมูลผู้ใช้ หากข้อมูลถูกต้องและบัญชียังอยู่ในสถานะที่สามารถใช้งานได้ ระบบจะบันทึกข้อมูลผู้ใช้ลงใน Session และเปลี่ยนเส้นทางไปยังหน้าที่เหมาะสมตามบทบาทของผู้ใช้ เช่น ลูกค้าจะไปยังหน้า Home ส่วน Admin, Staff หรือ Owner จะเข้าสู่ระบบหลังบ้าน

### 1.1 ส่วนติดต่อผู้ใช้เชิงข้อมูล (Model และ ViewModel)

ระบบ Login ใช้ข้อมูลจากตาราง `users` และ `roles` ในฐานข้อมูล โดยตาราง `users` ใช้เก็บข้อมูลบัญชีผู้ใช้ เช่น ชื่อผู้ใช้ อีเมล รหัสผ่าน เบอร์โทรศัพท์ สถานะบัญชี และ `role_id` ส่วนตาราง `roles` ใช้เก็บข้อมูลบทบาทของผู้ใช้ เช่น `user`, `staff`, `admin` และ `owner`

ในส่วนของ ViewModel ระบบใช้ `LoginViewModel` สำหรับรับข้อมูลจากฟอร์มเข้าสู่ระบบ โดยข้อมูลหลักที่รับเข้ามาคือ `Email` และ `Password` พร้อมกำหนดการตรวจสอบความถูกต้อง เช่น ต้องกรอกอีเมล ต้องกรอกรหัสผ่าน รูปแบบอีเมลต้องถูกต้อง และรหัสผ่านต้องมีความยาวตามที่ระบบกำหนด

ไฟล์ Model ที่เกี่ยวข้อง:

- `Models/Db/Accounts/User.cs`
- `Models/Db/Accounts/Role.cs`

ไฟล์ ViewModel ที่เกี่ยวข้อง:

- `ViewModel/AuthViewModels.cs`

โค้ดที่ควรใส่ในรายงาน:

```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
```

คำค้นในไฟล์โค้ด:

- `public class LoginViewModel`
- `กรุณากรอกอีเมล`
- `กรุณากรอกรหัสผ่าน`

### 1.2 ส่วนประมวลผล/ส่วนควบคุม (Controller)

ส่วนควบคุมการเข้าสู่ระบบอยู่ใน `AccountController` โดยมี action สำหรับแสดงหน้า Login และ action สำหรับรับข้อมูลจากฟอร์ม Login

`GET Login` ใช้สำหรับแสดงหน้าเข้าสู่ระบบให้ผู้ใช้กรอกอีเมลและรหัสผ่าน หากผู้ใช้ที่เข้าสู่ระบบอยู่แล้วเป็น role ที่สามารถเข้าหลังบ้านได้ ระบบจะเปลี่ยนเส้นทางไปยังหน้า Admin Dashboard แทน

`POST Login` ใช้สำหรับรับข้อมูลจากฟอร์มเข้าสู่ระบบ จากนั้นระบบจะตรวจสอบ `ModelState` ว่าข้อมูลครบถ้วนหรือไม่ หากข้อมูลไม่ครบจะส่งกลับไปยังหน้า Login พร้อมข้อความแจ้งเตือน แต่หากข้อมูลครบถ้วน ระบบจะตรวจสอบอีเมลและรหัสผ่านกับฐานข้อมูล

เมื่อข้อมูลเข้าสู่ระบบถูกต้อง ระบบจะตรวจสอบสถานะบัญชี หากบัญชีไม่ได้อยู่ในสถานะ `Active` ระบบจะไม่อนุญาตให้เข้าสู่ระบบ แต่หากบัญชีใช้งานได้ ระบบจะบันทึกข้อมูลผู้ใช้ลงใน Session ได้แก่ account id, account name, role key และ role label จากนั้นเปลี่ยนเส้นทางผู้ใช้ตาม role ที่ได้รับ

ไฟล์ Controller ที่เกี่ยวข้อง:

- `Controllers/AccountController.cs`
- `Controllers/Account/Credentials.cs`
- `Models/AdminPortalAuth.cs`

โค้ดที่ควรใส่ในรายงาน: `GET Login`

```csharp
[HttpGet]
public IActionResult Login()
{
    if (AdminPortalAuth.CanAccessAdmin(HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey)))
    {
        return RedirectToAction("Index", "Admin");
    }

    return View(new LoginViewModel());
}
```

โค้ดที่ควรใส่ในรายงาน: `POST Login`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var account = Authenticate(model.Email, model.Password);
    if (account is null)
    {
        ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านไม่ถูกต้อง");
        return View(model);
    }

    if (!string.Equals(account.Status, "Active", StringComparison.OrdinalIgnoreCase))
    {
        ModelState.AddModelError(string.Empty, "บัญชีนี้ยังไม่พร้อมใช้งาน");
        return View(model);
    }

    HttpContext.Session.SetString(AdminPortalAuth.SessionAccountIdKey, account.AccountId.ToString());
    HttpContext.Session.SetString(AdminPortalAuth.SessionAccountNameKey, account.FullName);
    HttpContext.Session.SetString(AdminPortalAuth.SessionAccountRoleKey, account.RoleKey);
    HttpContext.Session.SetString(AdminPortalAuth.SessionAccountRoleLabelKey, account.Role);

    if (AdminPortalAuth.CanAccessAdmin(account.RoleKey))
    {
        TempData["SiteNotice"] = $"เข้าสู่ระบบหลังบ้านในสิทธิ์ {account.Role} เรียบร้อยแล้ว";
        return RedirectToAction("Index", "Admin");
    }

    TempData["SiteNotice"] = $"ยินดีต้อนรับกลับ {account.FullName}";
    return RedirectToAction("Index", "Home");
}
```

โค้ดที่ควรใส่ในรายงาน: ตรวจสอบบัญชีผู้ใช้

```csharp
private AccountRecord? Authenticate(string email, string password)
{
    var normalizedEmail = NormalizeEmail(email);
    var normalizedPassword = NormalizePassword(password);
    var legacyPasswordHash = ComputeLegacyPasswordHash(normalizedPassword);

    var user = _dbContext.Users
        .Include(entry => entry.Role)
        .FirstOrDefault(entry => entry.Email == normalizedEmail);

    if (user is null || !PasswordMatches(user.PasswordHash, normalizedPassword, legacyPasswordHash))
    {
        return null;
    }

    user.LastActiveAt = DateTime.UtcNow;
    _dbContext.SaveChanges();

    return MapAccount(user);
}
```

คำค้นในไฟล์โค้ด:

- `[HttpGet]`
- `public IActionResult Login()`
- `[HttpPost]`
- `public IActionResult Login(LoginViewModel model)`
- `var account = Authenticate(model.Email, model.Password);`
- `HttpContext.Session.SetString`
- `private AccountRecord? Authenticate`

### 1.3 ตารางฐานข้อมูลที่เกี่ยวข้อง (Login)

`users` ใช้สำหรับตรวจสอบอีเมล รหัสผ่าน สถานะบัญชี และข้อมูลผู้ใช้ที่เข้าสู่ระบบ

`roles` ใช้สำหรับระบุบทบาทของผู้ใช้ และใช้กำหนดเส้นทางหลังเข้าสู่ระบบ เช่น `user`, `staff`, `admin` หรือ `owner`

ไฟล์ Model ที่ควรใช้ประกอบ:

- `Models/Db/Accounts/User.cs`
- `Models/Db/Accounts/Role.cs`
- `Models/Db/OneManVekeryDBContext.cs`

โค้ดที่ควรใส่ในรายงาน: User Model

```csharp
public partial class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public virtual Role Role { get; set; } = null!;
}
```

โค้ดที่ควรใส่ในรายงาน: Role Model

```csharp
public partial class Role
{
    public int Id { get; set; }
    public string RoleKey { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
```

คำค้นในไฟล์โค้ด:

- `public partial class User`
- `public partial class Role`
- `public virtual DbSet<User> Users`
- `public virtual DbSet<Role> Roles`

### 1.4 ลำดับการทำงานของระบบ

1. ผู้ใช้เปิดหน้า Login
2. ระบบแสดงฟอร์มให้กรอกอีเมลและรหัสผ่าน
3. ผู้ใช้กรอกข้อมูลและกดเข้าสู่ระบบ
4. Controller ตรวจสอบความถูกต้องของข้อมูลที่รับเข้ามา
5. ระบบค้นหาผู้ใช้จากอีเมลในฐานข้อมูล
6. ระบบตรวจสอบรหัสผ่านและสถานะบัญชี
7. หากข้อมูลไม่ถูกต้อง ระบบแจ้งข้อผิดพลาดและให้กรอกใหม่
8. หากข้อมูลถูกต้อง ระบบบันทึกข้อมูลผู้ใช้ลงใน Session
9. ระบบตรวจสอบ role ของผู้ใช้
10. หากเป็น `user` ระบบพาไปหน้า Home
11. หากเป็น `staff`, `admin` หรือ `owner` ระบบพาไปหน้า Admin Dashboard

### 1.5 ส่วนแสดงผล (View)

View ของหน้า Login ทำหน้าที่แสดงฟอร์มเข้าสู่ระบบให้ผู้ใช้กรอกอีเมลและรหัสผ่าน รวมถึงแสดงข้อความแจ้งเตือนเมื่อข้อมูลไม่ถูกต้อง เช่น อีเมลหรือรหัสผ่านผิด หรือบัญชีไม่สามารถใช้งานได้

ไฟล์ View ที่เกี่ยวข้อง:

- `Views/Account/Login.cshtml`

โค้ดที่ควรใส่ในรายงาน: Login View

```cshtml
@model LoginViewModel

<form asp-action="Login" method="post" class="auth-form">
    @Html.AntiForgeryToken()
    <div asp-validation-summary="ModelOnly" class="text-danger small auth-form-span"></div>

    <div class="auth-field-wrap">
        <label asp-for="Email" class="form-label">อีเมล</label>
        <input asp-for="Email" class="form-control bakery-field auth-field" placeholder="example@email.com" />
        <span asp-validation-for="Email" class="text-danger small"></span>
    </div>

    <div class="auth-field-wrap">
        <label asp-for="Password" class="form-label">รหัสผ่าน</label>
        <input asp-for="Password" type="password" class="form-control bakery-field auth-field" placeholder="อย่างน้อย 8 ตัวอักษร" />
        <span asp-validation-for="Password" class="text-danger small"></span>
    </div>

    <button type="submit" class="btn auth-submit w-100">เข้าสู่ระบบ</button>
</form>
```

คำค้นในไฟล์โค้ด:

- `@model LoginViewModel`
- `<form asp-action="Login"`
- `asp-for="Email"`
- `asp-for="Password"`
- `_ValidationScriptsPartial`

Output ของระบบในส่วน Login:

ผู้ใช้สามารถเข้าสู่ระบบได้สำเร็จและถูกพาไปยังหน้าที่เหมาะสมตามสิทธิ์ หรือได้รับข้อความแจ้งเตือนหากข้อมูลเข้าสู่ระบบไม่ถูกต้อง

