namespace OneManVekery.Models;

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
