using System.Net.Http;
using System.Text.Json;
using Chummer.Contracts.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Chummer.Session.Web;

public sealed class BrowserSessionCoachApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiBaseUrl;

    public BrowserSessionCoachApiClient(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _apiBaseUrl = NormalizeApiBaseUrl(configuration["Chummer:ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("CHUMMER_SESSION_API_BASE_URL"));
    }

    public Task<BrowserSessionCoachApiCallResult<AiGatewayStatusProjection>> GetStatusAsync(CancellationToken ct = default)
        => SendAsync<AiGatewayStatusProjection>(HttpMethod.Get, "/api/ai/status", payload: null, ct);

    public Task<BrowserSessionCoachApiCallResult<AiProviderHealthProjection[]>> ListProviderHealthAsync(string? routeType = null, CancellationToken ct = default)
        => SendAsync<AiProviderHealthProjection[]>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/provider-health",
                ("routeType", routeType)),
            payload: null,
            ct);

    public Task<BrowserSessionCoachApiCallResult<AiConversationAuditCatalogPage>> ListConversationAuditsAsync(
        string routeType,
        string? characterId = null,
        string? runtimeFingerprint = null,
        int maxCount = 3,
        CancellationToken ct = default)
        => SendAsync<AiConversationAuditCatalogPage>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/conversation-audits",
                ("routeType", routeType),
                ("characterId", characterId),
                ("runtimeFingerprint", runtimeFingerprint),
                ("maxCount", maxCount.ToString())),
            payload: null,
            ct);

    private async Task<BrowserSessionCoachApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken ct)
    {
        try
        {
            string rawEnvelope = await _jsRuntime.InvokeAsync<string>(
                "chummerSessionApi.send",
                ct,
                BuildRequestPath(path),
                method.Method,
                payload);

            BrowserSessionCoachFetchEnvelope? envelope = JsonSerializer.Deserialize<BrowserSessionCoachFetchEnvelope>(rawEnvelope, JsonOptions);
            if (envelope is null)
            {
                return BrowserSessionCoachApiCallResult<T>.Failure(0, $"Coach request '{path}' returned an unreadable browser envelope.");
            }

            string responseText = envelope.Text ?? string.Empty;
            if (envelope.Status == StatusCodes.Status501NotImplemented)
            {
                AiNotImplementedReceipt? receipt = DeserializePayload<AiNotImplementedReceipt>(responseText);
                if (receipt is null)
                {
                    return BrowserSessionCoachApiCallResult<T>.Failure(
                        envelope.Status,
                        $"Coach request '{path}' returned HTTP 501 without an AI receipt.");
                }

                return BrowserSessionCoachApiCallResult<T>.FromNotImplemented(envelope.Status, receipt);
            }

            if (envelope.Status == StatusCodes.Status429TooManyRequests)
            {
                AiQuotaExceededReceipt? receipt = DeserializePayload<AiQuotaExceededReceipt>(responseText);
                if (receipt is null)
                {
                    return BrowserSessionCoachApiCallResult<T>.Failure(
                        envelope.Status,
                        $"Coach request '{path}' returned HTTP 429 without an AI quota receipt.");
                }

                return BrowserSessionCoachApiCallResult<T>.FromQuotaExceeded(envelope.Status, receipt);
            }

            if (envelope.Status < StatusCodes.Status200OK || envelope.Status >= StatusCodes.Status300MultipleChoices)
            {
                return BrowserSessionCoachApiCallResult<T>.Failure(
                    envelope.Status,
                    ExtractErrorMessage(responseText) ?? $"Coach request '{path}' failed with HTTP {envelope.Status}.");
            }

            T? typedPayload = DeserializePayload<T>(responseText);
            if (typedPayload is null)
            {
                return BrowserSessionCoachApiCallResult<T>.Failure(
                    envelope.Status,
                    $"Coach request '{path}' returned an empty payload.");
            }

            return BrowserSessionCoachApiCallResult<T>.Success(envelope.Status, typedPayload);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException or JsonException)
        {
            return BrowserSessionCoachApiCallResult<T>.Failure(0, $"Coach request '{path}' failed in the browser head: {ex.Message}");
        }
    }

    private string BuildRequestPath(string relativePath)
        => string.IsNullOrEmpty(_apiBaseUrl)
            ? relativePath
            : $"{_apiBaseUrl}{relativePath}";

    private static T? DeserializePayload<T>(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(responseText, JsonOptions);
    }

    private static string NormalizeApiBaseUrl(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return rawValue.Trim().TrimEnd('/');
    }

    private static string AppendQuery(string path, params (string Key, string? Value)[] pairs)
    {
        List<string> encoded = [];
        foreach ((string key, string? value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            encoded.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        return encoded.Count == 0
            ? path
            : $"{path}?{string.Join("&", encoded)}";
    }

    private static string? ExtractErrorMessage(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseText);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return responseText;
            }

            if (root.TryGetProperty("message", out JsonElement message))
            {
                return message.GetString();
            }

            if (root.TryGetProperty("error", out JsonElement error))
            {
                return error.GetString();
            }
        }
        catch (JsonException)
        {
            return responseText;
        }

        return responseText;
    }
}

public sealed record BrowserSessionCoachApiCallResult<T>(
    bool IsSuccess,
    bool IsImplemented,
    int StatusCode,
    T? Payload,
    string? ErrorMessage = null,
    AiNotImplementedReceipt? NotImplemented = null,
    AiQuotaExceededReceipt? QuotaExceeded = null)
{
    public static BrowserSessionCoachApiCallResult<T> Success(int statusCode, T payload)
        => new(true, true, statusCode, payload);

    public static BrowserSessionCoachApiCallResult<T> Failure(int statusCode, string message)
        => new(false, true, statusCode, default, message);

    public static BrowserSessionCoachApiCallResult<T> FromNotImplemented(int statusCode, AiNotImplementedReceipt receipt)
        => new(false, false, statusCode, default, receipt.Message, receipt);

    public static BrowserSessionCoachApiCallResult<T> FromQuotaExceeded(int statusCode, AiQuotaExceededReceipt receipt)
        => new(false, true, statusCode, default, receipt.Message, null, receipt);
}

public sealed record BrowserSessionCoachFetchEnvelope(int Status, string? Text);
