using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AccountController : Controller
{
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
        if (EmailExists(model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AddAccount(new AccountInput
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

    private bool EmailExists(string email, int? excludingAccountId = null)
    {
        var normalizedEmail = NormalizeEmail(email);

        return _dbContext.Users.Any(user =>
            user.Email == normalizedEmail &&
            user.Id != excludingAccountId);
    }

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

        if (!string.Equals(user.PasswordHash, normalizedPassword, StringComparison.Ordinal))
        {
            user.PasswordHash = normalizedPassword;
        }

        user.LastActiveAt = DateTime.UtcNow;
        _dbContext.SaveChanges();

        return MapAccount(user);
    }

    private AccountRecord AddAccount(AccountInput input)
    {
        var role = ResolveRole(input.Role);
        var user = new User
        {
            FullName = NormalizeText(input.FullName),
            Email = NormalizeEmail(input.Email),
            PasswordHash = NormalizePassword(input.Password),
            Phone = NormalizeOptionalText(input.PhoneNumber),
            RoleId = role.Id,
            Status = NormalizeStatusValue(input.Status),
            Notes = NormalizeOptionalText(input.Notes),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _dbContext.Entry(user).Reference(entry => entry.Role).Load();

        return MapAccount(user);
    }

    private Role ResolveRole(string roleValue)
    {
        var normalized = roleValue.Trim();
        var role = _dbContext.Roles
            .AsNoTracking()
            .ToList()
            .FirstOrDefault(entry =>
                string.Equals(entry.RoleName, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.RoleKey, normalized, StringComparison.OrdinalIgnoreCase));

        if (role is null)
        {
            throw new InvalidOperationException($"Role '{roleValue}' was not found in the database.");
        }

        return role;
    }

    private static AccountRecord MapAccount(User user)
    {
        var roleKey = user.Role?.RoleKey?.Trim().ToLowerInvariant() ?? "user";
        var roleLabel = NormalizeRoleLabel(user.Role?.RoleName, roleKey);

        return new AccountRecord
        {
            AccountId = user.Id,
            AccountCode = $"ACC-{user.Id:0000}",
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.Phone ?? string.Empty,
            Role = roleLabel,
            RoleKey = roleKey,
            Status = NormalizeStatusLabel(user.Status),
            LastActiveAt = user.LastActiveAt ?? user.CreatedAt,
            Notes = user.Notes ?? string.Empty
        };
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }

    private static string NormalizePassword(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalText(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeStatusValue(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "suspended" => "suspended",
            "closed" => "closed",
            _ => "active"
        };
    }

    private static string NormalizeStatusLabel(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => "Active"
        };
    }

    private static string NormalizeRoleLabel(string? roleName, string? roleKey)
    {
        var source = string.IsNullOrWhiteSpace(roleName) ? roleKey : roleName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "User";
        }

        return source.Trim().ToLowerInvariant() switch
        {
            "owner" => "Owner",
            "admin" => "Admin",
            "manager" => "Manager",
            "staff" => "Staff",
            "support" => "Support",
            _ => "User"
        };
    }

    private static bool PasswordMatches(string storedPassword, string normalizedPassword, string legacyPasswordHash)
    {
        return string.Equals(storedPassword, normalizedPassword, StringComparison.Ordinal) ||
               string.Equals(storedPassword, legacyPasswordHash, StringComparison.Ordinal);
    }

    private static string ComputeLegacyPasswordHash(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
