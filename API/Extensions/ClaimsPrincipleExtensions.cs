using System.Security.Authentication;
using System.Security.Claims;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;

public static class ClaimsPrincipleExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id)) throw new AuthenticationException("User id not found");

        return id;
    }

    public static async Task<AppUser> GetUserByEmail(this UserManager<AppUser> userManager,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var userById = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (userById != null) return userById;
        }

        var email = user.GetEmail();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var userToReturn = await userManager.Users.FirstOrDefaultAsync(x =>
            x.NormalizedEmail == normalizedEmail);

        if (userToReturn == null) throw new AuthenticationException("User not found");

        return userToReturn;
    }

    public static async Task<AppUser> GetUserByEmailWithAddress(this UserManager<AppUser> userManager,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var userById = await userManager.Users
                .Include(x => x.Address)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (userById != null) return userById;
        }

        var email = user.GetEmail();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var userToReturn = await userManager.Users
            .Include(x => x.Address)
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail);

        if (userToReturn == null) throw new AuthenticationException("User not found");

        return userToReturn;
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? user.Identity?.Name
            ?? throw new AuthenticationException("Email claim not found");

        return email;
    }
}
