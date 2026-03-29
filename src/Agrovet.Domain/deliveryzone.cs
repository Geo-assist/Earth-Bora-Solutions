namespace Agrovet.Domain;

public class DeliveryZone
{
    public Guid Id { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public int EstimatedDays { get; set; }
}