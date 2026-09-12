using Marketplace.API.Data;
using Marketplace.API.Models;
using Microsoft.AspNetCore.Authorization;
using Marketplace.API.DTOs.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // GET ALL USERS
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // =========================
    // GET BY ID
    // =========================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("Usuario no encontrado");

        return Ok(user);
    }

    // =========================
    // CREATE USER (🔥 FALTABA ESTO)
    // =========================
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email);

        if (exists)
            return BadRequest("El email ya existe");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Role = dto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role
        });
    }
    // =========================
    // UPDATE USER
    // =========================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] User dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("Usuario no encontrado");

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Role = dto.Role;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role
        });
    }

    // =========================
    // DELETE USER
    // =========================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound("Usuario no encontrado");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Usuario eliminado correctamente" });
    }
}