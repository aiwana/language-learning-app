using System.ComponentModel.DataAnnotations;

namespace WebShadowing.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class AuthPageViewModel
{
    public LoginViewModel Login { get; set; } = new();
    public RegisterViewModel Register { get; set; } = new();
    public CompleteOnboardingViewModel Onboarding { get; set; } = new();
    public string ActiveStep { get; set; } = "login";
}

public class CompleteOnboardingViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn hình thức học.")]
    public string LearningMode { get; set; } = LearningModes.Casual;

    [Required(ErrorMessage = "Vui lòng chọn chuẩn phát âm.")]
    public string Accent { get; set; } = Accents.EnUs;

    [Required(ErrorMessage = "Vui lòng chọn mục tiêu phát âm.")]
    [Range(1, 100, ErrorMessage = "Mục tiêu không hợp lệ.")]
    public byte PronunciationTarget { get; set; } = PronunciationTargets.Comprehension70;

    [Required]
    public string Plan { get; set; } = "free";
}
