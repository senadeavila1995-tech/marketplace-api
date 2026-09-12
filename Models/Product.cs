using System;

namespace Marketplace.API.Models;

public class Product
{
    public int Id { get; set; }

    public int StoreId { get; set; }
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public string? ImageUrl { get; set; }
    public bool Status { get; set; }

    public DateTime CreatedAt { get; set; }

    // relaciones
    public Store Store { get; set; } = null!;
    public Category Category { get; set; } = null!;
}