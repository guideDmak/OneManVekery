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
    private static RegisterViewModel BuildRegisterViewModel(PendingRegistrationRecord pendingRegistration)
    {
        return new RegisterViewModel
        {
            FullName = pendingRegistration.FullName,
            Email = pendingRegistration.Email,
            PhoneNumber = pendingRegistration.PhoneNumber,
            Password = pendingRegistration.Password,
            ConfirmPassword = pendingRegistration.Password
        };
    }

    private RegisterAddressViewModel BuildRegisterAddressViewModel(
        PendingRegistrationRecord pendingRegistration,
        RegisterAddressViewModel? input = null)
    {
        return new RegisterAddressViewModel
        {
            AccountFullName = pendingRegistration.FullName,
            AccountEmail = pendingRegistration.Email,
            AccountPhoneNumber = pendingRegistration.PhoneNumber,
            Label = string.IsNullOrWhiteSpace(input?.Label) ? "บ้าน" : input!.Label,
            RecipientName = string.IsNullOrWhiteSpace(input?.RecipientName) ? pendingRegistration.FullName : input!.RecipientName,
            PhoneNumber = string.IsNullOrWhiteSpace(input?.PhoneNumber) ? pendingRegistration.PhoneNumber : input!.PhoneNumber,
            AddressLine = input?.AddressLine ?? string.Empty,
            PostalCode = input?.PostalCode ?? string.Empty
        };
    }

    private void CompleteStorefrontRegistration(PendingRegistrationRecord pendingRegistration, RegisterAddressViewModel addressInput)
    {
        using var transaction = _dbContext.Database.BeginTransaction();

        var user = AddAccount(new AccountInput
        {
            FullName = pendingRegistration.FullName,
            Email = pendingRegistration.Email,
            PhoneNumber = pendingRegistration.PhoneNumber,
            Role = "User",
            Status = "Active",
            Notes = "Registered from storefront",
            Password = pendingRegistration.Password
        });

        _dbContext.UserAddresses.Add(new UserAddress
        {
            UserId = user.Id,
            Label = NormalizeOptionalText(addressInput.Label),
            RecipientName = NormalizeText(addressInput.RecipientName),
            Phone = NormalizeText(addressInput.PhoneNumber),
            AddressLine = NormalizeText(addressInput.AddressLine),
            PostalCode = NormalizeOptionalText(addressInput.PostalCode),
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _dbContext.SaveChanges();
        transaction.Commit();
    }

    private PendingRegistrationRecord? ReadPendingRegistration()
    {
        var raw = HttpContext.Session.GetString(PendingRegistrationSessionKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PendingRegistrationRecord>(raw);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void WritePendingRegistration(PendingRegistrationRecord pendingRegistration)
    {
        HttpContext.Session.SetString(
            PendingRegistrationSessionKey,
            JsonSerializer.Serialize(pendingRegistration));
    }

    private void ClearPendingRegistration()
    {
        HttpContext.Session.Remove(PendingRegistrationSessionKey);
    }

    private sealed class PendingRegistrationRecord
    {
        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
