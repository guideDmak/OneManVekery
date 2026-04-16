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
    public IActionResult Customers()
    {
        return RedirectToAction(nameof(Staff));
    }

    [HttpGet]
    public IActionResult Staff()
    {
        if (!AdminPortalAuth.CanManageStaffDirectory(GetCurrentAdminRoleKey()))
        {
            TempData["SiteNotice"] = "หน้านี้เปิดให้เฉพาะ Admin สำหรับดูข้อมูล Staff";
            return RedirectToAction(nameof(Index));
        }

        return View(BuildStaffModel());
    }

    private AdminStaffViewModel BuildStaffModel()
    {
        var staffMembers = _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .Where(user => user.Role.RoleKey == "staff")
            .OrderByDescending(user => user.LastActiveAt ?? user.CreatedAt)
            .ThenBy(user => user.FullName)
            .ToList();

        return new AdminStaffViewModel
        {
            DateRangeLabel = $"Staff sync {DateTime.Now:dd MMM yyyy}",
            Metrics = BuildStaffMetrics(staffMembers),
            StaffMembers = staffMembers.Select(MapStaff).ToList()
        };
    }

    private static IReadOnlyList<AdminMetricCardViewModel> BuildStaffMetrics(IReadOnlyList<User> staffMembers)
    {
        var activeCount = staffMembers.Count(staff => NormalizeStatusKey(staff.Status) == "active");
        var inactiveCount = staffMembers.Count - activeCount;
        var recentlyActiveCount = staffMembers.Count(staff =>
            (staff.LastActiveAt ?? staff.CreatedAt) >= DateTime.UtcNow.AddDays(-7));

        return
        [
            new AdminMetricCardViewModel { Label = "Staff", Value = staffMembers.Count.ToString(), Delta = "บัญชีพนักงานในระบบ", PositiveTrend = staffMembers.Count > 0, AccentKey = "orange" },
            new AdminMetricCardViewModel { Label = "Active", Value = activeCount.ToString(), Delta = $"{inactiveCount} suspended or closed", PositiveTrend = activeCount > 0, AccentKey = "green" },
            new AdminMetricCardViewModel { Label = "Recent", Value = recentlyActiveCount.ToString(), Delta = "ใช้งานภายใน 7 วัน", PositiveTrend = recentlyActiveCount > 0, AccentKey = "gold" },
            new AdminMetricCardViewModel { Label = "Need Review", Value = inactiveCount.ToString(), Delta = "บัญชีที่ไม่ active", PositiveTrend = inactiveCount == 0, AccentKey = "blue" }
        ];
    }

    private static AdminStaffRecordViewModel MapStaff(User staff)
    {
        return new AdminStaffRecordViewModel
        {
            StaffId = staff.Id,
            StaffCode = $"STF-{staff.Id:0000}",
            FullName = staff.FullName,
            Email = staff.Email,
            PhoneNumber = string.IsNullOrWhiteSpace(staff.Phone) ? "-" : staff.Phone,
            RoleLabel = FormatRoleLabel(staff.Role?.RoleName, staff.Role?.RoleKey),
            Status = FormatStatusLabel(staff.Status),
            StatusKey = NormalizeStatusKey(staff.Status),
            LastActiveLabel = FormatLastActive(staff.LastActiveAt ?? staff.CreatedAt),
            CreatedLabel = staff.CreatedAt.ToString("dd MMM yyyy"),
            Notes = string.IsNullOrWhiteSpace(staff.Notes) ? "-" : staff.Notes!
        };
    }
}
