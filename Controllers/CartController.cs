using Marketplace.API.Data;
using Marketplace.API.DTOs;
using Marketplace.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    // 🛒 VER CARRITO
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var cart = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.ProductId,
                Product = c.Product.Name,
                c.Quantity,
                Price = c.Product.Price,
                Total = c.Product.Price * c.Quantity
            })
            .ToListAsync();

        return Ok(cart);
    }

    // ➕ AGREGAR AL CARRITO
    [HttpPost]
    public async Task<IActionResult> AddToCart(AddToCartDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
            return NotFound("Producto no existe");

        var existing = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId);

        if (existing != null)
        {
            existing.Quantity += dto.Quantity;
        }
        else
        {
            var item = new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            };

            _context.CartItems.Add(item);
        }

        await _context.SaveChangesAsync();
        return Ok("Producto agregado al carrito");
    }

    // ✏️ ACTUALIZAR CANTIDAD
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCartDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item == null)
            return NotFound();

        item.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    // ❌ ELIMINAR ITEM
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var item = await _context.CartItems
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (item == null)
            return NotFound();

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return Ok("Eliminado del carrito");
    }
}