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
    private string GetCurrentAdminRoleKey()
    {
        return HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleKey)?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private int GetCurrentAdminAccountId()
    {
        return int.TryParse(HttpContext.Session.GetString(AdminPortalAuth.SessionAccountIdKey), out var accountId)
            ? accountId
            : 0;
    }

    private static string NormalizeStatusKey(string? status)
    {
        return (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "suspended" => "suspended",
            "closed" => "closed",
            _ => "active"
        };
    }

    private static string FormatStatusLabel(string? status)
    {
        return NormalizeStatusKey(status) switch
        {
            "suspended" => "Suspended",
            "closed" => "Closed",
            _ => "Active"
        };
    }

    private static string FormatRoleLabel(string? roleName, string? roleKey, string? fallbackLabel = null)
    {
        var source = !string.IsNullOrWhiteSpace(roleName)
            ? roleName
            : !string.IsNullOrWhiteSpace(fallbackLabel)
                ? fallbackLabel
                : roleKey;

        return (source ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "owner" => "Owner",
            "admin" => "Admin",
            "manager" => "Manager",
            "support" => "Support",
            "staff" => "Staff",
            "user" => "User",
            _ => string.IsNullOrWhiteSpace(source) ? "Staff" : source!.Trim()
        };
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
               string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }
}
