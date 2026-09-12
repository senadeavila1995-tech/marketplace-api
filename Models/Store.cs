namespace Marketplace.API.Models;

public class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // dueño de la tienda
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // relación 1 a muchos
    public List<Product> Products { get; set; } = new();
}