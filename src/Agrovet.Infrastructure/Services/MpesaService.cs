using System.Net.Http.Headers;
using System.Text;
using Agrovet.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Agrovet.Infrastructure.Services;

public class MpesaService
{
    private readonly MpesaSettings _settings;
    private readonly HttpClient _httpClient;

    public MpesaService(IOptions<MpesaSettings> settings)
    {
        _settings = settings.Value;
        _httpClient = new HttpClient();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ConsumerKey}:{_settings.ConsumerSecret}"));

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        var url = _settings.Environment == "sandbox"
            ? "https://sandbox.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials"
            : "https://api.safaricom.co.ke/oauth/v1/generate?grant_type=client_credentials";

        var response = await _httpClient.GetStringAsync(url);
        var token = JsonConvert.DeserializeObject<dynamic>(response);
        return token?.access_token ?? string.Empty;
    }

    public async Task<string> InitiateStkPushAsync(string phoneNumber, decimal amount, string orderReference)
    {
        var accessToken = await GetAccessTokenAsync();
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var password = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ShortCode}{_settings.Passkey}{timestamp}"));

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var url = _settings.Environment == "sandbox"
            ? "https://sandbox.safaricom.co.ke/mpesa/stkpush/v1/processrequest"
            : "https://api.safaricom.co.ke/mpesa/stkpush/v1/processrequest";

        var payload = new
        {
            BusinessShortCode = _settings.ShortCode,
            Password = password,
            Timestamp = timestamp,
            TransactionType = "CustomerPayBillOnline",
            Amount = (int)amount,
            PartyA = phoneNumber,
            PartyB = _settings.ShortCode,
            PhoneNumber = phoneNumber,
            CallBackURL = _settings.CallbackUrl,
            AccountReference = orderReference,
            TransactionDesc = "Earth Bora Solutions Order Payment"
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }
}