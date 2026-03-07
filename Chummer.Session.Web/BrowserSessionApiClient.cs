using System.Net.Http;
using System.Text.Json;
using Chummer.Contracts.Content;
using Chummer.Contracts.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Chummer.Session.Web;

public sealed class BrowserSessionApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiBaseUrl;

    public BrowserSessionApiClient(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _apiBaseUrl = NormalizeApiBaseUrl(configuration["Chummer:ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("CHUMMER_SESSION_API_BASE_URL"));
    }

    public Task<BrowserSessionApiCallResult<SessionProfileCatalog>> ListProfilesAsync(CancellationToken ct = default)
        => SendAsync<SessionProfileCatalog>(HttpMethod.Get, "/api/session/profiles", payload: null, ct);

    public Task<BrowserSessionApiCallResult<SessionCharacterCatalog>> ListCharactersAsync(CancellationToken ct = default)
        => SendAsync<SessionCharacterCatalog>(HttpMethod.Get, "/api/session/characters", payload: null, ct);

    public Task<BrowserSessionApiCallResult<SessionRuntimeStatusProjection>> GetRuntimeStateAsync(string characterId, CancellationToken ct = default)
        => SendAsync<SessionRuntimeStatusProjection>(
            HttpMethod.Get,
            $"/api/session/characters/{Uri.EscapeDataString(characterId)}/runtime-state",
            payload: null,
            ct);

    public Task<BrowserSessionApiCallResult<SessionRuntimeBundleIssueReceipt>> GetRuntimeBundleAsync(string characterId, CancellationToken ct = default)
        => SendAsync<SessionRuntimeBundleIssueReceipt>(
            HttpMethod.Get,
            $"/api/session/characters/{Uri.EscapeDataString(characterId)}/runtime-bundle",
            payload: null,
            ct);

    public Task<BrowserSessionApiCallResult<SessionRuntimeBundleRefreshReceipt>> RefreshRuntimeBundleAsync(string characterId, CancellationToken ct = default)
        => SendAsync<SessionRuntimeBundleRefreshReceipt>(
            HttpMethod.Post,
            $"/api/session/characters/{Uri.EscapeDataString(characterId)}/runtime-bundle/refresh",
            payload: null,
            ct);

    public Task<BrowserSessionApiCallResult<SessionProfileSelectionReceipt>> SelectProfileAsync(
        string characterId,
        SessionProfileSelectionRequest request,
        CancellationToken ct = default)
        => SendAsync<SessionProfileSelectionReceipt>(
            HttpMethod.Post,
            $"/api/session/characters/{Uri.EscapeDataString(characterId)}/profile",
            request,
            ct);

    public Task<BrowserSessionApiCallResult<RulePackCatalog>> ListRulePacksAsync(CancellationToken ct = default)
        => SendAsync<RulePackCatalog>(HttpMethod.Get, "/api/session/rulepacks", payload: null, ct);

    private async Task<BrowserSessionApiCallResult<T>> SendAsync<T>(
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

            BrowserSessionFetchEnvelope? envelope = JsonSerializer.Deserialize<BrowserSessionFetchEnvelope>(rawEnvelope, JsonOptions);
            if (envelope is null)
            {
                return BrowserSessionApiCallResult<T>.Failure(0, $"Session request '{path}' returned an unreadable browser envelope.");
            }

            string responseText = envelope.Text ?? string.Empty;
            if (envelope.Status == StatusCodes.Status501NotImplemented)
            {
                SessionNotImplementedReceipt? receipt = DeserializePayload<SessionNotImplementedReceipt>(responseText);
                if (receipt is null)
                {
                    return BrowserSessionApiCallResult<T>.Failure(
                        envelope.Status,
                        $"Session request '{path}' returned HTTP 501 without a session receipt.");
                }

                return BrowserSessionApiCallResult<T>.FromNotImplemented(envelope.Status, receipt);
            }

            if (envelope.Status < StatusCodes.Status200OK || envelope.Status >= StatusCodes.Status300MultipleChoices)
            {
                return BrowserSessionApiCallResult<T>.Failure(
                    envelope.Status,
                    ExtractErrorMessage(responseText)
                    ?? $"Session request '{path}' failed with HTTP {envelope.Status}.");
            }

            T? typedPayload = DeserializePayload<T>(responseText);
            if (typedPayload is null)
            {
                return BrowserSessionApiCallResult<T>.Failure(
                    envelope.Status,
                    $"Session request '{path}' returned an empty payload.");
            }

            return BrowserSessionApiCallResult<T>.Success(envelope.Status, typedPayload);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException or JsonException)
        {
            return BrowserSessionApiCallResult<T>.Failure(0, $"Session request '{path}' failed in the browser head: {ex.Message}");
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

    private sealed record BrowserSessionFetchEnvelope(
        int Status,
        string? Text);
}

public sealed record BrowserSessionApiCallResult<T>(
    int StatusCode,
    T? Payload = default,
    SessionNotImplementedReceipt? NotImplemented = null,
    string? ErrorMessage = null)
{
    public bool IsImplemented => NotImplemented is null;

    public bool IsSuccess => ErrorMessage is null && NotImplemented is null;

    public static BrowserSessionApiCallResult<T> Success(int statusCode, T payload)
        => new(statusCode, payload);

    public static BrowserSessionApiCallResult<T> FromNotImplemented(int statusCode, SessionNotImplementedReceipt receipt)
        => new(statusCode, default, receipt);

    public static BrowserSessionApiCallResult<T> Failure(int statusCode, string message)
        => new(statusCode, default, null, message);
}
