namespace Agrovet.Infrastructure.Settings;

public class MpesaSettings
{
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string Passkey { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox";
}