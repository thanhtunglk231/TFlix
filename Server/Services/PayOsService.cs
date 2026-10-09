using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Server.Services;

public interface IPayOsService
{
    Task<PayOsPaymentLink> CreatePaymentLinkAsync(long orderCode, int amount, string description, CancellationToken cancellationToken);
    Task<PayOsPaymentStatus> GetPaymentAsync(long orderCode, CancellationToken cancellationToken);
    bool VerifyWebhook(JsonElement data, string signature);
}

public sealed record PayOsPaymentLink(string CheckoutUrl, string QrCode);
public sealed record PayOsPaymentStatus(string Status, decimal AmountPaid);

public sealed class PayOsService : IPayOsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PayOsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri("https://api-merchant.payos.vn");
    }

    public async Task<PayOsPaymentLink> CreatePaymentLinkAsync(long orderCode, int amount, string description, CancellationToken cancellationToken)
    {
        var clientId = Required("PayOS:ClientId");
        var apiKey = Required("PayOS:ApiKey");
        var checksumKey = Required("PayOS:ChecksumKey");
        var returnUrl = Required("PayOS:ReturnUrl");
        var cancelUrl = Required("PayOS:CancelUrl");
        var signatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests");
        request.Headers.Add("x-client-id", clientId);
        request.Headers.Add("x-api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            orderCode, amount, description, cancelUrl, returnUrl,
            expiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds(),
            signature = Hmac(signatureData, checksumKey)
        });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.GetProperty("code").GetString() != "00")
            throw new InvalidOperationException("PayOS từ chối yêu cầu tạo mã thanh toán.");
        var data = document.RootElement.GetProperty("data");
        return new PayOsPaymentLink(data.GetProperty("checkoutUrl").GetString() ?? "", data.GetProperty("qrCode").GetString() ?? "");
    }

    public bool VerifyWebhook(JsonElement data, string signature)
    {
        var canonical = string.Join("&", data.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => $"{x.Name}={Value(x.Value)}"));
        var expected = Hmac(canonical, Required("PayOS:ChecksumKey"));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }

    public async Task<PayOsPaymentStatus> GetPaymentAsync(long orderCode, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/v2/payment-requests/{orderCode}");
        request.Headers.Add("x-client-id", Required("PayOS:ClientId"));
        request.Headers.Add("x-api-key", Required("PayOS:ApiKey"));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var data = document.RootElement.GetProperty("data");
        return new PayOsPaymentStatus(data.GetProperty("status").GetString() ?? "", data.GetProperty("amountPaid").GetDecimal());
    }

    private string Required(string key) => _configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Thiếu cấu hình {key}.");
    private static string Hmac(string data, string key) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    private static string Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Null => "",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Number => value.GetRawText(),
        _ => value.GetRawText()
    };
}
