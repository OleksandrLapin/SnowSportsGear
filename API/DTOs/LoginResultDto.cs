namespace API.DTOs;

public class LoginResultDto
{
    public bool Success { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
    public string? Message { get; set; }
}
