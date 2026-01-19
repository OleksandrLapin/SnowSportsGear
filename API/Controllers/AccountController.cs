using System.Security.Claims;
using System.Text;
using API.DTOs;
using API.Extensions;
using Core.Constants;
using Core.Entities;
using Core.Interfaces;
using Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace API.Controllers;

//?????????? ???????????????:
public class AccountController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    INotificationService notificationService,
    IOptions<NotificationSettings> notificationOptions) : BaseApiController
{
    private readonly NotificationSettings notificationSettings = notificationOptions.Value;
    private const string EmailServiceUnavailableMessage = "Email service is not configured. Contact support.";

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
    {
        var user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            UserName = registerDto.Email,
            TwoFactorEnabled = notificationSettings.RequireTwoFactorOnLogin
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

        await SendEmailConfirmationAsync(user);
        await SendWelcomeEmailAsync(user);

        return Ok(new LoginResultDto
        {
            Success = true,
            RequiresEmailConfirmation = true,
            Message = "Registration successful. Check your email to confirm your account."
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login([FromBody] LoginDto loginDto)
    {
        var user = await userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            return Unauthorized(new LoginResultDto { Success = false, Message = "Invalid credentials" });
        }

        if (!user.EmailConfirmed)
        {
            var confirmationSent = await SendEmailConfirmationAsync(user);
            if (!confirmationSent)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResultDto
                {
                    Success = false,
                    Message = EmailServiceUnavailableMessage
                });
            }
            return StatusCode(StatusCodes.Status403Forbidden, new LoginResultDto
            {
                Success = false,
                RequiresEmailConfirmation = true,
                Message = "Email confirmation required"
            });
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            await SendSuspiciousActivityAsync(user);
            return Unauthorized(new LoginResultDto { Success = false, Message = "Account locked due to failed attempts" });
        }

        if (!result.Succeeded)
        {
            await NotifySuspiciousIfThresholdReachedAsync(user);
            return Unauthorized(new LoginResultDto { Success = false, Message = "Invalid credentials" });
        }

        if (user.TwoFactorEnabled)
        {
            var twoFactorSent = await SendTwoFactorCodeAsync(user);
            if (!twoFactorSent)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new LoginResultDto
                {
                    Success = false,
                    Message = EmailServiceUnavailableMessage
                });
            }
            return Ok(new LoginResultDto
            {
                Success = false,
                RequiresTwoFactor = true,
                Message = "Two-factor code sent"
            });
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Ok(new LoginResultDto { Success = true });
    }

    [HttpPost("verify-2fa")]
    public async Task<ActionResult<LoginResultDto>> VerifyTwoFactor([FromBody] TwoFactorDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return Unauthorized(new LoginResultDto { Success = false, Message = "Invalid credentials" });
        }

        var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, dto.Code);
        if (!valid)
        {
            return Unauthorized(new LoginResultDto { Success = false, Message = "Invalid code" });
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        return Ok(new LoginResultDto { Success = true });
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
            IsAdmin = roles.Contains("Admin"),
            TwoFactorEnabled = user.TwoFactorEnabled
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

    [HttpPost("confirm-email")]
    public async Task<ActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return BadRequest("Invalid email");

        var token = DecodeToken(dto.Token);
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded) return BadRequest("Invalid confirmation code");

        return Ok("Email confirmed");
    }

    [HttpPost("resend-confirmation")]
    public async Task<ActionResult> ResendConfirmation([FromBody] ForgotPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return BadRequest("Email not found");

        var sent = await SendEmailConfirmationAsync(user);
        if (!sent) return StatusCode(StatusCodes.Status500InternalServerError, EmailServiceUnavailableMessage);
        return Ok("Confirmation sent");
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return BadRequest("Email not found");

        var sent = await SendPasswordResetAsync(user);
        if (!sent) return StatusCode(StatusCodes.Status500InternalServerError, EmailServiceUnavailableMessage);
        return Ok("Password reset email sent");
    }

    [HttpPost("password-reset")]
    public async Task<ActionResult> PasswordReset([FromBody] ResetPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null) return BadRequest("Email not found");

        var token = DecodeToken(dto.Token);
        var result = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem();
        }

        await SendPasswordChangedAsync(user);
        return Ok("Password updated");
    }

    [Authorize]
    [HttpPost("request-password-reset")]
    public async Task<ActionResult> RequestPasswordReset()
    {
        var user = await userManager.GetUserByEmail(User);
        var sent = await SendPasswordResetAsync(user);
        if (!sent) return StatusCode(StatusCodes.Status500InternalServerError, EmailServiceUnavailableMessage);
        return Ok("Password reset email sent");
    }

    [Authorize]
    [HttpPost("request-email-change")]
    public async Task<ActionResult> RequestEmailChange([FromBody] ChangeEmailRequestDto dto)
    {
        var user = await userManager.GetUserByEmail(User);
        if (string.Equals(user.Email, dto.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("New email must be different");
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);
        var encoded = EncodeToken(token);
        var confirmUrl = BuildAppUrl($"/account/confirm-email-change?userId={Uri.EscapeDataString(user.Id)}&newEmail={Uri.EscapeDataString(dto.NewEmail)}&token={Uri.EscapeDataString(encoded)}");

        var tokens = BuildBaseTokens(user);
        tokens["Code"] = encoded;
        tokens["CodeExpiry"] = $"{notificationSettings.SecurityCodeExpiryMinutes} minutes";
        tokens["ConfirmUrl"] = confirmUrl;

        var sent = await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountEmailChange,
            dto.NewEmail,
            tokens,
            user.Id));

        if (!sent) return StatusCode(StatusCodes.Status500InternalServerError, EmailServiceUnavailableMessage);
        return Ok("Email change confirmation sent");
    }

    [HttpPost("confirm-email-change")]
    public async Task<ActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto dto)
    {
        var user = await userManager.FindByIdAsync(dto.UserId);
        if (user == null) return BadRequest("Invalid user");

        var token = DecodeToken(dto.Token);
        var result = await userManager.ChangeEmailAsync(user, dto.NewEmail, token);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return ValidationProblem();
        }

        user.UserName = dto.NewEmail;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        return Ok("Email updated");
    }

    [Authorize]
    [HttpPost("request-deletion")]
    public async Task<ActionResult> RequestDeletion()
    {
        var user = await userManager.GetUserByEmail(User);
        var tokens = BuildBaseTokens(user);
        tokens["ProcessingTime"] = $"{notificationSettings.DeletionRequestSlaDays} days";

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountDeletionRequest,
            user.Email ?? string.Empty,
            tokens,
            user.Id));

        await NotifyAdminsAsync(NotificationTemplateKeys.AdminAccountRequest, new Dictionary<string, string>(tokens)
        {
            ["RequestType"] = "Account deletion",
            ["AdminDashboardUrl"] = BuildAppUrl("/admin")
        });

        return Ok("Deletion request received");
    }

    [Authorize]
    [HttpPost("request-data-export")]
    public async Task<ActionResult> RequestDataExport()
    {
        var user = await userManager.GetUserByEmail(User);
        var tokens = BuildBaseTokens(user);

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountDataExportRequest,
            user.Email ?? string.Empty,
            tokens,
            user.Id));

        await NotifyAdminsAsync(NotificationTemplateKeys.AdminAccountRequest, new Dictionary<string, string>(tokens)
        {
            ["RequestType"] = "Data export",
            ["AdminDashboardUrl"] = BuildAppUrl("/admin")
        });

        return Ok("Data export request received");
    }

    [Authorize]
    [HttpPost("toggle-2fa")]
    public async Task<ActionResult> ToggleTwoFactor([FromQuery] bool enabled)
    {
        var user = await userManager.GetUserByEmail(User);
        user.TwoFactorEnabled = enabled;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest("Unable to update two-factor settings");
        return Ok(new { TwoFactorEnabled = enabled });
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
            IsAdmin = (await userManager.GetRolesAsync(user)).Contains("Admin"),
            TwoFactorEnabled = user.TwoFactorEnabled
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
            await SendPasswordChangedAsync(user);
            return Ok("Password updated");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return ValidationProblem();
    }

    private async Task<bool> SendEmailConfirmationAsync(AppUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encoded = EncodeToken(token);
        var confirmUrl = BuildAppUrl($"/account/confirm-email?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(encoded)}");

        var tokens = BuildBaseTokens(user);
        tokens["Code"] = encoded;
        tokens["CodeExpiry"] = $"{notificationSettings.SecurityCodeExpiryMinutes} minutes";
        tokens["ConfirmUrl"] = confirmUrl;

        return await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountEmailConfirmation,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task SendWelcomeEmailAsync(AppUser user)
    {
        var tokens = BuildBaseTokens(user);
        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountWelcome,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task<bool> SendPasswordResetAsync(AppUser user)
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = EncodeToken(token);
        var resetUrl = BuildAppUrl($"/account/reset-password?email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(encoded)}");

        var tokens = BuildBaseTokens(user);
        tokens["Code"] = encoded;
        tokens["CodeExpiry"] = $"{notificationSettings.SecurityCodeExpiryMinutes} minutes";
        tokens["ResetUrl"] = resetUrl;

        return await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountPasswordReset,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task SendPasswordChangedAsync(AppUser user)
    {
        var resetUrl = BuildAppUrl($"/account/forgot-password?email={Uri.EscapeDataString(user.Email ?? string.Empty)}");
        var tokens = BuildBaseTokens(user);
        tokens["ResetUrl"] = resetUrl;

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountPasswordChanged,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task<bool> SendTwoFactorCodeAsync(AppUser user)
    {
        var code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        var tokens = BuildBaseTokens(user);
        tokens["Code"] = code;
        tokens["CodeExpiry"] = $"{notificationSettings.SecurityCodeExpiryMinutes} minutes";

        return await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountTwoFactorCode,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task NotifySuspiciousIfThresholdReachedAsync(AppUser user)
    {
        var failedCount = await userManager.GetAccessFailedCountAsync(user);
        if (failedCount < 7) return;

        await SendSuspiciousActivityAsync(user, failedCount);
    }

    private async Task SendSuspiciousActivityAsync(AppUser user, int? failedAttempts = null)
    {
        var resetUrl = BuildAppUrl($"/account/forgot-password?email={Uri.EscapeDataString(user.Email ?? string.Empty)}");
        var tokens = BuildBaseTokens(user);
        tokens["FailedAttempts"] = (failedAttempts ?? user.AccessFailedCount).ToString();
        tokens["ResetUrl"] = resetUrl;

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.AccountSuspiciousActivity,
            user.Email ?? string.Empty,
            tokens,
            user.Id));
    }

    private async Task NotifyAdminsAsync(string templateKey, IDictionary<string, string> tokens)
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        var requests = admins
            .Where(a => !string.IsNullOrWhiteSpace(a.Email))
            .Select(a => new Core.Models.Notifications.NotificationRequest(
                templateKey,
                a.Email ?? string.Empty,
                tokens,
                a.Id))
            .ToList();

        if (requests.Count > 0)
        {
            await notificationService.SendBulkAsync(requests);
        }
    }

    private Dictionary<string, string> BuildBaseTokens(AppUser user)
    {
        return new Dictionary<string, string>
        {
            ["CustomerName"] = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim(),
            ["CustomerEmail"] = user.Email ?? string.Empty
        };
    }

    private string BuildAppUrl(string path)
    {
        return $"{notificationSettings.StoreUrl}{path}";
    }

    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private static string DecodeToken(string token)
    {
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
    }
}
