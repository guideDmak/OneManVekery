using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;
using OneManVekery.Models.Db;
using OneManVekery.Models;
using OneManVekery.ViewModel;
using System.Globalization;

namespace OneManVekery.Controllers;

public partial class AdminController : Controller
{
    private const int DefaultReorderLevel = 10;
    private const long MaxItemImageUploadBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedItemImageUploadExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private readonly OneManVekeryDBContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AdminController(OneManVekeryDBContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var roleKey = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey);
        if (!AdminPortalAuth.CanAccessAdmin(roleKey))
        {
            TempData["SiteNotice"] = "กรุณาเข้าสู่ระบบด้วยบัญชี Staff, Admin หรือ Owner ก่อนเข้าหน้าหลังบ้าน";
            context.Result = RedirectToAction("Login", "Account");
            return;
        }

        ViewData["AdminSignedInName"] = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountNameKey) ?? "Bakery Team";
        ViewData["AdminSignedInRole"] = HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleLabelKey) ?? "Staff";
        ViewData["AdminSignedInRoleKey"] = roleKey?.Trim().ToLowerInvariant() ?? string.Empty;

        base.OnActionExecuting(context);
    }

}
