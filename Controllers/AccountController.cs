using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OneManVekery.Services;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AccountController : Controller
{
    private readonly IAccountDirectoryService _accountDirectoryService;

    public AccountController(IAccountDirectoryService accountDirectoryService)
    {
        _accountDirectoryService = accountDirectoryService;
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

        var account = _accountDirectoryService.Authenticate(model.Email, model.Password);
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
    public IActionResult Register()
    {
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (_accountDirectoryService.EmailExists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _accountDirectoryService.AddAccount(new AccountInput
        {
            FullName = model.FullName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Role = "User",
            Status = "Active",
            Notes = "Registered from storefront",
            Password = model.Password
        });

        TempData["SiteNotice"] = "สมัครสมาชิกผู้ใช้งานเรียบร้อยแล้ว บัญชีประเภทพนักงานและแอดมินต้องเพิ่มจากหน้าแอดมินเท่านั้น";
        return RedirectToAction(nameof(Login));
    }
}
