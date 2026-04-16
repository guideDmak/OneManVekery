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
    private void ValidateItemImageUpload(IFormFile? imageFile, string modelField)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return;
        }

        if (imageFile.Length > MaxItemImageUploadBytes)
        {
            ModelState.AddModelError(modelField, "รูปสินค้าต้องมีขนาดไม่เกิน 5MB");
            return;
        }

        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedItemImageUploadExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(modelField, "รองรับเฉพาะไฟล์ .jpg, .jpeg, .png, .webp หรือ .gif");
        }
    }

    private bool TryApplyUploadedItemImage(AdminItemEditorViewModel form, string modelField)
    {
        if (form.ImageFile is null || form.ImageFile.Length == 0)
        {
            form.ImagePath = NormalizeInventoryImagePath(form.ImagePath);
            return true;
        }

        try
        {
            form.ImagePath = SaveItemImageUpload(form.ImageFile);
            return true;
        }
        catch (IOException)
        {
            ModelState.AddModelError(modelField, "บันทึกรูปสินค้าไม่สำเร็จ กรุณาลองใหม่");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            ModelState.AddModelError(modelField, "ระบบไม่มีสิทธิ์บันทึกรูปสินค้าในโฟลเดอร์ wwwroot");
            return false;
        }
    }

    private string SaveItemImageUpload(IFormFile imageFile)
    {
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : _environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRootPath, "images", "products");
        Directory.CreateDirectory(uploadDirectory);

        var safeName = BuildSafeImageFileNameStem(imageFile.FileName);
        var fileNameStem = $"{safeName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
        if (fileNameStem.Length > 72)
        {
            fileNameStem = fileNameStem[..72].TrimEnd('-');
        }

        var fileName = fileNameStem + extension;
        var filePath = Path.Combine(uploadDirectory, fileName);

        using var stream = System.IO.File.Create(filePath);
        imageFile.CopyTo(stream);

        return $"/images/products/{fileName}";
    }

    private static string BuildSafeImageFileNameStem(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var characters = stem
            .Select(character =>
            {
                var lower = char.ToLowerInvariant(character);
                return (lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9') ? lower : '-';
            })
            .ToArray();
        var normalized = string.Join("-", new string(characters).Split('-', StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(normalized) ? "item-image" : normalized;
    }

    private IReadOnlyList<string> BuildImageOptions(IReadOnlyList<InventoryItemRecord> items, params string?[] extraImagePaths)
    {
        return items
            .Select(item => item.ImagePath)
            .Concat(GetUploadedItemImageOptions())
            .Concat(
            [
                "/images/theme-cake.svg",
                "/images/theme-macaron.svg",
                "/images/theme-cream.svg",
                "/images/theme-gold.svg",
                "/images/theme-berry.svg",
                "/images/theme-milk.svg"
            ])
            .Concat(extraImagePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => path!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private IReadOnlyList<string> GetUploadedItemImageOptions()
    {
        var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : _environment.WebRootPath;
        var uploadDirectory = Path.Combine(webRootPath, "images", "products");

        if (!Directory.Exists(uploadDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(uploadDirectory)
            .Where(path => AllowedItemImageUploadExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => $"/images/products/{Path.GetFileName(path)}")
            .ToList();
    }
}
