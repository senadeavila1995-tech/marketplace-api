namespace Marketplace.API.Models;

public class SellerPaymentAccount
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int StoreId { get; set; }

    public string Provider { get; set; } = "PAYPAL";

    public string PaypalEmail { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public Store Store { get; set; } = null!;
}