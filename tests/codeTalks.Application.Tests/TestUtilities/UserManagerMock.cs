using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace codeTalks.Application.Tests.TestUtilities;

/// <summary>
/// UserManager&lt;User&gt; has a 9-argument constructor, so it cannot be substituted
/// with a parameterless call. This helper builds a substitute with the required
/// dependencies stubbed out. Its public methods are virtual, so NSubstitute can
/// configure return values on the instance it produces.
/// </summary>
public static class UserManagerMock
{
    public static UserManager<User> Create()
    {
        var store = Substitute.For<IUserStore<User>>();

        return Substitute.For<UserManager<User>>(
            store,
            null, null, null, null, null, null, null, null);
    }
}