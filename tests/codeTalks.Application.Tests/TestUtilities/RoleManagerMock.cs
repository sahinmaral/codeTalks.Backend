using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.TestUtilities;

/// <summary>
/// RoleManager&lt;Role&gt; has a 5-argument constructor and cannot be substituted with a
/// bare call. This builds a substitute over stubbed dependencies; its public methods
/// (e.g. FindByNameAsync) are virtual, so return values can be configured per test.
/// </summary>
public static class RoleManagerMock
{
    public static RoleManager<Role> Create()
    {
        var store = Substitute.For<IRoleStore<Role>>();

        return Substitute.For<RoleManager<Role>>(
            store,
            null, null, null, null);
    }
}
