namespace OneManVekery.Services;

public interface IAccountDirectoryService
{
    IReadOnlyList<AccountRecord> GetAllAccounts();

    AccountRecord? GetAccount(Guid accountId);

    IReadOnlyList<string> GetRoles();

    IReadOnlyList<string> GetStatuses();

    bool EmailExists(string email, Guid? excludingAccountId = null);

    AccountRecord AddAccount(AccountInput input);

    bool UpdateAccount(Guid accountId, AccountInput input);

    bool CloseAccount(Guid accountId);
}

public sealed class AccountInput
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}

public sealed record AccountRecord
{
    public Guid AccountId { get; init; }

    public string AccountCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime LastActiveAt { get; init; }

    public string Notes { get; init; } = string.Empty;
}

public sealed class InMemoryAccountDirectoryService : IAccountDirectoryService
{
    private static readonly string[] SupportedRoles = ["Owner", "Admin", "Manager", "Staff", "Support", "User"];
    private static readonly string[] SupportedStatuses = ["Active", "Suspended", "Closed"];

    private readonly object _sync = new();
    private readonly List<AccountRecord> _accounts;

    public InMemoryAccountDirectoryService()
    {
        _accounts = SeedAccounts();
    }

    public IReadOnlyList<AccountRecord> GetAllAccounts()
    {
        lock (_sync)
        {
            return _accounts
                .OrderBy(account => account.Status, StringComparer.OrdinalIgnoreCase)
                .ThenBy(account => account.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(Clone)
                .ToList();
        }
    }

    public AccountRecord? GetAccount(Guid accountId)
    {
        lock (_sync)
        {
            var account = _accounts.FirstOrDefault(entry => entry.AccountId == accountId);
            return account is null ? null : Clone(account);
        }
    }

    public IReadOnlyList<string> GetRoles()
    {
        return SupportedRoles;
    }

    public IReadOnlyList<string> GetStatuses()
    {
        return SupportedStatuses;
    }

    public bool EmailExists(string email, Guid? excludingAccountId = null)
    {
        var normalizedEmail = NormalizeEmail(email);

        lock (_sync)
        {
            return _accounts.Any(account =>
                string.Equals(account.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
                account.AccountId != excludingAccountId);
        }
    }

    public AccountRecord AddAccount(AccountInput input)
    {
        lock (_sync)
        {
            var account = new AccountRecord
            {
                AccountId = Guid.NewGuid(),
                AccountCode = GenerateAccountCode(),
                FullName = NormalizeText(input.FullName),
                Email = NormalizeEmail(input.Email),
                PhoneNumber = NormalizeText(input.PhoneNumber),
                Role = NormalizeRole(input.Role),
                Status = NormalizeStatus(input.Status),
                LastActiveAt = DateTime.Now,
                Notes = NormalizeText(input.Notes)
            };

            _accounts.Add(account);
            return Clone(account);
        }
    }

    public bool UpdateAccount(Guid accountId, AccountInput input)
    {
        lock (_sync)
        {
            var index = _accounts.FindIndex(account => account.AccountId == accountId);
            if (index < 0)
            {
                return false;
            }

            _accounts[index] = _accounts[index] with
            {
                FullName = NormalizeText(input.FullName),
                Email = NormalizeEmail(input.Email),
                PhoneNumber = NormalizeText(input.PhoneNumber),
                Role = NormalizeRole(input.Role),
                Status = NormalizeStatus(input.Status),
                Notes = NormalizeText(input.Notes)
            };

            return true;
        }
    }

    public bool CloseAccount(Guid accountId)
    {
        lock (_sync)
        {
            var index = _accounts.FindIndex(account => account.AccountId == accountId);
            if (index < 0)
            {
                return false;
            }

            _accounts[index] = _accounts[index] with
            {
                Status = "Closed",
                Notes = _accounts[index].Status == "Closed"
                    ? _accounts[index].Notes
                    : $"{_accounts[index].Notes.TrimEnd('.')} Closed by admin.".Trim()
            };

            return true;
        }
    }

    private static AccountRecord Clone(AccountRecord account)
    {
        return account with { };
    }

    private static string NormalizeText(string value)
    {
        return value.Trim();
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeRole(string value)
    {
        return SupportedRoles.FirstOrDefault(role => string.Equals(role, value.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? "User";
    }

    private static string NormalizeStatus(string value)
    {
        return SupportedStatuses.FirstOrDefault(status => string.Equals(status, value.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? "Active";
    }

    private string GenerateAccountCode()
    {
        var nextNumber = _accounts
            .Select(account => account.AccountCode)
            .Select(code => code.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
            .Select(value => int.TryParse(value, out var number) ? number : 1999)
            .DefaultIfEmpty(2000)
            .Max() + 1;

        return $"ACC-{nextNumber:0000}";
    }

    private static List<AccountRecord> SeedAccounts()
    {
        return
        [
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50001"),
                AccountCode = "ACC-2001",
                FullName = "Mad Bakery",
                Email = "owner@madbakery.com",
                PhoneNumber = "+66 98 765 4321",
                Role = "Owner",
                Status = "Active",
                LastActiveAt = DateTime.Today.AddHours(9).AddMinutes(15),
                Notes = "Full access"
            },
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50002"),
                AccountCode = "ACC-2002",
                FullName = "Nicha Saelim",
                Email = "nicha@madbakery.com",
                PhoneNumber = "+66 81 443 2288",
                Role = "Admin",
                Status = "Active",
                LastActiveAt = DateTime.Today.AddHours(8).AddMinutes(42),
                Notes = "Order approvals"
            },
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50003"),
                AccountCode = "ACC-2003",
                FullName = "Pimchanok Dee",
                Email = "pim@madbakery.com",
                PhoneNumber = "+66 89 111 4402",
                Role = "Manager",
                Status = "Active",
                LastActiveAt = DateTime.Today.AddHours(8).AddMinutes(10),
                Notes = "Stock and reports"
            },
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50004"),
                AccountCode = "ACC-2004",
                FullName = "Krittin Boon",
                Email = "krittin@madbakery.com",
                PhoneNumber = "+66 95 274 8821",
                Role = "Staff",
                Status = "Suspended",
                LastActiveAt = new DateTime(2026, 3, 17, 16, 05, 0),
                Notes = "Waiting schedule review"
            },
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50005"),
                AccountCode = "ACC-2005",
                FullName = "Arisa Moon",
                Email = "arisa@madbakery.com",
                PhoneNumber = "+66 84 220 1155",
                Role = "Support",
                Status = "Closed",
                LastActiveAt = new DateTime(2026, 3, 9, 11, 40, 0),
                Notes = "Closed account, keep history"
            },
            new AccountRecord
            {
                AccountId = Guid.Parse("B8ED33D3-4D6B-40F0-A841-81C70DA50006"),
                AccountCode = "ACC-2006",
                FullName = "Thanawat Korn",
                Email = "thanawat@example.com",
                PhoneNumber = "+66 93 606 9014",
                Role = "User",
                Status = "Active",
                LastActiveAt = new DateTime(2026, 3, 18, 14, 20, 0),
                Notes = "Registered from storefront"
            }
        ];
    }
}
