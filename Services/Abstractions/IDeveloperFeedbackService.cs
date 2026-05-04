using otelturizmnew.Models.DeveloperFeedback;

namespace otelturizmnew.Services.Abstractions;

public interface IDeveloperFeedbackService
{
    Task<(bool Success, string Message)> CreateAsync(
        long userId,
        string? fullName,
        string? email,
        string? accountType,
        string? ipAddress,
        string? userAgent,
        DeveloperFeedbackForm form,
        CancellationToken cancellationToken = default);

    Task<DeveloperFeedbackHistoryResponse> GetUserHistoryAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> UpdateAsync(
        long userId,
        DeveloperFeedbackForm form,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteAsync(
        long userId,
        long feedbackId,
        CancellationToken cancellationToken = default);
}
