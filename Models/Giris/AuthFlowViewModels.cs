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
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class AuthFlowException : Exception
{
    public AuthFlowException(string message)
        : base(message)
    {
    }
}
