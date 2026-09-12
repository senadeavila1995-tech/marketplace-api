using Marketplace.API.Data;
using Marketplace.API.Models;
using Marketplace.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // CHECKOUT
    // =========================
    [Authorize(Roles = "CUSTOMER")]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Token inválido");

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("Carrito vacío");

        if (dto.Items.Any(item => item.Quantity <= 0))
            return BadRequest("La cantidad de los productos debe ser mayor que cero");

        var paymentMethod = dto.PaymentMethod?.Trim().ToUpperInvariant();

        var allowedPaymentMethods = new[]
        {
            "CASH_ON_DELIVERY",
            "NEQUI",
            "PAYPAL"
        };

        if (string.IsNullOrWhiteSpace(paymentMethod) ||
            !allowedPaymentMethods.Contains(paymentMethod))
        {
            return BadRequest(
                "Método de pago inválido. Usa CASH_ON_DELIVERY, NEQUI o PAYPAL"
            );
        }

        var duplicateProducts = dto.Items
            .GroupBy(x => x.ProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateProducts.Any())
            return BadRequest(
                $"El producto {duplicateProducts[0]} aparece más de una vez en el checkout"
            );

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            decimal total = 0;

            var orderItems = new List<OrderItem>();
            var payments = new List<Payment>();
            var productIds = dto.Items.Select(x => x.ProductId).ToList();

            var products = await _context.Products
                .Include(p => p.Store)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in dto.Items)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    return BadRequest($"Producto {item.ProductId} no encontrado");

                if (!product.Status)
                    return BadRequest(
                        $"El producto {product.Name} no está disponible"
                    );

                if (product.Store == null)
                    return BadRequest(
                        $"El producto {product.Name} no tiene tienda asociada"
                    );

                if (product.Stock < item.Quantity)
                    return BadRequest(
                        $"Stock insuficiente para el producto {product.Name}. " +
                        $"Disponible: {product.Stock}, solicitado: {item.Quantity}"
                    );
            }

            var order = new Order
            {
                UserId = userId,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow,
                Total = 0
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                var product = products[item.ProductId];

                var subtotal = product.Price * item.Quantity;

                total += subtotal;

                orderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price
                });

                payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Method = paymentMethod,
                    Status = "PENDING",
                    SellerId = product.Store.UserId,
                    StoreId = product.StoreId,
                    PaidAt = DateTime.UtcNow,
                    AdminConfirmed = false,
                    EscrowStatus = "HELD"
                });

                product.Stock -= item.Quantity;
            }

            order.Total = total;

            _context.OrderItems.AddRange(orderItems);
            _context.Payments.AddRange(payments);

            var cartItems = await _context.CartItems
                .Where(c =>
                    c.UserId == userId &&
                    productIds.Contains(c.ProductId)
                )
                .ToListAsync();

            if (cartItems.Count > 0)
            {
                _context.CartItems.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Pedido creado correctamente",
                order.Id,
                total,
                stockUpdated = true,
                cartCleared = cartItems.Count > 0
            });
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(
                500,
                "Ocurrió un error al procesar el pedido"
            );
        }
    }

    // =========================
    // SELLER ORDERS
    // =========================
    [Authorize(Roles = "SELLER")]
    [HttpGet("seller")]
    public async Task<IActionResult> GetSellerOrders()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Token inválido");

        var orders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Store)
            .Where(o =>
                o.Items.Any(oi =>
                    oi.Product.Store != null &&
                    oi.Product.Store.UserId == userId
                )
            )
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.CreatedAt,
                o.Total,
                PaymentMethod = o.Items
                    .Where(oi =>
                        oi.Product.Store != null &&
                        oi.Product.Store.UserId == userId
                    )
                    .Select(oi => _context.Payments
                        .Where(p => p.OrderId == o.Id)
                        .Select(p => p.Method)
                        .FirstOrDefault()
                    )
                    .FirstOrDefault(),
                Items = o.Items
                    .Where(oi =>
                        oi.Product.Store != null &&
                        oi.Product.Store.UserId == userId
                    )
                    .Select(oi => new
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Name,
                        oi.Quantity,
                        oi.Price,
                        Subtotal = oi.Price * oi.Quantity
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    // =========================
    // MARK ORDER AS SHIPPED
    // =========================
    [Authorize(Roles = "SELLER")]
    [HttpPost("{id}/ship")]
    public async Task<IActionResult> Ship(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized("Token inválido");

        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Store)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound("Pedido no encontrado");

        if (order.Status != "PENDING" && order.Status != "PAID")
            return BadRequest(
                "El pedido no está disponible para despacho"
            );

        var sellerPayment = await _context.Payments
            .Where(p =>
                p.OrderId == order.Id &&
                p.SellerId == userId
            )
            .OrderBy(p => p.Id)
            .FirstOrDefaultAsync();

        if (sellerPayment == null)
            return BadRequest(
                "No se encontró información de pago para este pedido"
            );

        if (
            order.Status == "PENDING" &&
            sellerPayment.Method != "CASH_ON_DELIVERY"
        )
        {
            return BadRequest(
                "El pedido debe tener el pago confirmado antes de ser enviado"
            );
        }

        if (order.Items == null || order.Items.Count == 0)
            return BadRequest("El pedido no tiene productos");

        var sellerOwnsAllItems = order.Items.All(
            item => item.Product.Store.UserId == userId
        );

        if (!sellerOwnsAllItems)
            return Forbid();

        order.Status = "SHIPPED";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Pedido marcado como enviado correctamente",
            order.Id,
            order.Status
        });
    }
}
