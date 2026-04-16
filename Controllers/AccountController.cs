using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public partial class AccountController : Controller
{
    private const string PendingRegistrationSessionKey = "account-register-pending";
    private readonly OneManVekeryDBContext _dbContext;

    public AccountController(OneManVekeryDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (AdminPortalAuth.CanAccessAdmin(HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey)))
        {
            return RedirectToAction("Index", "Admin");
        }

        return View(new LoginViewModel());
    }

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

    [HttpGet]
    public IActionResult Register(bool restore = false)
    {
        if (restore)
        {
            var pendingRegistration = ReadPendingRegistration();
            if (pendingRegistration is not null)
            {
                return View(BuildRegisterViewModel(pendingRegistration));
            }
        }

        ClearPendingRegistration();
        return View(new RegisterViewModel());
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(AdminPortalAuth.SessionAccountIdKey);
        HttpContext.Session.Remove(AdminPortalAuth.SessionAccountNameKey);
        HttpContext.Session.Remove(AdminPortalAuth.SessionAccountRoleKey);
        HttpContext.Session.Remove(AdminPortalAuth.SessionAccountRoleLabelKey);

        TempData["SiteNotice"] = "ออกจากระบบเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Profile()
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อน";
            return RedirectToAction(nameof(Login));
        }

        if (AdminPortalAuth.CanAccessAdmin(roleKey))
        {
            return RedirectToAction("Profile", "Admin");
        }

        var accountId = GetCurrentAccountId();
        if (accountId <= 0)
        {
            TempData["SiteNotice"] = "ไม่พบข้อมูลบัญชีผู้ใช้ในระบบ";
            return RedirectToAction(nameof(Login));
        }

        var user = GetProfileUser(accountId);

        if (user is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีผู้ใช้ในระบบ";
            return RedirectToAction(nameof(Login));
        }

        return View(BuildProfileViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile([Bind(Prefix = "EditForm")] AccountProfileEditViewModel form)
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อน";
            return RedirectToAction(nameof(Login));
        }

        if (AdminPortalAuth.CanAccessAdmin(roleKey))
        {
            return RedirectToAction("Profile", "Admin");
        }

        var accountId = GetCurrentAccountId();
        var user = GetProfileUser(accountId);
        if (user is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีผู้ใช้ในระบบ";
            return RedirectToAction(nameof(Login));
        }

        if (!string.IsNullOrWhiteSpace(form.Email) && EmailExists(form.Email, accountId))
        {
            ModelState.AddModelError(ProfileEditField(nameof(form.Email)), "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfileViewModel(user, editForm: form, activeModal: "profile-edit"));
        }

        user.FullName = NormalizeText(form.FullName);
        user.Email = NormalizeEmail(form.Email);
        user.Phone = NormalizeOptionalText(form.PhoneNumber);
        user.LastActiveAt = DateTime.UtcNow;

        _dbContext.SaveChanges();

        HttpContext.Session.SetString(AdminPortalAuth.SessionAccountNameKey, user.FullName);
        TempData["SiteNotice"] = "อัปเดตข้อมูลโปรไฟล์เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveAddress([Bind(Prefix = "AddressForm")] AccountAddressEditViewModel form)
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบก่อน";
            return RedirectToAction(nameof(Login));
        }

        if (AdminPortalAuth.CanAccessAdmin(roleKey))
        {
            return RedirectToAction("Profile", "Admin");
        }

        var accountId = GetCurrentAccountId();
        var user = GetProfileUser(accountId);
        if (user is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีผู้ใช้ในระบบ";
            return RedirectToAction(nameof(Login));
        }

        var targetAddress = form.AddressId <= 0
            ? null
            : user.UserAddresses.FirstOrDefault(address => address.Id == form.AddressId);

        if (form.AddressId > 0 && targetAddress is null)
        {
            ModelState.AddModelError(AddressEditField(nameof(form.AddressId)), "ไม่พบที่อยู่ที่ต้องการแก้ไข");
        }

        if (!ModelState.IsValid)
        {
            return View("Profile", BuildProfileViewModel(user, addressForm: form, activeModal: "address-edit"));
        }

        var existingAddresses = user.UserAddresses.ToList();
        var existingAddressCount = existingAddresses.Count;
        var now = DateTime.UtcNow;
        if (targetAddress is null)
        {
            targetAddress = new UserAddress
            {
                UserId = user.Id,
                CreatedAt = now
            };

            _dbContext.UserAddresses.Add(targetAddress);
        }

        targetAddress.Label = NormalizeOptionalText(form.Label);
        targetAddress.RecipientName = NormalizeText(form.RecipientName);
        targetAddress.Phone = NormalizeText(form.PhoneNumber);
        targetAddress.AddressLine = NormalizeText(form.AddressLine);
        targetAddress.PostalCode = NormalizeOptionalText(form.PostalCode);
        targetAddress.IsDefault = form.IsDefault || existingAddressCount == 0;
        targetAddress.UpdatedAt = now;

        if (targetAddress.IsDefault)
        {
            foreach (var address in existingAddresses.Where(address => address.Id != targetAddress.Id))
            {
                address.IsDefault = false;
            }
        }

        if (!targetAddress.IsDefault && !existingAddresses.Any(address => address.Id != targetAddress.Id && address.IsDefault))
        {
            targetAddress.IsDefault = true;
        }

        _dbContext.SaveChanges();

        TempData["SiteNotice"] = form.AddressId <= 0 ? "เพิ่มที่อยู่เรียบร้อยแล้ว" : "อัปเดตที่อยู่เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (EmailExists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        WritePendingRegistration(new PendingRegistrationRecord
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Password = model.Password
        });

        return RedirectToAction(nameof(RegisterAddress));
    }

    [HttpGet]
    public IActionResult RegisterAddress()
    {
        var pendingRegistration = ReadPendingRegistration();
        if (pendingRegistration is null)
        {
            TempData["SiteNotice"] = "กรอกข้อมูลบัญชีก่อนเพื่อไปยังขั้นตอนที่อยู่";
            return RedirectToAction(nameof(Register));
        }

        return View(BuildRegisterAddressViewModel(pendingRegistration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CompleteRegistration(RegisterAddressViewModel model)
    {
        var pendingRegistration = ReadPendingRegistration();
        if (pendingRegistration is null)
        {
            TempData["SiteNotice"] = "ข้อมูลสมัครสมาชิกหมดอายุแล้ว กรุณาเริ่มใหม่อีกครั้ง";
            return RedirectToAction(nameof(Register));
        }

        if (EmailExists(pendingRegistration.Email))
        {
            ModelState.AddModelError(string.Empty, "อีเมลนี้ถูกใช้งานแล้ว กรุณาเริ่มสมัครใหม่อีกครั้ง");
        }

        if (!ModelState.IsValid)
        {
            return View("RegisterAddress", BuildRegisterAddressViewModel(pendingRegistration, model));
        }

        CompleteStorefrontRegistration(pendingRegistration, model);
        ClearPendingRegistration();

        TempData["SiteNotice"] = "สมัครสมาชิกเรียบร้อยแล้ว ตอนนี้คุณสามารถเข้าสู่ระบบและใช้ที่อยู่เริ่มต้นนี้ในการสั่งซื้อได้";
        return RedirectToAction(nameof(Login));
    }

}
