namespace Bebrakumpis.Application.Features.Users.DTOs;

public class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}
