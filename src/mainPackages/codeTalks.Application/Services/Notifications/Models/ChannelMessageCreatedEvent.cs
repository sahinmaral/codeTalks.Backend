namespace codeTalks.Application.Services.Notifications.Models;

public record ChannelMessageCreatedEvent(
    string MessageId,
    string ChannelId,
    string ChannelName,  // ← add this
    string SenderId,
    string SenderName,   // ← add this
    string Content,
    DateTime SentAt
);