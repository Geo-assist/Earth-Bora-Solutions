using Agrovet.Infrastructure;
using Agrovet.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Agrovet.Domain;

namespace Agrovet.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MpesaController : ControllerBase
{
    private readonly MpesaService _mpesaService;
    private readonly SmsService _smsService;
    private readonly AppDbContext _context;

    public MpesaController(MpesaService mpesaService, SmsService smsService, AppDbContext context)
    {
        _mpesaService = mpesaService;
        _smsService = smsService;
        _context = context;
    }

    // POST: api/mpesa/pay
    [HttpPost("pay")]
    public async Task<IActionResult> InitiatePayment([FromBody] MpesaPaymentRequest request)
    {
        var response = await _mpesaService.InitiateStkPushAsync(
            request.PhoneNumber,
            request.Amount,
            request.OrderReference);

        return Ok(response);
    }

    // POST: api/mpesa/callback
    [HttpPost("callback")]
    public async Task<IActionResult> MpesaCallback([FromBody] dynamic callbackData)
    {
        try
        {
            string resultCode = callbackData?.Body?.stkCallback?.ResultCode?.ToString() ?? "";
            string checkoutRequestId = callbackData?.Body?.stkCallback?.CheckoutRequestID?.ToString() ?? "";
            string transactionRef = callbackData?.Body?.stkCallback?.CallbackMetadata?.Item?[1]?.Value?.ToString() ?? "";

            if (resultCode == "0")
            {
                // Payment successful — find and update the order
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionReference == checkoutRequestId);

                if (payment != null)
                {
                    payment.Status = Agrovet.Domain.PaymentStatus.Paid;
                    payment.TransactionReference = transactionRef;
                    payment.PaidAt = DateTime.UtcNow;

                    var order = await _context.Orders
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.Id == payment.OrderId);

                    if (order != null)
                    {
                        order.PaymentStatus = Agrovet.Domain.PaymentStatus.Paid;
                        order.Status = Agrovet.Domain.OrderStatus.Confirmed;

                        await _smsService.SendPaymentConfirmationAsync(
                            order.User.PhoneNumber,
                            transactionRef,
                            payment.Amount);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Callback error: {ex.Message}");
            return Ok();
        }
    }
}

public class MpesaPaymentRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderReference { get; set; } = string.Empty;
}