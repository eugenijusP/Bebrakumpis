namespace Bebrakumpis.Application.Features.Users.DTOs;

public class UpdateUserRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
