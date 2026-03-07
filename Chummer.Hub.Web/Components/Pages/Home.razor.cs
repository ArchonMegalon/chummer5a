using Chummer.Contracts.AI;
using Chummer.Contracts.Content;
using Chummer.Contracts.Hub;
using Chummer.Contracts.Presentation;
using Microsoft.AspNetCore.Components;

namespace Chummer.Hub.Web.Components.Pages;

public partial class Home : ComponentBase
{
    private const string DefaultDraftKind = HubCatalogItemKinds.RuleProfile;
    private const string DefaultDraftProjectId = "profile.new";
    private const string DefaultDraftRulesetId = "sr5";
    private const int CoachAuditCount = 3;

    private string _queryText = string.Empty;
    private string _selectedKind = string.Empty;
    private string _targetKind = RuleProfileApplyTargetKinds.GlobalDefaults;
    private string _targetId = "hub-preview";
    private string _draftKindFilter = string.Empty;
    private string _draftRulesetFilter = string.Empty;
    private string _draftStateFilter = string.Empty;
    private string _moderationStateFilter = string.Empty;
    private string _moderationDecisionNotes = string.Empty;
    private string _draftEditorKind = DefaultDraftKind;
    private string _draftEditorProjectId = DefaultDraftProjectId;
    private string _draftEditorRulesetId = DefaultDraftRulesetId;
    private string _draftEditorTitle = "New Hub Draft";
    private string _draftEditorSummary = string.Empty;
    private string _draftEditorDescription = string.Empty;
    private string _draftSubmissionNotes = string.Empty;
    private HubCatalogResultPage? _catalog;
    private HubProjectDetailProjection? _detail;
    private HubProjectCompatibilityMatrix? _compatibility;
    private HubProjectInstallPreviewReceipt? _installPreview;
    private HubPublishDraftList? _drafts;
    private HubDraftDetailProjection? _draftDetail;
    private HubModerationQueue? _moderationQueue;
    private AiGatewayStatusProjection? _coachStatus;
    private IReadOnlyList<AiProviderHealthProjection> _coachProviderHealth = [];
    private IReadOnlyList<AiConversationAuditSummary> _coachAudits = [];
    private string? _selectedKindForDetail;
    private string? _selectedItemId;
    private string? _statusMessage;
    private string? _errorMessage;
    private string? _coachErrorMessage;
    private bool _isBusy;

    [Inject]
    private BrowserHubApiClient HubApi { get; set; } = default!;

    [Inject]
    private BrowserHubCoachApiClient HubCoachApi { get; set; } = default!;

    private bool HasSelectedDraft => _draftDetail is not null;

    private string BuildCoachLaunchUri()
        => AiCoachLaunchQuery.BuildRelativeUri(
            "/coach/",
            new AiCoachLaunchContext(
                RouteType: AiRouteTypes.Coach,
                RuntimeFingerprint: _detail?.RuntimeFingerprint,
                RulesetId: NormalizeOptionalText(_detail?.Summary.RulesetId)
                    ?? NormalizeOptionalText(_draftDetail?.Draft.RulesetId)
                    ?? NormalizeOptionalText(_draftEditorRulesetId),
                BuildIdeaQuery: NormalizeOptionalText(_queryText)));

