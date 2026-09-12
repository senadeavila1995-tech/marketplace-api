namespace Marketplace.API.DTOs;

public class CreateStoreDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}