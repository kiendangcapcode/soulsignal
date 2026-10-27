using System.ComponentModel.DataAnnotations;

namespace SoulSignal.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email."), StringLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên hiển thị."), StringLength(80, MinimumLength = 2,
        ErrorMessage = "Tên hiển thị cần từ 2 đến 80 ký tự.")]
    public string DisplayName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(100, MinimumLength = 10,
        ErrorMessage = "Mật khẩu cần từ 10 đến 100 ký tự."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu."), DataType(DataType.Password),
        Compare(nameof(Password), ErrorMessage = "Hai mật khẩu chưa khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