    private string BuildCoachLaunchUri(AiConversationAuditSummary audit)
        => AiCoachLaunchQuery.BuildRelativeUri(
            "/coach/",
            new AiCoachLaunchContext(
                RouteType: audit.RouteType,
                ConversationId: audit.ConversationId,
                RuntimeFingerprint: audit.RuntimeFingerprint,
                CharacterId: audit.CharacterId,
                WorkspaceId: audit.WorkspaceId,
                RulesetId: NormalizeOptionalText(_detail?.Summary.RulesetId)
                    ?? NormalizeOptionalText(_draftDetail?.Draft.RulesetId)
                    ?? NormalizeOptionalText(_draftEditorRulesetId)));

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await LoadCatalogAsync();
    }

    private Task LoadCatalogAsync()
        => RunBusyAsync(
            "Loading ChummerHub catalog results.",
            async () =>
            {
                BrowseQuery query = BuildQuery();
                HubApiCallResult<HubCatalogResultPage> result = await HubApi.SearchAsync(query);
                if (!TryCaptureResult(result, payload =>
                    {
                        _catalog = payload;
                        _detail = null;
                        _compatibility = null;
                        _installPreview = null;
                        _selectedKindForDetail = null;
                        _selectedItemId = null;
                    }))
                {
                    return;
                }

                await LoadCoachSidecarCoreAsync();
                _statusMessage = $"Loaded {_catalog!.TotalCount} hub result(s).";
            });

    private Task LoadCoachSidecarAsync()
        => RunBusyAsync(
            "Refreshing the Hub Coach sidecar.",
            async () =>
            {
                await LoadCoachSidecarCoreAsync();
                _statusMessage = "Hub Coach sidecar refreshed.";
            });

    private Task InspectProjectAsync(string kind, string itemId)
        => RunBusyAsync(
            $"Loading hub detail for '{itemId}'.",
            async () =>
            {
                HubApiCallResult<HubProjectDetailProjection> detailResult = await HubApi.GetProjectDetailAsync(kind, itemId);
                if (!TryCaptureResult(detailResult, payload => _detail = payload))
                {
                    return;
                }

                HubApiCallResult<HubProjectCompatibilityMatrix> compatibilityResult = await HubApi.GetCompatibilityAsync(kind, itemId);
                if (!TryCaptureResult(compatibilityResult, payload => _compatibility = payload))
                {
                    return;
                }

                _selectedKindForDetail = kind;
                _selectedItemId = itemId;
                _installPreview = null;
                _statusMessage = $"Loaded detail and compatibility for '{itemId}'.";
            });

    private Task PreviewInstallAsync()
        => RunBusyAsync(
            "Previewing hub install target changes.",
            async () =>
            {
                if (_detail is null)
                {
                    _errorMessage = "Select a hub project before previewing install changes.";
                    return;
                }

                RuleProfileApplyTarget target = new(_targetKind, string.IsNullOrWhiteSpace(_targetId) ? "hub-preview" : _targetId.Trim());
                HubApiCallResult<HubProjectInstallPreviewReceipt> result = await HubApi.GetInstallPreviewAsync(
                    _detail.Summary.Kind,
                    _detail.Summary.ItemId,
                    target);

                if (!TryCaptureResult(result, payload => _installPreview = payload))
                {
                    return;
                }

                _statusMessage = $"Previewed install changes for '{_detail.Summary.ItemId}'.";
            });

    private Task LoadDraftsAsync()
        => RunBusyAsync(
            "Loading owner-backed hub drafts.",
            async () =>
            {
                if (!await LoadDraftsCoreAsync())
                {
                    return;
                }

                _statusMessage = $"Loaded {_drafts!.Items.Count} owner draft(s).";
            });

    private Task InspectDraftAsync(string draftId)
        => RunBusyAsync(
            $"Loading hub draft '{draftId}'.",
            async () =>
            {
                if (!await LoadDraftDetailCoreAsync(draftId))
                {
                    return;
                }

                _statusMessage = $"Loaded draft '{_draftDetail!.Draft.Title}'.";
            });

    private Task StartNewDraftAsync()
        => RunBusyAsync(
            "Preparing a new hub draft.",
            () =>
            {
                ResetDraftEditor();
                _statusMessage = "Draft composer reset for a new publication entry.";
                return Task.CompletedTask;
            });

    private Task CreateDraftAsync()
        => RunBusyAsync(
            "Creating a hub publication draft.",
            async () =>
            {
                HubPublishDraftRequest request = new(
                    ProjectKind: _draftEditorKind.Trim(),
                    ProjectId: _draftEditorProjectId.Trim(),
                    RulesetId: _draftEditorRulesetId.Trim(),
                    Title: _draftEditorTitle.Trim(),
                    Summary: NormalizeOptionalText(_draftEditorSummary),
                    Description: NormalizeOptionalText(_draftEditorDescription));

                HubApiCallResult<HubPublishDraftReceipt> result = await HubApi.CreateDraftAsync(request);
                if (!TryCaptureResult(result, payload => ApplyDraftReceipt(payload, NormalizeOptionalText(_draftEditorDescription))))
                {
                    return;
                }

                await LoadDraftsCoreAsync();
                _statusMessage = $"Created draft '{_draftDetail!.Draft.Title}'.";
            });

    private Task SaveDraftAsync()
        => RunBusyAsync(
            "Saving hub draft metadata.",
            async () =>
            {
                if (_draftDetail is null)
                {
                    _errorMessage = "Load or create a hub draft before saving updates.";
                    return;
                }

                HubUpdateDraftRequest request = new(
                    Title: _draftEditorTitle.Trim(),
                    Summary: NormalizeOptionalText(_draftEditorSummary),
                    Description: NormalizeOptionalText(_draftEditorDescription),
                    PublisherId: _draftDetail.Draft.PublisherId);

                HubApiCallResult<HubPublishDraftReceipt> result = await HubApi.UpdateDraftAsync(_draftDetail.Draft.DraftId, request);
                if (!TryCaptureResult(result, payload => ApplyDraftReceipt(payload, NormalizeOptionalText(_draftEditorDescription))))
                {
                    return;
                }

                await LoadDraftsCoreAsync();
                _statusMessage = $"Saved draft '{_draftDetail!.Draft.Title}'.";
            });

    private Task SubmitDraftAsync()
        => RunBusyAsync(
            "Submitting hub draft for review.",
            async () =>
            {
                if (_draftDetail is null)
                {
                    _errorMessage = "Load or create a hub draft before submitting it for review.";
                    return;
                }

                HubApiCallResult<HubProjectSubmissionReceipt> result = await HubApi.SubmitDraftAsync(
                    _draftDetail.Draft.ProjectKind,
                    _draftDetail.Draft.ProjectId,
                    _draftDetail.Draft.RulesetId,
                    new HubSubmitProjectRequest(
                        Notes: NormalizeOptionalText(_draftSubmissionNotes),
                        PublisherId: _draftDetail.Draft.PublisherId));

                if (!TryCaptureResult(result, payload =>
                    {
                        _statusMessage = $"Submitted draft '{payload.ProjectId}' for review.";
                    }))
                {
                    return;
                }

                await LoadDraftsCoreAsync();
                await LoadDraftDetailCoreAsync(_draftDetail.Draft.DraftId);
            });

    private Task ArchiveDraftAsync()
        => RunBusyAsync(
            "Archiving the selected hub draft.",
            async () =>
            {
                if (_draftDetail is null)
                {
                    _errorMessage = "Load or create a hub draft before archiving it.";
                    return;
                }

                HubApiCallResult<HubPublishDraftReceipt> result = await HubApi.ArchiveDraftAsync(_draftDetail.Draft.DraftId);
                if (!TryCaptureResult(result, payload => ApplyDraftReceipt(payload, NormalizeOptionalText(_draftEditorDescription))))
                {
                    return;
                }

                UpsertDraftListReceipt(_draftDetail.Draft);
                _statusMessage = $"Archived draft '{_draftDetail.Draft.Title}'.";
            });

    private Task DeleteDraftAsync()
        => RunBusyAsync(
            "Deleting the selected hub draft.",
            async () =>
            {
                if (_draftDetail is null)
                {
                    _errorMessage = "Load or create a hub draft before deleting it.";
                    return;
                }

                string draftId = _draftDetail.Draft.DraftId;
                string draftTitle = _draftDetail.Draft.Title;
                HubApiCallResult<bool> result = await HubApi.DeleteDraftAsync(draftId);
                if (!TryCaptureResult(result, _ => { }))
                {
                    return;
                }

                RemoveDraftFromList(draftId);
                ResetDraftEditor();
                _statusMessage = $"Deleted draft '{draftTitle}'.";
            });

    private Task LoadModerationQueueAsync()
        => RunBusyAsync(
            "Loading hub moderation queue.",
            async () =>
            {
                HubApiCallResult<HubModerationQueue> result = await HubApi.ListModerationQueueAsync(NormalizeOptionalText(_moderationStateFilter));
                if (!TryCaptureResult(result, payload => _moderationQueue = payload))
                {
                    return;
                }

                _statusMessage = $"Loaded {_moderationQueue!.Items.Count} moderation queue item(s).";
            });

    private Task ApproveModerationCaseAsync(string caseId)
        => RunBusyAsync(
            $"Approving moderation case '{caseId}'.",
            async () =>
            {
                HubApiCallResult<HubModerationDecisionReceipt> result = await HubApi.ApproveModerationCaseAsync(
                    caseId,
                    new HubModerationDecisionRequest(NormalizeOptionalText(_moderationDecisionNotes)));

                if (!TryCaptureResult(result, ApplyModerationDecision))
                {
                    return;
                }

                _statusMessage = $"Approved moderation case '{caseId}'.";
            });

    private Task RejectModerationCaseAsync(string caseId)
        => RunBusyAsync(
            $"Rejecting moderation case '{caseId}'.",
            async () =>
            {
                HubApiCallResult<HubModerationDecisionReceipt> result = await HubApi.RejectModerationCaseAsync(
                    caseId,
                    new HubModerationDecisionRequest(NormalizeOptionalText(_moderationDecisionNotes)));

                if (!TryCaptureResult(result, ApplyModerationDecision))
                {
                    return;
                }

                _statusMessage = $"Rejected moderation case '{caseId}'.";
            });

    private BrowseQuery BuildQuery()
    {
        Dictionary<string, IReadOnlyList<string>> facets = new(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_selectedKind))
        {
            facets[HubCatalogFacetIds.Kind] = [_selectedKind];
        }

        return new BrowseQuery(
            QueryText: _queryText.Trim(),
            FacetSelections: facets,
            SortId: HubCatalogSortIds.Title,
            SortDirection: BrowseSortDirections.Ascending,
            Offset: 0,
            Limit: 12);
    }

    private async Task<bool> LoadDraftsCoreAsync()
    {
        HubApiCallResult<HubPublishDraftList> result = await HubApi.ListDraftsAsync(
            NormalizeOptionalText(_draftKindFilter),
            NormalizeOptionalText(_draftRulesetFilter),
            NormalizeOptionalText(_draftStateFilter));

        return TryCaptureResult(result, payload => _drafts = payload);
    }

    private async Task<bool> LoadDraftDetailCoreAsync(string draftId)
    {
        HubApiCallResult<HubDraftDetailProjection> result = await HubApi.GetDraftAsync(draftId);
        return TryCaptureResult(result, payload =>
        {
            _draftDetail = payload;
            ApplyDraftDetail(payload);
        });
    }

    private void ApplyDraftReceipt(HubPublishDraftReceipt receipt, string? description)
        => ApplyDraftDetail(
            new HubDraftDetailProjection(
                Draft: receipt,
                Moderation: _draftDetail?.Moderation,
                Description: description,
                LatestModerationNotes: _draftDetail?.LatestModerationNotes,
                LatestModerationUpdatedAtUtc: _draftDetail?.LatestModerationUpdatedAtUtc));

    private void ApplyDraftDetail(HubDraftDetailProjection detail)
    {
        _draftDetail = detail;
        _draftEditorKind = detail.Draft.ProjectKind;
        _draftEditorProjectId = detail.Draft.ProjectId;
        _draftEditorRulesetId = detail.Draft.RulesetId;
        _draftEditorTitle = detail.Draft.Title;
        _draftEditorSummary = detail.Draft.Summary ?? string.Empty;
        _draftEditorDescription = detail.Description ?? string.Empty;
    }

    private void ResetDraftEditor()
    {
        _draftDetail = null;
        _draftEditorKind = DefaultDraftKind;
        _draftEditorProjectId = DefaultDraftProjectId;
        _draftEditorRulesetId = DefaultDraftRulesetId;
        _draftEditorTitle = "New Hub Draft";
        _draftEditorSummary = string.Empty;
        _draftEditorDescription = string.Empty;
        _draftSubmissionNotes = string.Empty;
    }

    private void UpsertDraftListReceipt(HubPublishDraftReceipt receipt)
    {
        if (_drafts is null)
        {
            _drafts = new HubPublishDraftList([receipt]);
            return;
        }

        HubPublishDraftReceipt[] items = _drafts.Items
            .Where(existing => !string.Equals(existing.DraftId, receipt.DraftId, StringComparison.Ordinal))
            .Append(receipt)
            .OrderBy(existing => existing.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _drafts = new HubPublishDraftList(items);
    }

    private void RemoveDraftFromList(string draftId)
    {
        if (_drafts is null)
        {
            return;
        }

        HubPublishDraftReceipt[] remaining = _drafts.Items
            .Where(existing => !string.Equals(existing.DraftId, draftId, StringComparison.Ordinal))
            .ToArray();
        _drafts = new HubPublishDraftList(remaining);
    }

    private void ApplyModerationDecision(HubModerationDecisionReceipt receipt)
    {
        if (_moderationQueue is not null)
        {
            HubModerationQueueItem[] updatedQueue = _moderationQueue.Items
                .Select(item => string.Equals(item.CaseId, receipt.CaseId, StringComparison.Ordinal)
                    ? item with
                    {
                        State = receipt.State
                    }
                    : item)
                .ToArray();
            _moderationQueue = new HubModerationQueue(updatedQueue);
        }

        if (_draftDetail is not null
            && string.Equals(_draftDetail.Draft.DraftId, receipt.DraftId, StringComparison.Ordinal))
        {
            HubModerationQueueItem moderationItem = _draftDetail.Moderation is not null
                ? _draftDetail.Moderation with
                {
                    CaseId = receipt.CaseId,
                    State = receipt.State
                }
                : new HubModerationQueueItem(
                    CaseId: receipt.CaseId,
                    DraftId: receipt.DraftId,
                    ProjectKind: receipt.ProjectKind,
                    ProjectId: receipt.ProjectId,
                    RulesetId: receipt.RulesetId,
                    Title: _draftDetail.Draft.Title,
                    OwnerId: receipt.OwnerId,
                    PublisherId: receipt.PublisherId,
                    State: receipt.State,
                    CreatedAtUtc: receipt.UpdatedAtUtc,
                    Summary: _draftDetail.Draft.Summary);

            _draftDetail = _draftDetail with
            {
                Moderation = moderationItem,
                LatestModerationNotes = receipt.Notes,
                LatestModerationUpdatedAtUtc = receipt.UpdatedAtUtc
            };
        }
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private async Task LoadCoachSidecarCoreAsync()
    {
        HubCoachApiCallResult<AiGatewayStatusProjection> statusResult = await HubCoachApi.GetStatusAsync();
        if (!TryCaptureCoachResult(statusResult, payload => _coachStatus = payload))
        {
            return;
        }

        HubCoachApiCallResult<AiProviderHealthProjection[]> providerResult = await HubCoachApi.ListProviderHealthAsync(AiRouteTypes.Coach);
        if (!TryCaptureCoachResult(providerResult, payload => _coachProviderHealth = payload))
        {
            return;
        }

        HubCoachApiCallResult<AiConversationAuditCatalogPage> auditResult = await HubCoachApi.ListConversationAuditsAsync(AiRouteTypes.Coach, CoachAuditCount);
        TryCaptureCoachResult(auditResult, payload => _coachAudits = payload.Items);
    }

    private bool TryCaptureResult<T>(HubApiCallResult<T> result, Action<T> apply)
    {
        _errorMessage = null;

        if (!result.IsSuccess)
        {
            _errorMessage = result.ErrorMessage ?? $"Hub request failed with HTTP {result.StatusCode}.";
            return false;
        }

        if (result.Payload is null)
        {
            _errorMessage = $"Hub request returned HTTP {result.StatusCode} without a payload.";
            return false;
        }

        apply(result.Payload);
        return true;
    }

    private bool TryCaptureCoachResult<T>(HubCoachApiCallResult<T> result, Action<T> apply)
    {
        _coachErrorMessage = null;

        if (!result.IsImplemented)
        {
            _coachErrorMessage = result.NotImplemented?.Message ?? "Hub Coach sidecar route is not implemented yet.";
            return false;
        }

        if (result.QuotaExceeded is not null)
        {
            _coachErrorMessage = result.QuotaExceeded.Message;
            return false;
        }

        if (!result.IsSuccess)
        {
            _coachErrorMessage = result.ErrorMessage ?? $"Coach request failed with HTTP {result.StatusCode}.";
            return false;
        }

        if (result.Payload is null)
        {
            _coachErrorMessage = $"Coach request returned HTTP {result.StatusCode} without a payload.";
            return false;
        }

        apply(result.Payload);
        return true;
    }

    private async Task RunBusyAsync(string busyMessage, Func<Task> operation)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        _errorMessage = null;
        _statusMessage = busyMessage;
        StateHasChanged();

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isBusy = false;
            StateHasChanged();
        }
    }

    private string GetStatusChipClass()
        => _isBusy
            ? "status-chip busy"
            : !string.IsNullOrWhiteSpace(_errorMessage)
                ? "status-chip error"
                : "status-chip ok";

    private string GetStatusChipText()
        => _isBusy
            ? "loading"
            : !string.IsNullOrWhiteSpace(_errorMessage)
                ? "error"
                : "ready";

    private AiRouteBudgetStatusProjection? GetCoachRouteBudgetStatus()
        => _coachStatus?.RouteBudgetStatuses?.FirstOrDefault(status => string.Equals(status.RouteType, AiRouteTypes.Coach, StringComparison.Ordinal));

    private IReadOnlyList<AiProviderHealthProjection> GetCoachProviders()
        => _coachProviderHealth
            .Where(provider => provider.AllowedRouteTypes.Contains(AiRouteTypes.Coach))
            .ToArray();

    private string DescribeCoachProviderTransport(AiProviderHealthProjection provider)
    {
        string readiness = provider.TransportMetadataConfigured
            ? "ready"
            : provider.TransportBaseUrlConfigured || provider.TransportModelConfigured
                ? "partial"
                : "missing";
        return $"{readiness} · base {(provider.TransportBaseUrlConfigured ? "yes" : "no")} · model {(provider.TransportModelConfigured ? "yes" : "no")}";
    }

    private string DescribeCoachProviderKeys(AiProviderHealthProjection provider)
        => $"primary {provider.PrimaryCredentialCount} / fallback {provider.FallbackCredentialCount}";

    private static string DescribeCoachProviderBinding(AiProviderHealthProjection provider)
    {
        string route = string.IsNullOrWhiteSpace(provider.LastRouteType)
            ? "route n/a"
            : $"route {provider.LastRouteType}";
        string binding = string.IsNullOrWhiteSpace(provider.LastCredentialTier)
            ? "binding none"
            : provider.LastCredentialSlotIndex is int slotIndex
                ? $"binding {provider.LastCredentialTier} / slot {slotIndex}"
                : $"binding {provider.LastCredentialTier}";
        return $"{route} · {binding}";
    }

    private static string GetCoachProviderHealthBadgeClass(AiProviderHealthProjection provider)
        => provider.CircuitState switch
        {
            AiProviderCircuitStates.Open => "badge error",
            AiProviderCircuitStates.Degraded => "badge warn",
            AiProviderCircuitStates.Closed => "badge good",
            _ => "badge"
        };

    private static string GetCoachCacheBadgeClass(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "badge good",
            AiCacheStatuses.Miss => "badge warn",
            _ => "badge"
        };

    private static string FormatCoachCacheStatus(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "cache hit",
            AiCacheStatuses.Miss => "cache miss",
            _ => "cache none"
        };

    private static string DescribeCoachCoverage(AiGroundingCoverage? coverage)
        => coverage is null
            ? "none"
            : $"{coverage.ScorePercent}% · {coverage.Summary}";

    private static string DescribeCoachBudgetSnapshot(AiBudgetSnapshot? budget)
        => budget is null
            ? "n/a"
            : $"{budget.MonthlyConsumed} / {budget.MonthlyAllowance} {budget.BudgetUnit} · burst {budget.CurrentBurstConsumed} / {budget.BurstLimitPerMinute}";

    private static string DescribeCoachRecommendationSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Recommendations.Count == 0)
        {
            return "none";
        }

        AiRecommendation top = structuredAnswer.Recommendations[0];
        return $"{structuredAnswer.Recommendations.Count} · {top.Title}";
    }

    private static string DescribeCoachEvidenceSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Evidence.Count == 0)
        {
            return "none";
        }

        AiEvidenceEntry lead = structuredAnswer.Evidence[0];
        return $"{structuredAnswer.Evidence.Count} · {lead.Title}";
    }

    private static string DescribeCoachRiskSummary(AiStructuredAnswer? structuredAnswer)
    {
        if (structuredAnswer is null || structuredAnswer.Risks.Count == 0)
        {
            return "none";
        }

        AiRiskEntry lead = structuredAnswer.Risks[0];
        return $"{structuredAnswer.Risks.Count} · {lead.Title}";
    }

    private static string DescribeCoachSourceSummary(AiStructuredAnswer? structuredAnswer)
        => structuredAnswer is null
            ? "0 sources / 0 action drafts"
            : $"{structuredAnswer.Sources.Count} sources / {structuredAnswer.ActionDrafts.Count} action drafts";

    private static string DescribeCoachRouteDecision(AiProviderRouteDecision? routeDecision, string providerId)
    {
        if (routeDecision is null)
        {
            return providerId;
        }

        string binding = DescribeCoachCredentialBinding(routeDecision);
        return string.Equals(binding, "none", StringComparison.Ordinal)
            ? $"{routeDecision.ProviderId} · {routeDecision.Reason}"
            : $"{routeDecision.ProviderId} · {routeDecision.Reason} · {binding}";
    }

    private static string DescribeCoachCredentialBinding(AiProviderRouteDecision routeDecision)
    {
        if (string.IsNullOrWhiteSpace(routeDecision.CredentialTier)
            || string.Equals(routeDecision.CredentialTier, AiProviderCredentialTiers.None, StringComparison.Ordinal))
        {
            return "none";
        }

        return routeDecision.CredentialSlotIndex is int slotIndex
            ? $"{routeDecision.CredentialTier} / slot {slotIndex}"
            : routeDecision.CredentialTier;
    }

    private static string FormatCoachTimestamp(DateTimeOffset? value)
        => value?.ToString("yyyy-MM-dd HH:mm:ss") ?? "n/a";
}
