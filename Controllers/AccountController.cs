using Microsoft.AspNetCore.Mvc;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AccountController : Controller
{
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["SiteNotice"] = "สมัครสมาชิกตัวอย่างเรียบร้อยแล้ว กรุณาเข้าสู่ระบบ";
        return RedirectToAction(nameof(Login));
    }
}
