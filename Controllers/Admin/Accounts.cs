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

public partial class AdminController
{
    [HttpGet]
    public IActionResult Accounts()
    {
        return View(BuildAccountsModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddAccount([Bind(Prefix = "AddForm")] AdminAccountEditorViewModel form)
    {
        var currentRoleKey = GetCurrentAdminRoleKey();

        if (string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError("AddForm.Password", "กรุณากำหนดรหัสผ่านสำหรับบัญชีนี้");
        }

        if (!CanAssignRole(currentRoleKey, form.Role))
        {
            ModelState.AddModelError("AddForm.Role", AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey)
                ? "บัญชีที่สร้างจากหน้านี้เลือกได้เฉพาะ User หรือ Staff"
                : "บัญชี Staff สร้างบัญชีใหม่ได้เฉพาะ User เท่านั้น");
        }

        if (EmailExists(form.Email))
        {
            ModelState.AddModelError("AddForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(addForm: form, activeModal: "account-add"));
        }

        var createdAccount = AddAccount(CreateAccountInput(form));
        TempData["SiteNotice"] = $"เพิ่มบัญชี {createdAccount.FullName} เรียบร้อยแล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateAccount([Bind(Prefix = "EditForm")] AdminAccountEditorViewModel form)
    {
        var currentRoleKey = GetCurrentAdminRoleKey();
        var existingAccount = form.AccountId <= 0 ? null : GetAccount(form.AccountId);

        if (existingAccount is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการแก้ไข";
            return RedirectToAction(nameof(Accounts));
        }

        if (!AdminPortalAuth.CanChangeAccountRoles(currentRoleKey))
        {
            form.Role = existingAccount.Role;
        }
        else if (IsProtectedAdminAccount(existingAccount))
        {
            form.Role = existingAccount.Role;
        }
        else if (!CanKeepOrAssignRole(currentRoleKey, form.Role, existingAccount.Role))
        {
            ModelState.AddModelError("EditForm.Role", "บัญชีนี้เปลี่ยน role ได้เฉพาะ User หรือ Staff");
        }

        if (IsProtectedAdminAccount(existingAccount) && !IsActiveAccountStatus(form.Status))
        {
            ModelState.AddModelError("EditForm.Status", "บัญชี Admin ต้องเป็น Active และไม่สามารถระงับหรือปิดได้");
        }

        if (EmailExists(form.Email, form.AccountId))
        {
            ModelState.AddModelError("EditForm.Email", "อีเมลนี้ถูกใช้งานแล้ว");
        }

        if (!ModelState.IsValid)
        {
            return View("Accounts", BuildAccountsModel(editForm: form, activeModal: "account-edit"));
        }

        if (!UpdateAccount(form.AccountId, CreateAccountInput(form)))
        {
            TempData["SiteNotice"] = "ไม่สามารถอัปเดตบัญชีนี้ได้";
            return RedirectToAction(nameof(Accounts));
        }

        TempData["SiteNotice"] = $"อัปเดตบัญชี {form.FullName} แล้ว";

        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CloseAccount(int accountId)
    {
        var existingAccount = GetAccount(accountId);
        if (accountId <= 0 || existingAccount is null)
        {
            TempData["SiteNotice"] = "ไม่พบบัญชีที่ต้องการปิด";
            return RedirectToAction(nameof(Accounts));
        }

        if (IsProtectedAdminAccount(existingAccount))
        {
            TempData["SiteNotice"] = "บัญชี Admin เป็นบัญชีหลักของระบบ ไม่สามารถปิดได้";
            return RedirectToAction(nameof(Accounts));
        }

        if (!CloseAccountRecord(accountId))
        {
            TempData["SiteNotice"] = "ไม่สามารถปิดบัญชีนี้ได้";
            return RedirectToAction(nameof(Accounts));
        }

        TempData["SiteNotice"] = $"ปิดบัญชี {existingAccount.FullName} แล้ว";
        return RedirectToAction(nameof(Accounts));
    }

    private IReadOnlyList<AccountRecord> GetAllAccounts()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.Status)
            .ThenBy(user => user.FullName)
            .Select(MapAccountRecord)
            .ToList();
    }

    private AccountRecord? GetAccount(int accountId)
    {
        var user = _dbContext.Users
            .AsNoTracking()
            .Include(entry => entry.Role)
            .FirstOrDefault(entry => entry.Id == accountId);

        return user is null ? null : MapAccountRecord(user);
    }

    private static IReadOnlyList<string> GetAccountStatuses()
    {
        return ["Active", "Suspended", "Closed"];
    }

    private bool EmailExists(string email, int? excludingAccountId = null)
    {
        var normalizedEmail = NormalizeAccountEmail(email);

        return _dbContext.Users.Any(user =>
            user.Email == normalizedEmail &&
            user.Id != excludingAccountId);
    }

    private AccountRecord AddAccount(AccountInput input)
    {
        var role = ResolveRole(input.Role);
        var user = new User
        {
            FullName = NormalizeAccountText(input.FullName),
            Email = NormalizeAccountEmail(input.Email),
            PasswordHash = NormalizeAccountPassword(input.Password),
            Phone = NormalizeOptionalAccountText(input.PhoneNumber),
            RoleId = role.Id,
            Status = NormalizeAccountStatusValue(input.Status),
            Notes = NormalizeOptionalAccountText(input.Notes),
            CreatedAt = DateTime.UtcNow,
            LastActiveAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.SaveChanges();
        _dbContext.Entry(user).Reference(entry => entry.Role).Load();

        return MapAccountRecord(user);
    }

    private bool UpdateAccount(int accountId, AccountInput input)
    {
        var user = _dbContext.Users
            .Include(entry => entry.Role)
            .FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
        {
            return false;
        }

        var isProtectedAdmin = IsProtectedAdminRoleKey(user.Role?.RoleKey);
        var normalizedStatus = NormalizeAccountStatusValue(input.Status);
        if (isProtectedAdmin && normalizedStatus != "active")
        {
            return false;
        }

        var role = isProtectedAdmin ? null : ResolveRole(input.Role);

        user.FullName = NormalizeAccountText(input.FullName);
        user.Email = NormalizeAccountEmail(input.Email);
        user.Phone = NormalizeOptionalAccountText(input.PhoneNumber);
        user.Status = isProtectedAdmin ? "active" : normalizedStatus;
        user.Notes = NormalizeOptionalAccountText(input.Notes);

        if (role is not null)
        {
            user.RoleId = role.Id;
        }

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            user.PasswordHash = NormalizeAccountPassword(input.Password);
        }

        _dbContext.SaveChanges();
        return true;
    }

    private bool CloseAccountRecord(int accountId)
    {
        var user = _dbContext.Users
            .Include(entry => entry.Role)
            .FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
        {
            return false;
        }

        if (IsProtectedAdminRoleKey(user.Role?.RoleKey))
        {
            return false;
        }

        user.Status = "closed";
        _dbContext.SaveChanges();
        return true;
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

    private static AccountRecord MapAccountRecord(User user)
    {
        var roleKey = user.Role?.RoleKey?.Trim().ToLowerInvariant() ?? "user";
        var roleLabel = NormalizeAccountRoleLabel(user.Role?.RoleName, roleKey);

        return new AccountRecord
        {
            AccountId = user.Id,
            AccountCode = $"ACC-{user.Id:0000}",
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.Phone ?? string.Empty,
            Role = roleLabel,
            RoleKey = roleKey,
            Status = NormalizeAccountStatusLabel(user.Status),
            LastActiveAt = user.LastActiveAt ?? user.CreatedAt,
            Notes = user.Notes ?? string.Empty
        };
    }

    private static string NormalizeAccountEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeAccountText(string value)
    {
        return value.Trim();
    }

    private static string NormalizeAccountPassword(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalAccountText(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeAccountStatusValue(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "active",
            "suspended" => "suspended",
            "closed" => "closed",
            _ => "active"
        };
    }

    private static bool IsActiveAccountStatus(string value)
    {
        return string.Equals(NormalizeAccountStatusValue(value), "active", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProtectedAdminAccount(AccountRecord account)
    {
        return IsProtectedAdminRoleKey(account.RoleKey);
    }

    private static bool IsProtectedAdminRoleKey(string? roleKey)
    {
        return (roleKey ?? string.Empty).Trim().ToLowerInvariant() is "admin" or "owner";
    }

    private static string NormalizeAccountStatusLabel(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => "Active"
        };
    }

    private static string NormalizeAccountRoleLabel(string? roleName, string? roleKey)
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

    private AdminAccountsViewModel BuildAccountsModel(
        AdminAccountEditorViewModel? addForm = null,
        AdminAccountEditorViewModel? editForm = null,
        string activeModal = "")
    {
        var currentRoleKey = GetCurrentAdminRoleKey();
        var accounts = GetAllAccounts();
        var statusOptions = GetAccountStatuses();
        var addRoleOptions = BuildAddRoleOptions(currentRoleKey);
        var editRoleOptions = addRoleOptions;
        var defaultRole = addRoleOptions.FirstOrDefault() ?? "User";

        return new AdminAccountsViewModel
        {
            DateRangeLabel = $"Account sync {DateTime.Now:dd MMM yyyy}",
            SummaryItems = BuildAccountSummary(accounts),
            Accounts = accounts.Select(MapAccount).ToList(),
            AddRoleOptions = addRoleOptions,
            EditRoleOptions = editRoleOptions,
            StatusOptions = statusOptions,
            CanCreateStaffAccounts = AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey),
            CanChangeRoles = AdminPortalAuth.CanChangeAccountRoles(currentRoleKey),
            AddForm = addForm ?? new AdminAccountEditorViewModel
            {
                Role = defaultRole,
                Status = "Active"
            },
            EditForm = editForm ?? new AdminAccountEditorViewModel
            {
                Role = defaultRole,
                Status = "Active"
            },
            ActiveModal = activeModal
        };
    }

    private static AccountInput CreateAccountInput(AdminAccountEditorViewModel form)
    {
        return new AccountInput
        {
            FullName = form.FullName,
            Email = form.Email,
            PhoneNumber = form.PhoneNumber,
            Role = form.Role,
            Status = form.Status,
            Notes = form.Notes,
            Password = form.Password ?? string.Empty
        };
    }

    private static IReadOnlyList<string> BuildAddRoleOptions(string currentRoleKey)
    {
        return AdminPortalAuth.CanCreateStaffAccounts(currentRoleKey)
            ? ["User", "Staff"]
            : ["User"];
    }

    private static bool CanAssignRole(string currentRoleKey, string requestedRole)
    {
        return BuildAddRoleOptions(currentRoleKey)
            .Contains(requestedRole, StringComparer.OrdinalIgnoreCase);
    }

    private static bool CanKeepOrAssignRole(string currentRoleKey, string requestedRole, string existingRole)
    {
        return string.Equals(requestedRole, existingRole, StringComparison.OrdinalIgnoreCase) ||
               CanAssignRole(currentRoleKey, requestedRole);
    }

    private static IReadOnlyList<AdminInfoItemViewModel> BuildAccountSummary(IReadOnlyList<AccountRecord> accounts)
    {
        var activeCount = accounts.Count(account => account.Status == "Active");
        var suspendedCount = accounts.Count(account => account.Status == "Suspended");
        var closedCount = accounts.Count(account => account.Status == "Closed");

        return
        [
            new AdminInfoItemViewModel { Label = "All Accounts", Value = accounts.Count.ToString(), Detail = "System members" },
            new AdminInfoItemViewModel { Label = "Active", Value = activeCount.ToString(), Detail = "Can sign in", AccentKey = "green" },
            new AdminInfoItemViewModel { Label = "Suspended", Value = suspendedCount.ToString(), Detail = "Waiting review", AccentKey = "gold" },
            new AdminInfoItemViewModel { Label = "Closed", Value = closedCount.ToString(), Detail = "Kept for history", AccentKey = "red" }
        ];
    }

    private static AdminAccountRecordViewModel MapAccount(AccountRecord account)
    {
        return new AdminAccountRecordViewModel
        {
            AccountId = account.AccountId,
            AccountCode = account.AccountCode,
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            Role = account.Role,
            Status = account.Status,
            StatusKey = account.Status.ToLowerInvariant(),
            IsProtectedAdminAccount = IsProtectedAdminAccount(account),
            LastActive = FormatLastActive(account.LastActiveAt),
            LastActiveSort = account.LastActiveAt.Ticks,
            Notes = account.Notes
        };
    }

    private static string FormatLastActive(DateTime lastActiveAt)
    {
        return lastActiveAt.Date == DateTime.Today
            ? $"Today, {lastActiveAt:hh:mm tt}"
            : $"{lastActiveAt:MMM dd, yyyy}";
    }
}
