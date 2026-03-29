using Agrovet.Domain;
using Agrovet.Infrastructure;
using Agrovet.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agrovet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SmsService _smsService;

    public OrdersController(AppDbContext context, SmsService smsService)
    {
        _context = context;
        _smsService = smsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.Payment)
            .Select(o => new
            {
                o.Id,
                o.TotalAmount,
                o.Status,
                o.PaymentStatus,
                o.OrderedAt,
                UserName = o.User != null ? o.User.FullName : "Unknown",
                UserEmail = o.User != null ? o.User.Email : "",
                UserPhone = o.User != null ? o.User.PhoneNumber : "",
                OrderItems = o.OrderItems.Select(i => new
                {
                    i.Id,
                    i.Quantity,
                    i.UnitPrice,
                    i.Subtotal,
                    ProductName = i.Product != null ? i.Product.Name : "Unknown"
                })
            })
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.Payment)
            .Where(o => o.UserId == userId)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Order order)
    {
        order.Id = Guid.NewGuid();
        order.OrderedAt = DateTime.UtcNow;
        order.Status = OrderStatus.Pending;
        order.PaymentStatus = PaymentStatus.Unpaid;

        foreach (var item in order.OrderItems)
        {
            item.Id = Guid.NewGuid();
            item.Subtotal = item.Quantity * item.UnitPrice;
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
                _context.InventoryLogs.Add(new InventoryLog
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.ProductId,
                    ChangeType = "Sale",
                    QuantityChange = -item.Quantity,
                    StockAfter = product.StockQuantity,
                    RecordedAt = DateTime.UtcNow
                });
            }
        }

        order.TotalAmount = order.OrderItems.Sum(i => i.Subtotal);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Send order confirmation SMS
        var user = await _context.AppUsers.FindAsync(order.UserId);
        if (user != null)
        {
            await _smsService.SendOrderConfirmationAsync(
                user.PhoneNumber,
                order.Id.ToString().Substring(0, 8).ToUpper(),
                order.TotalAmount);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            order.Status = parsedStatus;
            await _context.SaveChangesAsync();

            // Send SMS notification to customer
            if (order.User != null)
            {
                await _smsService.SendOrderStatusUpdateAsync(
                    order.User.PhoneNumber,
                    order.Id.ToString().Substring(0, 8).ToUpper(),
                    status);
            }

            return NoContent();
        }

        return BadRequest(new { message = "Invalid status value" });
    }
}
