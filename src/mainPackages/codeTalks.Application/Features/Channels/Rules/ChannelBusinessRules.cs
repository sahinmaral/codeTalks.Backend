using codeTalks.Application.Services.Repositories;
using codeTalks.Domain;
using Core.CrossCuttingConcerns.Exceptions;

namespace codeTalks.Application.Features.Channels.Rules;

public class ChannelBusinessRules(IChannelRepository channelRepository)
{
    public async Task CheckChannelExistsById(string id)
    {
        Channel? foundChannel = await channelRepository.GetAsync(predicate: channel => channel.Id == id);

        if (foundChannel is null)
            throw new EntityNotFoundException("Channel does not found");
    }
}