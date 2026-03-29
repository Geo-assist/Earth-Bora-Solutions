namespace Agrovet.Domain;

public class InventoryLog
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product? Product { get; set; }
}