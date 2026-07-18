using System.Text;
using Core.Security.Encryption;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace Core.Security.UnitTests.Encryption;

public class EncryptionHelpersTests
{
    [Fact]
    public void CreateSecurityKey_WrapsUtf8BytesOfTheKey()
    {
        const string key = "some-signing-key-value";

        var securityKey = SecurityKeyHelper.CreateSecurityKey(key);

        securityKey.Should().BeOfType<SymmetricSecurityKey>();
        ((SymmetricSecurityKey)securityKey).Key.Should().Equal(Encoding.UTF8.GetBytes(key));
    }

    [Fact]
    public void CreateSigningCredentials_UsesHmacSha512AndGivenKey()
    {
        var securityKey = SecurityKeyHelper.CreateSecurityKey("some-signing-key-value");

        var credentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);

        credentials.Algorithm.Should().Be(SecurityAlgorithms.HmacSha512Signature);
        credentials.Key.Should().BeSameAs(securityKey);
    }
}