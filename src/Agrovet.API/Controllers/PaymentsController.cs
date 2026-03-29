using Agrovet.Domain;
using Agrovet.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Agrovet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _context.Payments
            .Include(p => p.Order)
            .ToListAsync();
        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null) return NotFound();
        return Ok(payment);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Payment payment)
    {
        payment.Id = Guid.NewGuid();
        payment.Status = PaymentStatus.Pending;

        _context.Payments.Add(payment);

        // Update order payment status
        var order = await _context.Orders.FindAsync(payment.OrderId);
        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Pending;
        }

        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] string transactionReference)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null) return NotFound();

        payment.Status = PaymentStatus.Paid;
        payment.TransactionReference = transactionReference;
        payment.PaidAt = DateTime.UtcNow;

        // Update order payment status
        var order = await _context.Orders.FindAsync(payment.OrderId);
        if (order != null)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Confirmed;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}