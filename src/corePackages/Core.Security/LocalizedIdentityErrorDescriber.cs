using Core.CrossCuttingConcerns;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Core.Security;

/// <summary>
/// Produces localized descriptions for the ASP.NET Core Identity errors that can surface to the
/// client (currently the password errors raised by <c>ChangePasswordAsync</c>/<c>CreateAsync</c>).
/// The English default string is used as the resource key, so English requests fall back to it
/// unchanged and Turkish requests are resolved from SharedResource.tr.resx.
/// </summary>
public class LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> localizer) : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => new()
    {
        Code = nameof(DefaultError),
        Description = localizer["An unknown failure has occurred."]
    };

    public override IdentityError PasswordMismatch() => new()
    {
        Code = nameof(PasswordMismatch),
        Description = localizer["Incorrect password."]
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new()
    {
        Code = nameof(PasswordRequiresNonAlphanumeric),
        Description = localizer["Passwords must have at least one non alphanumeric character."]
    };

    public override IdentityError PasswordRequiresDigit() => new()
    {
        Code = nameof(PasswordRequiresDigit),
        Description = localizer["Passwords must have at least one digit ('0'-'9')."]
    };

    public override IdentityError PasswordRequiresLower() => new()
    {
        Code = nameof(PasswordRequiresLower),
        Description = localizer["Passwords must have at least one lowercase ('a'-'z')."]
    };

    public override IdentityError PasswordRequiresUpper() => new()
    {
        Code = nameof(PasswordRequiresUpper),
        Description = localizer["Passwords must have at least one uppercase ('A'-'Z')."]
    };

    public override IdentityError PasswordTooShort(int length) => new()
    {
        Code = nameof(PasswordTooShort),
        Description = localizer["Passwords must be at least {0} characters.", length]
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
    {
        Code = nameof(PasswordRequiresUniqueChars),
        Description = localizer["Passwords must use at least {0} different characters.", uniqueChars]
    };
}
