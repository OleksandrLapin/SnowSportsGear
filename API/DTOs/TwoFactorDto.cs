namespace API.DTOs;

public class TwoFactorDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
