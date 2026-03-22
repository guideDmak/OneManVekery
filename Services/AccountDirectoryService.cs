using Microsoft.EntityFrameworkCore;
using OneManVekery.Models.Db;

namespace OneManVekery.Services;

public interface IAccountDirectoryService
{
    IReadOnlyList<AccountRecord> GetAllAccounts();

    AccountRecord? GetAccount(int accountId);

    IReadOnlyList<string> GetRoles();

    IReadOnlyList<string> GetStatuses();

    bool EmailExists(string email, int? excludingAccountId = null);

    AccountRecord? Authenticate(string email, string password);

    AccountRecord AddAccount(AccountInput input);

    bool UpdateAccount(int accountId, AccountInput input);

    bool CloseAccount(int accountId);
}

public sealed class AccountInput
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed record AccountRecord
{
    public int AccountId { get; init; }

    public string AccountCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string RoleKey { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime LastActiveAt { get; init; }

    public string Notes { get; init; } = string.Empty;
}

public sealed class DbAccountDirectoryService : IAccountDirectoryService
{
    private static readonly string[] SupportedStatuses = ["Active", "Suspended", "Closed"];
    private readonly OneManVekeryDBContext _dbContext;

    public DbAccountDirectoryService(OneManVekeryDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<AccountRecord> GetAllAccounts()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .OrderBy(user => user.Status)
            .ThenBy(user => user.FullName)
            .Select(MapAccount)
            .ToList();
    }

    public AccountRecord? GetAccount(int accountId)
    {
        var user = _dbContext.Users
            .AsNoTracking()
            .Include(entry => entry.Role)
            .FirstOrDefault(entry => entry.Id == accountId);

        return user is null ? null : MapAccount(user);
    }

    public IReadOnlyList<string> GetRoles()
    {
        return _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Id)
            .Select(role => NormalizeRoleLabel(role.RoleName, role.RoleKey))
            .ToList();
    }

    public IReadOnlyList<string> GetStatuses()
    {
        return SupportedStatuses;
    }

    public bool EmailExists(string email, int? excludingAccountId = null)
    {
        var normalizedEmail = NormalizeEmail(email);

        return _dbContext.Users.Any(user =>
            user.Email == normalizedEmail &&
            user.Id != excludingAccountId);
    }

    public AccountRecord? Authenticate(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedPassword = NormalizePassword(password);
        var legacyPasswordHash = ComputeLegacyPasswordHash(normalizedPassword);

        var user = _dbContext.Users
            .Include(entry => entry.Role)
            .FirstOrDefault(entry =>
                entry.Email == normalizedEmail);

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

    public AccountRecord AddAccount(AccountInput input)
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

    public bool UpdateAccount(int accountId, AccountInput input)
    {
        var user = _dbContext.Users.FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
        {
            return false;
        }

        var role = ResolveRole(input.Role);

        user.FullName = NormalizeText(input.FullName);
        user.Email = NormalizeEmail(input.Email);
        user.Phone = NormalizeOptionalText(input.PhoneNumber);
        user.RoleId = role.Id;
        user.Status = NormalizeStatusValue(input.Status);
        user.Notes = NormalizeOptionalText(input.Notes);

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            user.PasswordHash = NormalizePassword(input.Password);
        }

        _dbContext.SaveChanges();
        return true;
    }

    public bool CloseAccount(int accountId)
    {
        var user = _dbContext.Users.FirstOrDefault(entry => entry.Id == accountId);
        if (user is null)
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
