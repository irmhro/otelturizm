namespace otelturizmnew.Models.Register;

public class UserRegistrationModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
    public bool AcceptKvkk { get; set; }
    public bool AcceptMarketing { get; set; }
}
