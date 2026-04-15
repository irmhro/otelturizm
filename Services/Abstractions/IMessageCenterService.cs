using otelturizmnew.Models.Messages;

namespace otelturizmnew.Services.Abstractions;

public interface IMessageCenterService
{
    Task<MessageInboxResult> GetUserInboxAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
    Task<MessageInboxResult> GetFirmaInboxAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
    Task<(bool Allowed, string Message)> CanStartHotelConversationAsync(long userId, long hotelId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? ConversationId)> StartHotelConversationForUserAsync(long userId, long hotelId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendFromUserAsync(long userId, MessageSendRequest request, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendFromFirmaAsync(long userId, MessageSendRequest request, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteForUserAsync(long userId, MessageDeleteRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteForFirmaAsync(long userId, MessageDeleteRequest request, CancellationToken cancellationToken = default);
}
