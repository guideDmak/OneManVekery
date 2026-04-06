using System.ComponentModel.DataAnnotations;

namespace OneManVekery.ViewModel;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทร")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย 8 ตัวอักษร")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class RegisterAddressViewModel
{
    public string AccountFullName { get; set; } = string.Empty;

    public string AccountEmail { get; set; } = string.Empty;

    public string AccountPhoneNumber { get; set; } = string.Empty;

    public string Label { get; set; } = "บ้าน";

    [Required(ErrorMessage = "กรุณากรอกชื่อผู้รับ")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรสำหรับจัดส่ง")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรไม่ถูกต้อง")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกที่อยู่จัดส่ง")]
    public string AddressLine { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;
}

public class AccountProfileViewModel
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string RoleLabel { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public string AccountCode { get; init; } = string.Empty;

    public DateTime LastActiveAt { get; init; }

    public int CurrentPoints { get; init; }

    public int LifetimeEarnedPoints { get; init; }

    public int LifetimeRedeemedPoints { get; init; }

    public IReadOnlyList<AccountAddressCardViewModel> Addresses { get; init; } = [];
}

public class AccountAddressCardViewModel
{
    public string Label { get; init; } = string.Empty;

    public string RecipientName { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string AddressLine { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public bool IsDefault { get; init; }
}
