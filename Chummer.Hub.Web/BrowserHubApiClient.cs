using System.Net.Http;
using System.Text.Json;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Presentation;
using Microsoft.JSInterop;

namespace Chummer.Hub.Web;

public sealed class BrowserHubApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;
    private readonly string _apiBaseUrl;

    public BrowserHubApiClient(IJSRuntime jsRuntime, IConfiguration configuration)
    {
        _jsRuntime = jsRuntime;
        _apiBaseUrl = NormalizeApiBaseUrl(configuration["Chummer:ApiBaseUrl"] ?? Environment.GetEnvironmentVariable("CHUMMER_HUB_API_BASE_URL"));
    }

    public Task<HubApiCallResult<HubCatalogResultPage>> SearchAsync(BrowseQuery query, CancellationToken ct = default)
        => SendAsync<HubCatalogResultPage>(HttpMethod.Post, "/api/hub/search", query, ct);

    public Task<HubApiCallResult<HubProjectDetailProjection>> GetProjectDetailAsync(string kind, string itemId, CancellationToken ct = default)
        => SendAsync<HubProjectDetailProjection>(
            HttpMethod.Get,
            $"/api/hub/projects/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(itemId)}",
            payload: null,
            ct);

    public Task<HubApiCallResult<HubProjectCompatibilityMatrix>> GetCompatibilityAsync(string kind, string itemId, CancellationToken ct = default)
        => SendAsync<HubProjectCompatibilityMatrix>(
            HttpMethod.Get,
            $"/api/hub/projects/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(itemId)}/compatibility",
            payload: null,
            ct);

    public Task<HubApiCallResult<HubProjectInstallPreviewReceipt>> GetInstallPreviewAsync(
        string kind,
        string itemId,
        RuleProfileApplyTarget target,
        CancellationToken ct = default)
        => SendAsync<HubProjectInstallPreviewReceipt>(
            HttpMethod.Post,
            $"/api/hub/projects/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(itemId)}/install-preview",
            target,
            ct);

    public Task<HubApiCallResult<HubPublishDraftList>> ListDraftsAsync(
        string? kind = null,
        string? ruleset = null,
        string? state = null,
        CancellationToken ct = default)
        => SendAsync<HubPublishDraftList>(
            HttpMethod.Get,
            AppendQuery(
                "/api/hub/publish/drafts",
                ("kind", kind),
                ("ruleset", ruleset),
                ("state", state)),
            payload: null,
            ct);

    public Task<HubApiCallResult<HubDraftDetailProjection>> GetDraftAsync(string draftId, CancellationToken ct = default)
        => SendAsync<HubDraftDetailProjection>(
            HttpMethod.Get,
            $"/api/hub/publish/drafts/{Uri.EscapeDataString(draftId)}",
            payload: null,
            ct);

    public Task<HubApiCallResult<HubPublishDraftReceipt>> CreateDraftAsync(
        HubPublishDraftRequest request,
        CancellationToken ct = default)
        => SendAsync<HubPublishDraftReceipt>(
            HttpMethod.Post,
            "/api/hub/publish/drafts",
            request,
            ct);

    public Task<HubApiCallResult<HubPublishDraftReceipt>> UpdateDraftAsync(
        string draftId,
        HubUpdateDraftRequest request,
        CancellationToken ct = default)
        => SendAsync<HubPublishDraftReceipt>(
            HttpMethod.Put,
            $"/api/hub/publish/drafts/{Uri.EscapeDataString(draftId)}",
            request,
            ct);

    public Task<HubApiCallResult<HubProjectSubmissionReceipt>> SubmitDraftAsync(
        string kind,
        string itemId,
        string? ruleset,
        HubSubmitProjectRequest request,
        CancellationToken ct = default)
        => SendAsync<HubProjectSubmissionReceipt>(
            HttpMethod.Post,
            AppendQuery(
                $"/api/hub/publish/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(itemId)}/submit",
                ("ruleset", ruleset)),
            request,
            ct);

    public Task<HubApiCallResult<HubPublishDraftReceipt>> ArchiveDraftAsync(string draftId, CancellationToken ct = default)
        => SendAsync<HubPublishDraftReceipt>(
            HttpMethod.Post,
            $"/api/hub/publish/drafts/{Uri.EscapeDataString(draftId)}/archive",
            payload: null,
            ct);

    public Task<HubApiCallResult<bool>> DeleteDraftAsync(string draftId, CancellationToken ct = default)
        => SendAsync<bool>(
            HttpMethod.Delete,
            $"/api/hub/publish/drafts/{Uri.EscapeDataString(draftId)}",
            payload: null,
            ct,
            emptySuccessPayloadFactory: static (_, _) => true);

    public Task<HubApiCallResult<HubModerationQueue>> ListModerationQueueAsync(string? state = null, CancellationToken ct = default)
        => SendAsync<HubModerationQueue>(
            HttpMethod.Get,
            AppendQuery("/api/hub/moderation/queue", ("state", state)),
            payload: null,
            ct);

    public Task<HubApiCallResult<HubModerationDecisionReceipt>> ApproveModerationCaseAsync(
        string caseId,
        HubModerationDecisionRequest request,
        CancellationToken ct = default)
        => SendAsync<HubModerationDecisionReceipt>(
            HttpMethod.Post,
            $"/api/hub/moderation/queue/{Uri.EscapeDataString(caseId)}/approve",
            request,
            ct);

    public Task<HubApiCallResult<HubModerationDecisionReceipt>> RejectModerationCaseAsync(
        string caseId,
        HubModerationDecisionRequest request,
        CancellationToken ct = default)
        => SendAsync<HubModerationDecisionReceipt>(
            HttpMethod.Post,
            $"/api/hub/moderation/queue/{Uri.EscapeDataString(caseId)}/reject",
            request,
            ct);

    private async Task<HubApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? payload,
        CancellationToken ct,
        Func<int, string, T?>? emptySuccessPayloadFactory = null)
    {
        try
        {
            string rawEnvelope = await _jsRuntime.InvokeAsync<string>(
                "chummerHubApi.send",
                ct,
                BuildRequestPath(path),
                method.Method,
                payload);

            HubFetchEnvelope? envelope = JsonSerializer.Deserialize<HubFetchEnvelope>(rawEnvelope, JsonOptions);
            if (envelope is null)
            {
                return HubApiCallResult<T>.Failure(0, $"Hub request '{path}' returned an unreadable browser envelope.");
            }

            string responseText = envelope.Text ?? string.Empty;
            if (envelope.Status < 200 || envelope.Status >= 300)
            {
                return HubApiCallResult<T>.Failure(
                    envelope.Status,
                    ExtractErrorMessage(responseText) ?? $"Hub request '{path}' failed with HTTP {envelope.Status}.");
            }

            T? typedPayload = DeserializePayload<T>(responseText);
            if (typedPayload is null)
            {
                if (emptySuccessPayloadFactory is not null)
                {
                    T? emptySuccessPayload = emptySuccessPayloadFactory(envelope.Status, responseText);
                    if (emptySuccessPayload is not null)
                    {
                        return HubApiCallResult<T>.Success(envelope.Status, emptySuccessPayload);
                    }
                }

                return HubApiCallResult<T>.Failure(
                    envelope.Status,
                    $"Hub request '{path}' returned an empty payload.");
            }

            return HubApiCallResult<T>.Success(envelope.Status, typedPayload);
        }
        catch (Exception ex) when (ex is JSException or TaskCanceledException or JsonException)
        {
            return HubApiCallResult<T>.Failure(0, $"Hub request '{path}' failed in the browser head: {ex.Message}");
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

    private static string AppendQuery(string path, params (string Key, string? Value)[] queryValues)
    {
        List<string> encodedValues = [];
        foreach ((string key, string? value) in queryValues)
        {
            string? normalizedValue = NormalizeQueryValue(value);
            if (normalizedValue is null)
            {
                continue;
            }

            encodedValues.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(normalizedValue)}");
        }

        return encodedValues.Count == 0
            ? path
            : $"{path}?{string.Join("&", encodedValues)}";
    }

    private static string? NormalizeQueryValue(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

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

    private sealed record HubFetchEnvelope(
        int Status,
        string? Text);
}

public sealed record HubApiCallResult<T>(
    int StatusCode,
    T? Payload = default,
    string? ErrorMessage = null)
{
    public bool IsSuccess => ErrorMessage is null;

    public static HubApiCallResult<T> Success(int statusCode, T payload)
        => new(statusCode, payload);

    public static HubApiCallResult<T> Failure(int statusCode, string message)
        => new(statusCode, default, message);
}
