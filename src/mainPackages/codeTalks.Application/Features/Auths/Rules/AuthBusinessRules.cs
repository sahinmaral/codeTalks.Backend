using Core.CrossCuttingConcerns.Exceptions;
using Core.Security.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace codeTalks.Application.Features.Auths.Rules;

public class AuthBusinessRules(UserManager<User> userManager)
{
    public async Task<User> CheckUserExistsById(string id)
    {
        User? foundUser = await userManager.FindByIdAsync(id);

        if (foundUser is null)
            throw new EntityNotFoundException("User does not found");

        return foundUser;
    }

    public async Task CheckUserWithUsernameAlreadyExists(string userName)
    {
        User? foundUser = await userManager.FindByNameAsync(userName);

        if (foundUser is not null)
            throw new BusinessException("This username is already taken");
    }

    public async Task CheckUserWithEmailAlreadyExists(string email)
    {
        User? foundUser = await userManager.FindByEmailAsync(email);

        if (foundUser is not null)
            throw new BusinessException("This e-mail address is already taken");
    }

    public async Task<User> CheckUserWithEmailOrUsernameExists(string usernameOrEmail)
    {
        User? user = await userManager
              .Users
              .Where(x => x.UserName == usernameOrEmail || x.Email == usernameOrEmail)
              .FirstOrDefaultAsync();

        if (user is null)
            throw new EntityNotFoundException("There is no user with this username or email address");

        return user;
    }

    public async Task CheckIfUserEnteredCorrectPassword(User user, string password)
    {
        var passwordCheckResult = await userManager.CheckPasswordAsync(user, password);
        if (!passwordCheckResult)
            throw new BusinessException("You entered the wrong password along with the username or email you entered");
    }

    public Task CheckIfUserEmailHasBeenVerified(User user)
    {
        if (!user.EmailConfirmed)
            throw new BusinessException("Please confirm your account via the e-mail sent to the e-mail address you registered with");

        return Task.CompletedTask;
    }
}