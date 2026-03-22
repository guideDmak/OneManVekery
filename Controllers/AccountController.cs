using Microsoft.AspNetCore.Mvc;
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

        TempData["SiteNotice"] = "เข้าสู่ระบบตัวอย่างเรียบร้อยแล้ว";
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
        TempData["SiteNotice"] = "ออกจากระบบตัวอย่างเรียบร้อยแล้ว";
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
            Notes = "Registered from storefront"
        });

        TempData["SiteNotice"] = "สมัครสมาชิกผู้ใช้งานเรียบร้อยแล้ว บัญชีประเภทพนักงานและแอดมินต้องเพิ่มจากหน้าแอดมินเท่านั้น";
        return RedirectToAction(nameof(Login));
    }
}
