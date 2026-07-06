namespace codeTalks.Application.Services.Notifications.Models;

public record ChannelMessagePayload(
    string MessageId,
    string ChannelId,
    string ChannelName,
    string SenderId,
    string SenderName,
    string Content,
    DateTime SentAt,
    bool WithSound
);