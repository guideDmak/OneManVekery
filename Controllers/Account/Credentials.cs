using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OneManVekery.Models;
using OneManVekery.Models.Db;
using OneManVekery.ViewModel;

namespace OneManVekery.Controllers;

public partial class AccountController
{
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
}
