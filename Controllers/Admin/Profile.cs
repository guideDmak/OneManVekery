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
    public IActionResult Profile()
    {
        return View(BuildProfileModel());
    }

    private AdminProfileViewModel BuildProfileModel()
    {
        var currentAdminId = GetCurrentAdminAccountId();
        var adminAccount = currentAdminId <= 0
            ? null
            : _dbContext.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .FirstOrDefault(user => user.Id == currentAdminId);
        var customerCount = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Count(user => user.Role.RoleKey == "user");

        return new AdminProfileViewModel
        {
            AccountCode = adminAccount is null ? "-" : $"ACC-{adminAccount.Id:0000}",
            FullName = adminAccount?.FullName
                ?? (HttpContext.Session.GetString(AdminPortalAuth.SessionAccountNameKey) ?? "Bakery Team"),
            Role = FormatRoleLabel(
                adminAccount?.Role?.RoleName,
                adminAccount?.Role?.RoleKey,
                HttpContext.Session.GetString(AdminPortalAuth.SessionAccountRoleLabelKey)),
            Email = adminAccount?.Email ?? "-",
            Phone = string.IsNullOrWhiteSpace(adminAccount?.Phone) ? "-" : adminAccount!.Phone!,
            Status = adminAccount is null ? "Unknown" : FormatStatusLabel(adminAccount.Status),
            LastActiveLabel = adminAccount is null
                ? "-"
                : FormatLastActive(adminAccount.LastActiveAt ?? adminAccount.CreatedAt),
            Notes = string.IsNullOrWhiteSpace(adminAccount?.Notes) ? "ไม่มีหมายเหตุในระบบ" : adminAccount!.Notes!,
            SummaryItems =
            [
                new AdminInfoItemViewModel { Label = "Orders in system", Value = _dbContext.Orders.AsNoTracking().Count().ToString(), Detail = "ทุกออเดอร์ที่อยู่ในฐานข้อมูล", AccentKey = "orange" },
                new AdminInfoItemViewModel { Label = "Products live", Value = _dbContext.Products.AsNoTracking().Count(product => product.IsActive).ToString(), Detail = "สินค้าที่เปิดขายบนหน้าร้าน", AccentKey = "green" },
                new AdminInfoItemViewModel { Label = "Registered users", Value = customerCount.ToString(), Detail = "บัญชีลูกค้าที่สมัครแล้ว", AccentKey = "blue" },
                new AdminInfoItemViewModel { Label = "Contact messages", Value = _dbContext.ContactMessages.AsNoTracking().Count().ToString(), Detail = "ข้อความที่เข้ามาจากหน้าเว็บไซต์", AccentKey = "gold" }
            ]
        };
    }
}
