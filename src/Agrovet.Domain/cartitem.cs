namespace Agrovet.Domain;

public class CartItem
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product? Product { get; set; }
}