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
}
