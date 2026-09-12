namespace Marketplace.API.DTOs;

public class CheckoutDto
{
    public List<CheckoutItemDto> Items { get; set; } = new();

    public string PaymentMethod { get; set; } = "CASH_ON_DELIVERY";
}

public class CheckoutItemDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}
