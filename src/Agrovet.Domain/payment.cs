namespace Agrovet.Domain;

public class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }

    // Navigation property
    public Order? Order { get; set; }
}