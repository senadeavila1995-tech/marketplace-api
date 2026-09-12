namespace Marketplace.API.DTOs.Auth;

public class RegisterDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // SOLO CUSTOMER O SELLER
    public string Role { get; set; } = string.Empty;
}