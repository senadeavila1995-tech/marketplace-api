using System.Collections.Generic;

namespace Marketplace.API.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
    public List<OrderItem> Items { get; set; }
}