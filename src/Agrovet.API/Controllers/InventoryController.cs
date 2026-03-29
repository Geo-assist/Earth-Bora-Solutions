using Agrovet.Domain;
using Agrovet.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agrovet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var logs = await _context.InventoryLogs
            .Include(i => i.Product)
            .OrderByDescending(i => i.RecordedAt)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var logs = await _context.InventoryLogs
            .Include(i => i.Product)
            .Where(i => i.ProductId == productId)
            .OrderByDescending(i => i.RecordedAt)
            .ToListAsync();
        return Ok(logs);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _context.Products
            .Where(p => p.StockQuantity <= p.LowStockThreshold && p.IsActive)
            .ToListAsync();
        return Ok(products);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock(InventoryLog log)
    {
        var product = await _context.Products.FindAsync(log.ProductId);
        if (product == null) return NotFound();

        product.StockQuantity += log.QuantityChange;
        log.Id = Guid.NewGuid();
        log.StockAfter = product.StockQuantity;
        log.RecordedAt = DateTime.UtcNow;

        _context.InventoryLogs.Add(log);
        await _context.SaveChangesAsync();
        return Ok(log);
    }
}