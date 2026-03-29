using System.Net.Http.Headers;
using System.Text;
using Agrovet.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace Agrovet.Infrastructure.Services;

public class SmsService
{
    private readonly AfricasTalkingSettings _settings;
    private readonly HttpClient _httpClient;

    public SmsService(IOptions<AfricasTalkingSettings> settings)
    {
        _settings = settings.Value;
        _httpClient = new HttpClient();
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                _settings.ApiKey == "PASTE_YOUR_AFRICASTALKING_API_KEY_HERE")
            {
                Console.WriteLine($"[SMS SIMULATION] To: {phoneNumber} | Message: {message}");
                return true;
            }

            // Format phone number to international format
            var phone = phoneNumber.Trim();
            if (phone.StartsWith("0"))
                phone = "+254" + phone.Substring(1);
            else if (!phone.StartsWith("+"))
                phone = "+254" + phone;

            var url = _settings.Username == "sandbox"
                ? "https://api.sandbox.africastalking.com/version1/messaging"
                : "https://api.africastalking.com/version1/messaging";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apiKey", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", _settings.Username),
                new KeyValuePair<string, string>("to", phone),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("from", _settings.SenderName)
            });

            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"SMS Response: {result}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SMS Error: {ex.Message}");
            return false;
        }
    }

    public async Task SendOrderConfirmationAsync(string phoneNumber, string orderNumber, decimal amount)
    {
        var message = $"Dear Customer, your order #{orderNumber} worth KSh {amount} has been confirmed. Thank you for shopping with Earth Bora Solutions!";
        await SendSmsAsync(phoneNumber, message);
    }

    public async Task SendPaymentConfirmationAsync(string phoneNumber, string transactionRef, decimal amount)
    {
        var message = $"Earth Bora Solutions: Payment of KSh {amount} received. Ref: {transactionRef}. We will process your order shortly.";
        await SendSmsAsync(phoneNumber, message);
    }

    public async Task SendLowStockAlertAsync(string phoneNumber, string productName, int stockQuantity)
    {
        var message = $"STOCK ALERT: {productName} is running low. Current stock: {stockQuantity} units. Please restock soon. - Earth Bora Solutions";
        await SendSmsAsync(phoneNumber, message);
    }

    public async Task SendOrderStatusUpdateAsync(string phoneNumber, string orderNumber, string status)
    {
        var message = $"Earth Bora Solutions: Your order #{orderNumber} status has been updated to {status}. Thank you for your patience!";
        await SendSmsAsync(phoneNumber, message);
    }
}
