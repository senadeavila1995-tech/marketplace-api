using Marketplace.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Roles = "ADMIN")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // GET ALL PAYMENTS
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                p.Method,
                p.Status,
                p.PaidAt,
                p.SellerId,
                p.StoreId,
                p.ReleaseDate,
                p.AdminConfirmed,
                p.EscrowStatus
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return Ok(payments);
    }

    // =========================
    // CONFIRM PAYMENT
    // =========================
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound("Pago no encontrado");

        if (payment.EscrowStatus == "RELEASED")
            return BadRequest("El pago ya fue liberado");

        payment.AdminConfirmed = true;
        payment.EscrowStatus = "READY";

        // El pago confirmado cambia la orden de PENDING a PAID
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId);

        if (order != null && order.Status == "PENDING")
        {
            order.Status = "PAID";
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Pago confirmado correctamente",
            payment.Id,
            payment.OrderId,
            payment.AdminConfirmed,
            payment.EscrowStatus,
            orderStatus = order?.Status
        });
    }

    // =========================
    // RELEASE ESCROW
    // =========================
    [HttpPost("{id}/release")]
    public async Task<IActionResult> Release(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound("Pago no encontrado");

        if (!payment.AdminConfirmed)
            return BadRequest("El pago debe ser confirmado por el administrador antes de liberar el escrow");

        if (payment.EscrowStatus == "RELEASED")
            return BadRequest("El escrow ya fue liberado");

        payment.Status = "COMPLETED";
        payment.EscrowStatus = "RELEASED";
        payment.ReleaseDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Escrow liberado correctamente",
            payment.Id,
            payment.OrderId,
            payment.Status,
            payment.EscrowStatus,
            payment.ReleaseDate
        });
    }

    // =========================
    // GET PAYMENT BY ID
    // =========================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.OrderId,
                p.Method,
                p.Status,
                p.PaidAt,
                p.SellerId,
                p.StoreId,
                p.ReleaseDate,
                p.AdminConfirmed,
                p.EscrowStatus
            })
            .FirstOrDefaultAsync();

        if (payment == null)
            return NotFound("Pago no encontrado");

        return Ok(payment);
    }
}
