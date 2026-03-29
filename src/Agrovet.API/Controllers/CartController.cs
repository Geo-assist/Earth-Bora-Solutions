using Agrovet.Domain;
using Agrovet.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agrovet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetCart(string userId)
    {
        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .ToListAsync();
        return Ok(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(CartItem cartItem)
    {
        var existing = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == cartItem.UserId
                && c.ProductId == cartItem.ProductId);

        if (existing != null)
        {
            existing.Quantity += cartItem.Quantity;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        cartItem.Id = Guid.NewGuid();
        cartItem.AddedAt = DateTime.UtcNow;
        _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync();
        return Ok(cartItem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] int quantity)
    {
        var cartItem = await _context.CartItems.FindAsync(id);
        if (cartItem == null) return NotFound();

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromCart(Guid id)
    {
        var cartItem = await _context.CartItems.FindAsync(id);
        if (cartItem == null) return NotFound();
        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("clear/{userId}")]
    public async Task<IActionResult> ClearCart(string userId)
    {
        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToListAsync();
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}