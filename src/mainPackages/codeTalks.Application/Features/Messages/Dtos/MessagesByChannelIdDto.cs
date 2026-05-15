namespace codeTalks.Application.Features.Messages.Dtos;

public record MessagesByChannelIdDto(
    string Id,
    string Content, 
    UserWhoWroteMessageByChannelIdDto Sender, 
    string ChannelId,
    DateTime CreatedAt
    );
    
public record UserWhoWroteMessageByChannelIdDto(
    string Id,
    string UserName,
    string ProfilePhotoURL
);