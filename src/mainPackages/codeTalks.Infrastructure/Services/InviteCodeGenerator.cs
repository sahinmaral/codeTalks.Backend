using System.Security.Cryptography;
using codeTalks.Application.Services;

namespace codeTalks.Infrastructure.Services;

public class InviteCodeGenerator : IInviteCodeGenerator
{
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 10;

    public string Generate()
    {
        return RandomNumberGenerator.GetString(Chars, Length);
    }
}