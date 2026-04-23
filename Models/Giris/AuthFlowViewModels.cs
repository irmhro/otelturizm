using System.ComponentModel.DataAnnotations;

namespace otelturizmnew.Models.Giris;

public class EmailVerificationViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 ile 8 karakter arasında olmalıdır.")]
    public string Code { get; set; } = string.Empty;

    public string? Token { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "Şifre sıfırlama bağlantısı geçersiz.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Şifre en az 1 harf ve 1 rakam içermelidir.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [MinLength(6, ErrorMessage = "Şifre tekrarı en az 6 karakter olmalıdır.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginTwoFactorViewModel
{
    [Required]
    [StringLength(8, MinimumLength = 4)]
    public string Code { get; set; } = string.Empty;

    public string Channel { get; set; } = "email";
    public string ChannelLabel { get; set; } = "E-posta";
    public string DestinationHint { get; set; } = string.Empty;
    public string InlineHint { get; set; } = "Girişinizi tamamlamak için size iletilen güvenlik kodunu girin.";
}

public sealed class AuthFlowException : Exception
{
    public string? ErrorCode { get; }
    public string? RelatedEmail { get; }

    public AuthFlowException(string message, string? errorCode = null, string? relatedEmail = null)
        : base(message)
    {
        ErrorCode = errorCode;
        RelatedEmail = relatedEmail;
    }
}

public static class AuthFlowErrorCodes
{
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
}
