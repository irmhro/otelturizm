namespace otelturizmnew.Models.Messages;

public class MessageCenterThreadViewModel
{
    public long ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AvatarText { get; set; } = "OT";
    public string AvatarTone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int UnreadCount { get; set; }
    public string RouteUrl { get; set; } = string.Empty;
}

public class MessageCenterItemViewModel
{
    public long MessageId { get; set; }
    public bool IsOutgoing { get; set; }
    public bool IsDeleted { get; set; }
    public bool CanDelete { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string DeletedText { get; set; } = "Bu mesaj silindi.";
    public List<MessageAttachmentViewModel> Attachments { get; set; } = new();
}

public class MessageAttachmentViewModel
{
    public long FileId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string SizeText { get; set; } = string.Empty;
    public string AccessUrl { get; set; } = string.Empty;
    public bool IsImage { get; set; }
}

public class MessageSendRequest
{
    public long ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MessageDeleteRequest
{
    public long ConversationId { get; set; }
    public long MessageId { get; set; }
}

public class MessageInboxResult
{
    public List<MessageCenterThreadViewModel> Threads { get; set; } = new();
    public long? SelectedConversationId { get; set; }
    public string SelectedTitle { get; set; } = "Mesajlar";
    public string SelectedSubtitle { get; set; } = "Yazışma detayları";
    public List<MessageCenterItemViewModel> Messages { get; set; } = new();
}

public class SecureFileSaveRequest
{
    public string ContextTable { get; set; } = string.Empty;
    public long ContextId { get; set; }
    public long? OwnerUserId { get; set; }
    public long? OwnerFirmaId { get; set; }
    public string VisibilityScope { get; set; } = "private";
    public string Category { get; set; } = "message";
}

public class StoredSecureFileResult
{
    public long FileId { get; set; }
    public string StoredPath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public long SizeInBytes { get; set; }
}

public class SecureFileDownloadResult
{
    public string AbsolutePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}
