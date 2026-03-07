using System.Net.Http;
using System.Text.Json;
using Chummer.Contracts.AI;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;

namespace Chummer.Coach.Web;

public sealed class BrowserCoachApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiBaseUrl;

    public BrowserCoachApiClient(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _apiBaseUrl = NormalizeApiBaseUrl(configuration["Chummer:ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("CHUMMER_COACH_API_BASE_URL"));
    }

    public Task<BrowserCoachApiCallResult<AiGatewayStatusProjection>> GetStatusAsync(CancellationToken ct = default)
        => SendAsync<AiGatewayStatusProjection>(HttpMethod.Get, "/api/ai/status", payload: null, ct);

    public Task<BrowserCoachApiCallResult<AiProviderHealthProjection[]>> ListProviderHealthAsync(
        string? routeType = null,
        string? providerId = null,
        CancellationToken ct = default)
        => SendAsync<AiProviderHealthProjection[]>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/provider-health",
                ("routeType", routeType),
                ("providerId", providerId)),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiPromptCatalog>> ListPromptsAsync(
        string routeType,
        string? personaId = null,
        int maxCount = 6,
        CancellationToken ct = default)
        => SendAsync<AiPromptCatalog>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/prompts",
                ("routeType", routeType),
                ("personaId", personaId),
                ("maxCount", maxCount.ToString())),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiBuildIdeaCatalog>> ListBuildIdeasAsync(
        string routeType,
        string queryText,
        string? rulesetId,
        int maxCount = 6,
        CancellationToken ct = default)
        => SendAsync<AiBuildIdeaCatalog>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/build-ideas",
                ("routeType", routeType),
                ("queryText", queryText),
                ("rulesetId", rulesetId),
                ("maxCount", maxCount.ToString())),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiConversationCatalogPage>> ListConversationsAsync(
        string routeType,
        string? characterId = null,
        string? runtimeFingerprint = null,
        string? workspaceId = null,
        int maxCount = 6,
        CancellationToken ct = default)
        => SendAsync<AiConversationCatalogPage>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/conversations",
                ("routeType", routeType),
                ("characterId", characterId),
                ("runtimeFingerprint", runtimeFingerprint),
                ("workspaceId", workspaceId),
                ("maxCount", maxCount.ToString())),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiConversationAuditCatalogPage>> ListConversationAuditsAsync(
        string routeType,
        string? characterId = null,
        string? runtimeFingerprint = null,
        string? workspaceId = null,
        int maxCount = 6,
        CancellationToken ct = default)
        => SendAsync<AiConversationAuditCatalogPage>(
            HttpMethod.Get,
            AppendQuery(
                "/api/ai/conversation-audits",
                ("routeType", routeType),
                ("characterId", characterId),
                ("runtimeFingerprint", runtimeFingerprint),
                ("workspaceId", workspaceId),
                ("maxCount", maxCount.ToString())),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiConversationSnapshot>> GetConversationAsync(
        string conversationId,
        CancellationToken ct = default)
        => SendAsync<AiConversationSnapshot>(
            HttpMethod.Get,
            $"/api/ai/conversations/{Uri.EscapeDataString(conversationId)}",
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiRuntimeSummaryProjection>> GetRuntimeSummaryAsync(
        string runtimeFingerprint,
        string? rulesetId = null,
        CancellationToken ct = default)
        => SendAsync<AiRuntimeSummaryProjection>(
            HttpMethod.Get,
            AppendQuery(
                $"/api/ai/runtime/{Uri.EscapeDataString(runtimeFingerprint)}/summary",
                ("rulesetId", rulesetId)),
            payload: null,
            ct);

    public Task<BrowserCoachApiCallResult<AiConversationTurnPreview>> PreviewCoachTurnAsync(
        AiConversationTurnRequest request,
        CancellationToken ct = default)
        => PreviewTurnAsync(AiRouteTypes.Coach, request, ct);

    public Task<BrowserCoachApiCallResult<AiConversationTurnPreview>> PreviewTurnAsync(
        string routeType,
        AiConversationTurnRequest request,
        CancellationToken ct = default)
        => SendAsync<AiConversationTurnPreview>(
            HttpMethod.Post,
            $"/api/ai/preview/{Uri.EscapeDataString(routeType)}",
            request,
            ct);

    public Task<BrowserCoachApiCallResult<AiConversationTurnResponse>> SendCoachTurnAsync(
        AiConversationTurnRequest request,
        CancellationToken ct = default)
        => SendTurnAsync(AiRouteTypes.Coach, request, ct);

    public Task<BrowserCoachApiCallResult<AiConversationTurnResponse>> SendTurnAsync(
        string routeType,
        AiConversationTurnRequest request,
        CancellationToken ct = default)
        => SendAsync<AiConversationTurnResponse>(
            HttpMethod.Post,
            ResolveTurnPath(routeType),
            request,
            ct);

    public Task<BrowserCoachApiCallResult<AiActionPreviewReceipt>> PreviewKarmaSpendAsync(
        AiSpendPlanPreviewRequest request,
        CancellationToken ct = default)
        => SendAsync<AiActionPreviewReceipt>(
            HttpMethod.Post,
            "/api/ai/preview/karma-spend",
            request,
            ct);

    public Task<BrowserCoachApiCallResult<AiActionPreviewReceipt>> PreviewNuyenSpendAsync(
        AiSpendPlanPreviewRequest request,
        CancellationToken ct = default)
        => SendAsync<AiActionPreviewReceipt>(
            HttpMethod.Post,
            "/api/ai/preview/nuyen-spend",
            request,
            ct);

    public Task<BrowserCoachApiCallResult<AiActionPreviewReceipt>> CreateApplyPreviewAsync(
        AiApplyPreviewRequest request,
        CancellationToken ct = default)
        => SendAsync<AiActionPreviewReceipt>(
            HttpMethod.Post,
            "/api/ai/apply-preview",
            request,
            ct);

    private async Task<BrowserCoachApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken ct)
    {
        try
        {
            string rawEnvelope = await _jsRuntime.InvokeAsync<string>(
                "chummerCoachApi.send",
                ct,
                BuildRequestPath(path),
                method.Method,
                payload);

            BrowserCoachFetchEnvelope? envelope = JsonSerializer.Deserialize<BrowserCoachFetchEnvelope>(rawEnvelope, JsonOptions);
            if (envelope is null)
            {
                return BrowserCoachApiCallResult<T>.Failure(0, $"Coach request '{path}' returned an unreadable browser envelope.");
            }

            string responseText = envelope.Text ?? string.Empty;
            if (envelope.Status == StatusCodes.Status501NotImplemented)
            {
                AiNotImplementedReceipt? receipt = DeserializePayload<AiNotImplementedReceipt>(responseText);
                if (receipt is null)
                {
                    return BrowserCoachApiCallResult<T>.Failure(
                        envelope.Status,
                        $"Coach request '{path}' returned HTTP 501 without an AI receipt.");
                }

                return BrowserCoachApiCallResult<T>.FromNotImplemented(envelope.Status, receipt);
            }

            if (envelope.Status == StatusCodes.Status429TooManyRequests)
            {
                AiQuotaExceededReceipt? receipt = DeserializePayload<AiQuotaExceededReceipt>(responseText);
                if (receipt is null)
                {
                    return BrowserCoachApiCallResult<T>.Failure(
                        envelope.Status,
                        $"Coach request '{path}' returned HTTP 429 without an AI quota receipt.");
                }

                return BrowserCoachApiCallResult<T>.FromQuotaExceeded(envelope.Status, receipt);
            }

            if (envelope.Status < StatusCodes.Status200OK || envelope.Status >= StatusCodes.Status300MultipleChoices)
            {
                return BrowserCoachApiCallResult<T>.Failure(
                    envelope.Status,
                    ExtractErrorMessage(responseText) ?? $"Coach request '{path}' failed with HTTP {envelope.Status}.");
            }

            T? typedPayload = DeserializePayload<T>(responseText);
            if (typedPayload is null)
            {
                return BrowserCoachApiCallResult<T>.Failure(
                    envelope.Status,
                    $"Coach request '{path}' returned an empty payload.");
            }

            return BrowserCoachApiCallResult<T>.Success(envelope.Status, typedPayload);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException or JsonException)
        {
            return BrowserCoachApiCallResult<T>.Failure(0, $"Coach request '{path}' failed in the browser head: {ex.Message}");
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

    private static string ResolveTurnPath(string routeType)
        => routeType switch
        {
            AiRouteTypes.Chat => "/api/ai/chat",
            AiRouteTypes.Build => "/api/ai/build-lab/query",
            AiRouteTypes.Docs => "/api/ai/docs/query",
            AiRouteTypes.Recap => "/api/ai/session/recap",
            _ => "/api/ai/coach"
        };

    private sealed record BrowserCoachFetchEnvelope(
        int Status,
        string? Text);
}

public sealed record BrowserCoachApiCallResult<T>(
    int StatusCode,
    T? Payload = default,
    AiNotImplementedReceipt? NotImplemented = null,
    AiQuotaExceededReceipt? QuotaExceeded = null,
    string? ErrorMessage = null)
{
    public bool IsImplemented => NotImplemented is null;

    public bool IsSuccess => ErrorMessage is null && NotImplemented is null && QuotaExceeded is null;

    public static BrowserCoachApiCallResult<T> Success(int statusCode, T payload)
        => new(statusCode, payload);

    public static BrowserCoachApiCallResult<T> FromNotImplemented(int statusCode, AiNotImplementedReceipt receipt)
        => new(statusCode, default, receipt, null, null);

    public static BrowserCoachApiCallResult<T> FromQuotaExceeded(int statusCode, AiQuotaExceededReceipt receipt)
        => new(statusCode, default, null, receipt, null);

    public static BrowserCoachApiCallResult<T> Failure(int statusCode, string errorMessage)
        => new(statusCode, default, null, null, errorMessage);
}
