using System.ComponentModel.DataAnnotations;

namespace otelturizmnew.Models.Giris;

public class EmailVerificationViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public string? Token { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Şifre en az 1 harf ve 1 rakam içermelidir.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string ConfirmPassword { get; set; } = string.Empty;
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
