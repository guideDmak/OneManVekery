using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public class AccountController : Controller
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

    private User? GetProfileUser(int accountId)
    {
        return accountId <= 0
            ? null
            : _dbContext.Users
                .Include(entry => entry.Role)
                .Include(entry => entry.UserAddresses)
                .FirstOrDefault(entry => entry.Id == accountId);
    }

    private AccountProfileViewModel BuildProfileViewModel(
        User user,
        AccountProfileEditViewModel? editForm = null,
        AccountAddressEditViewModel? addressForm = null,
        string activeModal = "")
    {
        var loyaltyWallet = _dbContext.LoyaltyWallets
            .AsNoTracking()
            .FirstOrDefault(entry => entry.UserId == user.Id);

        return new AccountProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.Phone ?? "-",
            RoleLabel = NormalizeRoleLabel(user.Role?.RoleName, user.Role?.RoleKey),
            StatusLabel = NormalizeStatusLabel(user.Status),
            AccountCode = $"ACC-{user.Id:0000}",
            LastActiveAt = user.LastActiveAt ?? user.CreatedAt,
            CurrentPoints = loyaltyWallet?.CurrentPoints ?? 0,
            LifetimeEarnedPoints = loyaltyWallet?.LifetimeEarned ?? 0,
            LifetimeRedeemedPoints = loyaltyWallet?.LifetimeRedeemed ?? 0,
            Addresses = user.UserAddresses
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.Id)
                .Select(address => new AccountAddressCardViewModel
                {
                    AddressId = address.Id,
                    Label = string.IsNullOrWhiteSpace(address.Label) ? "ที่อยู่จัดส่ง" : address.Label!,
                    RecipientName = address.RecipientName,
                    PhoneNumber = address.Phone,
                    AddressLine = address.AddressLine,
                    PostalCode = address.PostalCode ?? string.Empty,
                    IsDefault = address.IsDefault
                })
                .ToList(),
            EditForm = editForm ?? new AccountProfileEditViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone ?? string.Empty
            },
            AddressForm = addressForm ?? new AccountAddressEditViewModel
            {
                Label = "บ้าน",
                RecipientName = user.FullName,
                PhoneNumber = user.Phone ?? string.Empty,
                IsDefault = user.UserAddresses.Count == 0
            },
            ActiveModal = activeModal
        };
    }

    private static string ProfileEditField(string propertyName)
    {
        return $"{nameof(AccountProfileViewModel.EditForm)}.{propertyName}";
    }

    private static string AddressEditField(string propertyName)
    {
        return $"{nameof(AccountProfileViewModel.AddressForm)}.{propertyName}";
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

    private User AddAccount(AccountInput input)
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

        return user;
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

    private int GetCurrentAccountId()
    {
        return int.TryParse(HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey), out var accountId)
            ? accountId
            : 0;
    }

    private static RegisterViewModel BuildRegisterViewModel(PendingRegistrationRecord pendingRegistration)
    {
        return new RegisterViewModel
        {
            FullName = pendingRegistration.FullName,
            Email = pendingRegistration.Email,
            PhoneNumber = pendingRegistration.PhoneNumber,
            Password = pendingRegistration.Password,
            ConfirmPassword = pendingRegistration.Password
        };
    }

    private RegisterAddressViewModel BuildRegisterAddressViewModel(
        PendingRegistrationRecord pendingRegistration,
        RegisterAddressViewModel? input = null)
    {
        return new RegisterAddressViewModel
        {
            AccountFullName = pendingRegistration.FullName,
            AccountEmail = pendingRegistration.Email,
            AccountPhoneNumber = pendingRegistration.PhoneNumber,
            Label = string.IsNullOrWhiteSpace(input?.Label) ? "บ้าน" : input!.Label,
            RecipientName = string.IsNullOrWhiteSpace(input?.RecipientName) ? pendingRegistration.FullName : input!.RecipientName,
            PhoneNumber = string.IsNullOrWhiteSpace(input?.PhoneNumber) ? pendingRegistration.PhoneNumber : input!.PhoneNumber,
            AddressLine = input?.AddressLine ?? string.Empty,
            PostalCode = input?.PostalCode ?? string.Empty
        };
    }

    private void CompleteStorefrontRegistration(PendingRegistrationRecord pendingRegistration, RegisterAddressViewModel addressInput)
    {
        using var transaction = _dbContext.Database.BeginTransaction();

        var user = AddAccount(new AccountInput
        {
            FullName = pendingRegistration.FullName,
            Email = pendingRegistration.Email,
            PhoneNumber = pendingRegistration.PhoneNumber,
            Role = "User",
            Status = "Active",
            Notes = "Registered from storefront",
            Password = pendingRegistration.Password
        });

        _dbContext.UserAddresses.Add(new UserAddress
        {
            UserId = user.Id,
            Label = NormalizeOptionalText(addressInput.Label),
            RecipientName = NormalizeText(addressInput.RecipientName),
            Phone = NormalizeText(addressInput.PhoneNumber),
            AddressLine = NormalizeText(addressInput.AddressLine),
            PostalCode = NormalizeOptionalText(addressInput.PostalCode),
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _dbContext.SaveChanges();
        transaction.Commit();
    }

    private PendingRegistrationRecord? ReadPendingRegistration()
    {
        var raw = HttpContext.Session.GetString(PendingRegistrationSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PendingRegistrationRecord>(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void WritePendingRegistration(PendingRegistrationRecord pendingRegistration)
    {
        HttpContext.Session.SetString(
            PendingRegistrationSessionKey,
            JsonSerializer.Serialize(pendingRegistration));
    }

    private void ClearPendingRegistration()
    {
        HttpContext.Session.Remove(PendingRegistrationSessionKey);
    }

    private sealed class PendingRegistrationRecord
    {
        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
