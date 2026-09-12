using Marketplace.API.Data;
using Marketplace.API.DTOs.Products;
using Marketplace.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // ========================
    // GET ALL (CLEAN & SAFE)
    // ========================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.Products
            .AsNoTracking();

        // SELLER solamente puede consultar los productos
        // pertenecientes a su propia tienda.
        if (User.IsInRole("SELLER"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Token inválido");

            query = query.Where(p =>
                p.Store != null &&
                p.Store.UserId == userId
            );
        }

        var products = await query
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                p.ImageUrl,
                p.Status,
                p.CreatedAt,

                Store = p.Store != null ? p.Store.Name : "Sin tienda",
                Category = p.Category != null ? p.Category.Name : "Sin categoría"
            })
            .ToListAsync();

        return Ok(products);
    }

    // ========================
    // CREATE PRODUCT
    // ========================
    [Authorize(Roles = "SELLER,ADMIN")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized("Token inválido");

        var userId = int.Parse(userIdClaim);

        var store = await _context.Stores
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (store == null)
            return BadRequest("No tienes tienda registrada");

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
            return BadRequest("La categoría no existe");

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock,
            StoreId = store.Id,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            Status = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // ========================
    // UPDATE PRODUCT
    // ========================
    [Authorize(Roles = "SELLER,ADMIN")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound("Producto no encontrado");

        // ADMIN puede modificar cualquier producto.
        // SELLER solamente puede modificar productos de su propia tienda.
        if (User.IsInRole("SELLER"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Token inválido");

            var userId = int.Parse(userIdClaim);

            var ownsProduct = await _context.Stores
                .AnyAsync(s => s.Id == product.StoreId && s.UserId == userId);

            if (!ownsProduct)
                return Forbid();
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
            return BadRequest("La categoría no existe");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.CategoryId = dto.CategoryId;
        product.ImageUrl = dto.ImageUrl;

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // ========================
    // DELETE PRODUCT
    // ========================
    [Authorize(Roles = "SELLER,ADMIN")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
            return NotFound("Producto no encontrado");

        // ADMIN puede eliminar cualquier producto.
        // SELLER solamente puede eliminar productos de su propia tienda.
        if (User.IsInRole("SELLER"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("Token inválido");

            var userId = int.Parse(userIdClaim);

            var ownsProduct = await _context.Stores
                .AnyAsync(s => s.Id == product.StoreId && s.UserId == userId);

            if (!ownsProduct)
                return Forbid();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Producto eliminado correctamente" });
    }
}