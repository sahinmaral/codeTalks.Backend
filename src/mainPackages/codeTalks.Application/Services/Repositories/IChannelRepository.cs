using System.Linq.Expressions;
using codeTalks.Domain;
using Core.Persistence.Paging;
using Core.Persistence.Repositories;

namespace codeTalks.Application.Services.Repositories;

public interface IChannelRepository : IAsyncRepository<Channel>, IRepository<Channel>
{
    Task<IList<TResult>> GetChannelAdminsAsync<TResult>(
        Expression<Func<ChannelUser, TResult>> selector,
        string channelId,
        CancellationToken cancellationToken = default);

    Task<IPaginate<TResult>> GetChannelUsersAsync<TResult>(
        Expression<Func<ChannelUser, TResult>> selector,
        string channelId,
        ChannelUserStatus status,
        string? excludeRoleName = null,
        string? search = null,
        int index = 0,
        int size = 10,
        CancellationToken cancellationToken = default);
}