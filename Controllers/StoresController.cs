using Marketplace.API.Data;
using Marketplace.API.Models;
using Marketplace.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public StoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stores = await _context.Stores
            .Include(s => s.User)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                Owner = s.User.Name
            })
            .ToListAsync();

        return Ok(stores);
    }

    [Authorize(Roles = "SELLER")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateStoreDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var exists = await _context.Stores.AnyAsync(s => s.UserId == userId);
        if (exists) return BadRequest("Ya tienes una tienda");

        var store = new Store
        {
            Name = dto.Name,
            Description = dto.Description,
            UserId = userId
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        return Ok(store);
    }

    [Authorize(Roles = "SELLER")]
    [HttpGet("my")]
    public async Task<IActionResult> MyStores()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var stores = await _context.Stores
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return Ok(stores);
    }
}