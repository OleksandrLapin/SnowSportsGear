using System.Security.Claims;
using API.DTOs;
using API.Extensions;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
//Управление аутентификацией:
public class AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager) : BaseApiController
{
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
    {
        var user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            UserName = registerDto.Email
        };

        var result = await signInManager.UserManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem();
        }

        return Ok();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        return NoContent();
    }

    [HttpGet("user-info")]
    public async Task<ActionResult> GetUserInfo()
    {
        if (User.Identity?.IsAuthenticated == false) return NoContent();

        var user = await signInManager.UserManager.GetUserByEmailWithAddress(User);
        var roles = await userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            Address = user.Address?.ToDto(),
            Roles = roles,
            IsAdmin = roles.Contains("Admin")
        });
    }

    [HttpGet("auth-status")]
    public ActionResult GetAuthState()
    {
        return Ok(new { IsAuthenticated = User.Identity?.IsAuthenticated ?? false });
    }

    [Authorize]
    [HttpPost("address")]
    public async Task<ActionResult<Address>> CreateOrUpdateAddress(AddressDto addressDto)
    {
        var user = await signInManager.UserManager.GetUserByEmailWithAddress(User);

        if (user.Address == null)
        {
            user.Address = addressDto.ToEntity();
        }
        else 
        {
            user.Address.UpdateFromDto(addressDto);
        }

        var result = await signInManager.UserManager.UpdateAsync(user);

        if (!result.Succeeded) return BadRequest("Problem updating user address");

        return Ok(user.Address.ToDto());
    }

    [Authorize]
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword(string currentPassword, string newPassword)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);

        var result = await signInManager.UserManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            return Ok("Password updated");
        } 

        return BadRequest("Failed to update password");
    }

    [Authorize]
    [HttpPost("profile")]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();

        var result = await signInManager.UserManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem();
        }

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.Email,
            Address = user.Address?.ToDto(),
            Roles = await userManager.GetRolesAsync(user),
            IsAdmin = (await userManager.GetRolesAsync(user)).Contains("Admin")
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] UpdatePasswordDto dto)
    {
        var user = await signInManager.UserManager.GetUserByEmail(User);

        var result = await signInManager.UserManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

        if (result.Succeeded)
        {
            return Ok("Password updated");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem();
    }
}
