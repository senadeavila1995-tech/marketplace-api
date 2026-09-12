namespace Marketplace.API.Models;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Method { get; set; } = "SIMULATED";

    public string Status { get; set; } = "PENDING"; 
    // PENDING | COMPLETED | FAILED

    public DateTime? PaidAt { get; set; }

    public int? SellerId { get; set; }

    public int? StoreId { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public bool AdminConfirmed { get; set; } = false;

    // 🔥 ESCROW REAL
    public string EscrowStatus { get; set; } = "HELD"; 
    // HELD | READY | RELEASED

    public Order Order { get; set; }
}