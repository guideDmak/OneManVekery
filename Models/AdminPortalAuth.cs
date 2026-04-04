namespace OneManVekery.Models;

public static class AdminPortalAuth
{
    public const string SessionAccountIdKey = "account.id";
    public const string SessionAccountNameKey = "account.name";
    public const string SessionAccountRoleKey = "account.role";
    public const string SessionAccountRoleLabelKey = "account.role-label";

    public static bool CanAccessAdmin(string? roleKey)
    {
        return !string.IsNullOrWhiteSpace(roleKey) &&
               !string.Equals(roleKey, "user", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanCreateStaffAccounts(string? roleKey)
    {
        return string.Equals(roleKey, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleKey, "owner", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanChangeAccountRoles(string? roleKey)
    {
        return CanCreateStaffAccounts(roleKey);
    }
}
