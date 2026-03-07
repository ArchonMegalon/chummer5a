using Chummer.Contracts.AI;
using Microsoft.AspNetCore.Components;

namespace Chummer.Coach.Web.Components.Pages;

public partial class Home : ComponentBase
{
    private const string DefaultRouteType = AiRouteTypes.Coach;
    private const string DefaultPersonaId = AiPersonaIds.DeckerContact;
    private const string DefaultMessageText = "What should I spend 18 Karma on next?";
    private const string DefaultBuildIdeaQueryText = "stealth";
    private const int DefaultPromptCount = 6;
    private const int DefaultBuildIdeaCount = 6;
    private const int DefaultConversationCount = 6;

    private string _messageText = DefaultMessageText;
    private string _routeType = DefaultRouteType;
    private string _launchConversationId = string.Empty;
    private string _runtimeFingerprint = string.Empty;
    private string _characterId = string.Empty;
    private string _workspaceId = string.Empty;
    private string _buildIdeaQueryText = DefaultBuildIdeaQueryText;
    private string _rulesetId = "sr5";
    private AiGatewayStatusProjection? _status;
    private IReadOnlyList<AiProviderHealthProjection> _providerHealth = [];
    private IReadOnlyList<AiPromptDescriptor> _prompts = [];
    private IReadOnlyList<BuildIdeaCard> _buildIdeas = [];
    private AiConversationTurnPreview? _preview;
    private AiConversationTurnResponse? _turnResponse;
    private AiActionPreviewReceipt? _actionPreview;
    private AiRuntimeSummaryProjection? _runtimeSummaryCard;
    private AiConversationAuditCatalogPage? _conversationCatalog;
    private AiConversationSnapshot? _selectedConversation;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _isBusy;

    [Inject]
    private BrowserCoachApiClient CoachApi { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        AiCoachLaunchContext launchContext = AiCoachLaunchQuery.Parse(new Uri(Navigation.Uri).Query);
        _routeType = NormalizeOptionalText(launchContext.RouteType) ?? DefaultRouteType;
        _launchConversationId = NormalizeOptionalText(launchContext.ConversationId) ?? string.Empty;
        _runtimeFingerprint = NormalizeOptionalText(launchContext.RuntimeFingerprint) ?? string.Empty;
        _characterId = NormalizeOptionalText(launchContext.CharacterId) ?? string.Empty;
        _workspaceId = NormalizeOptionalText(launchContext.WorkspaceId) ?? string.Empty;
        _rulesetId = NormalizeOptionalText(launchContext.RulesetId) ?? _rulesetId;
        _messageText = NormalizeOptionalText(launchContext.Message) ?? _messageText;
        _buildIdeaQueryText = NormalizeOptionalText(launchContext.BuildIdeaQuery) ?? _buildIdeaQueryText;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await ReloadAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Task ReloadAsync()
        => RunBusyAsync(
            "Loading live Chummer Coach gateway metadata.",
            async () =>
            {
                if (!await LoadStatusCoreAsync())
                {
                    return;
                }

                if (!await LoadPromptsCoreAsync())
                {
                    return;
                }

                if (!await LoadProviderHealthCoreAsync())
                {
                    return;
                }

                if (!await LoadBuildIdeasCoreAsync())
                {
                    return;
                }

                if (!await LoadConversationCatalogCoreAsync())
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(_launchConversationId)
                    && !await LoadConversationDetailCoreAsync(_launchConversationId))
                {
                    return;
                }

                _statusMessage = string.IsNullOrWhiteSpace(_launchConversationId)
                    ? "Coach gateway metadata, prompt registry, build ideas, and conversation catalog are current."
                    : $"Coach gateway metadata is current and conversation '{_launchConversationId}' is loaded.";
            });

    private Task SearchBuildIdeasAsync()
        => RunBusyAsync(
            "Refreshing coach build idea retrieval.",
            async () =>
            {
                if (!await LoadBuildIdeasCoreAsync())
                {
                    return;
                }

                _statusMessage = $"Loaded {_buildIdeas.Count} build idea card(s) for '{NormalizeQueryText()}'.";
            });

    private Task PreviewCoachTurnAsync()
        => RunBusyAsync(
            "Previewing the grounded coach turn plan.",
            async () =>
            {
                if (!await LoadPreviewCoreAsync())
                {
                    return;
                }

                _statusMessage = "Coach turn preview loaded from grounded runtime and retrieval metadata.";
            });

    private Task SendCoachTurnAsync()
        => RunBusyAsync(
            "Submitting the grounded coach turn.",
            async () =>
            {
                AiConversationTurnRequest? request = BuildTurnRequest();
                if (request is null)
                {
                    _errorMessage = "Enter a coach question before sending a live coach turn.";
                    return;
                }

                BrowserCoachApiCallResult<AiConversationTurnResponse> result = await CoachApi.SendTurnAsync(EffectiveRouteType, request);
                if (!TryCaptureResult(
                        result,
                        payload =>
                        {
                            _turnResponse = payload;
                            ApplyGroundingScope(payload.Grounding);
                        }))
                {
                    return;
                }

                AiConversationTurnResponse turnResponse = _turnResponse!;
                if (await LoadConversationDetailCoreAsync(turnResponse.ConversationId) && _selectedConversation is not null)
                {
                    PromoteConversationSummary(_selectedConversation);
                }

                _statusMessage = $"Coach turn stored in conversation '{turnResponse.ConversationId}'.";
            });

    private Task LoadConversationsAsync()
        => RunBusyAsync(
            "Refreshing stored coach conversations.",
            async () =>
            {
                if (!await LoadConversationCatalogCoreAsync())
                {
                    return;
                }

                _statusMessage = $"Loaded {_conversationCatalog?.Items.Count ?? 0} stored coach conversation(s).";
            });

    private Task OpenConversationAsync(string conversationId)
        => RunBusyAsync(
            $"Loading stored conversation '{conversationId}'.",
            async () =>
            {
                if (!await LoadConversationDetailCoreAsync(conversationId))
                {
                    return;
                }

                _statusMessage = $"Loaded conversation '{conversationId}'.";
            });

    private Task PreviewActionDraftAsync(AiActionDraft draft)
        => RunBusyAsync(
            $"Preparing '{draft.Title}' as a non-mutating preview.",
            async () =>
            {
                if (!TryResolveActionPreviewScope(draft, out string characterId, out string runtimeFingerprint, out string? workspaceId))
                {
                    return;
                }

                BrowserCoachApiCallResult<AiActionPreviewReceipt> result = draft.ActionId switch
                {
                    AiSuggestedActionIds.PreviewKarmaSpend => await CoachApi.PreviewKarmaSpendAsync(
                        new AiSpendPlanPreviewRequest(
                            CharacterId: characterId,
                            RuntimeFingerprint: runtimeFingerprint,
                            Steps:
                            [
                                new AiSpendPlanStep(
                                    StepId: draft.ActionId,
                                    Title: draft.Title,
                                    Notes: draft.Description)
                            ],
                            Goal: draft.Description,
                            WorkspaceId: workspaceId)),
                    AiSuggestedActionIds.PreviewNuyenSpend => await CoachApi.PreviewNuyenSpendAsync(
                        new AiSpendPlanPreviewRequest(
                            CharacterId: characterId,
                            RuntimeFingerprint: runtimeFingerprint,
                            Steps:
                            [
                                new AiSpendPlanStep(
                                    StepId: draft.ActionId,
                                    Title: draft.Title,
                                    Notes: draft.Description)
                            ],
                            Goal: draft.Description,
                            WorkspaceId: workspaceId)),
                    AiSuggestedActionIds.PreviewApplyPlan => await CoachApi.CreateApplyPreviewAsync(
                        new AiApplyPreviewRequest(
                            CharacterId: characterId,
                            RuntimeFingerprint: runtimeFingerprint,
                            ActionDraft: draft,
                            Goal: draft.Description,
                            WorkspaceId: workspaceId)),
                    _ => BrowserCoachApiCallResult<AiActionPreviewReceipt>.Failure(
                        0,
                        $"Action draft '{draft.Title}' does not map to a non-mutating preview endpoint yet.")
                };

                if (!TryCaptureResult(
                        result,
                        payload =>
                        {
                            _actionPreview = payload;
                            if (!string.IsNullOrWhiteSpace(payload.WorkspaceId))
                            {
                                _workspaceId = payload.WorkspaceId;
                            }
                        }))
                {
                    return;
                }

                _statusMessage = $"Prepared {_actionPreview!.PreviewKind} preview for '{draft.Title}'.";
            });

    private Task OpenRuntimeInspectorActionDraftAsync(AiActionDraft draft)
        => RunBusyAsync(
            $"Loading runtime summary for '{draft.Title}'.",
            async () =>
            {
                string? runtimeFingerprint = draft.RuntimeFingerprint ?? NormalizeRuntimeFingerprint();
                if (string.IsNullOrWhiteSpace(runtimeFingerprint))
                {
                    _errorMessage = "Choose a runtime fingerprint before opening the grounded runtime summary.";
                    return;
                }

                BrowserCoachApiCallResult<AiRuntimeSummaryProjection> result = await CoachApi.GetRuntimeSummaryAsync(
                    runtimeFingerprint,
                    NormalizeRulesetId());
                if (!TryCaptureResult(
                        result,
                        payload =>
                        {
                            _runtimeSummaryCard = payload;
                            _runtimeFingerprint = payload.RuntimeFingerprint;
                            if (!string.IsNullOrWhiteSpace(payload.RulesetId))
                            {
                                _rulesetId = payload.RulesetId;
                            }
                        }))
                {
                    return;
                }

                _statusMessage = $"Loaded grounded runtime summary for '{runtimeFingerprint}'.";
            });

    private Task BrowseBuildIdeasActionDraftAsync(AiActionDraft draft)
        => RunBusyAsync(
            $"Loading build ideas for '{draft.Title}'.",
            async () =>
            {
                _buildIdeaQueryText = ResolveBuildIdeaActionQueryText(draft);

                if (!string.IsNullOrWhiteSpace(draft.RuntimeFingerprint))
                {
                    _runtimeFingerprint = draft.RuntimeFingerprint;
                }

                if (!string.IsNullOrWhiteSpace(draft.CharacterId))
                {
                    _characterId = draft.CharacterId;
                }

                if (!string.IsNullOrWhiteSpace(draft.WorkspaceId))
                {
                    _workspaceId = draft.WorkspaceId;
                }

                if (!await LoadBuildIdeasCoreAsync())
                {
                    return;
                }

                _statusMessage = $"Loaded {_buildIdeas.Count} build idea card(s) for '{NormalizeQueryText()}'.";
            });

    private async Task<bool> LoadStatusCoreAsync()
    {
        BrowserCoachApiCallResult<AiGatewayStatusProjection> result = await CoachApi.GetStatusAsync();
        return TryCaptureResult(
            result,
            payload =>
            {
                _status = payload;
            });
    }

    private async Task<bool> LoadPromptsCoreAsync()
    {
        BrowserCoachApiCallResult<AiPromptCatalog> result = await CoachApi.ListPromptsAsync(
            EffectiveRouteType,
            DefaultPersonaId,
            DefaultPromptCount);
        return TryCaptureResult(
            result,
            payload =>
            {
                _prompts = payload.Items;
            });
    }

    private async Task<bool> LoadProviderHealthCoreAsync()
    {
        BrowserCoachApiCallResult<AiProviderHealthProjection[]> result = await CoachApi.ListProviderHealthAsync(EffectiveRouteType);
        return TryCaptureResult(
            result,
            payload =>
            {
                _providerHealth = payload;
            });
    }

    private async Task<bool> LoadBuildIdeasCoreAsync()
    {
        BrowserCoachApiCallResult<AiBuildIdeaCatalog> result = await CoachApi.ListBuildIdeasAsync(
            EffectiveRouteType,
            NormalizeQueryText(),
            NormalizeRulesetId(),
            DefaultBuildIdeaCount);
        return TryCaptureResult(
            result,
            payload =>
            {
                _buildIdeas = payload.Items;
            });
    }

    private async Task<bool> LoadPreviewCoreAsync()
    {
        AiConversationTurnRequest? request = BuildTurnRequest();
        if (request is null)
        {
            _errorMessage = "Enter a coach question before previewing the turn plan.";
            return false;
        }

        BrowserCoachApiCallResult<AiConversationTurnPreview> result = await CoachApi.PreviewTurnAsync(EffectiveRouteType, request);
        return TryCaptureResult(
            result,
            payload =>
            {
                _preview = payload;
            });
    }

    private async Task<bool> LoadConversationCatalogCoreAsync()
    {
        BrowserCoachApiCallResult<AiConversationAuditCatalogPage> result = await CoachApi.ListConversationAuditsAsync(
            EffectiveRouteType,
            NormalizeCharacterId(),
            NormalizeRuntimeFingerprint(),
            NormalizeWorkspaceId(),
            DefaultConversationCount);
        return TryCaptureResult(
            result,
            payload =>
            {
                _conversationCatalog = payload;
                if (_selectedConversation is not null
                    && !payload.Items.Any(item => string.Equals(item.ConversationId, _selectedConversation.ConversationId, StringComparison.Ordinal)))
                {
                    _selectedConversation = null;
                }
            });
    }

    private async Task<bool> LoadConversationDetailCoreAsync(string conversationId)
    {
        BrowserCoachApiCallResult<AiConversationSnapshot> result = await CoachApi.GetConversationAsync(conversationId);
        return TryCaptureResult(
            result,
            payload =>
            {
                _selectedConversation = payload;
                PromoteConversationSummary(payload);
                ApplyConversationScope(payload);
            });
    }

    private AiConversationTurnRequest? BuildTurnRequest()
    {
        string message = NormalizeMessageText();
        return string.IsNullOrWhiteSpace(message)
            ? null
            : new AiConversationTurnRequest(
                Message: message,
                ConversationId: _selectedConversation?.ConversationId,
                RuntimeFingerprint: NormalizeRuntimeFingerprint(),
                CharacterId: NormalizeCharacterId(),
                WorkspaceId: NormalizeWorkspaceId());
    }

    private void PromoteConversationSummary(AiConversationSnapshot conversation)
    {
        AiConversationAuditSummary summary = CreateAuditSummary(conversation);
        List<AiConversationAuditSummary> items = _conversationCatalog?.Items.ToList() ?? [];
        int existingIndex = items.FindIndex(item => string.Equals(item.ConversationId, conversation.ConversationId, StringComparison.Ordinal));
        if (existingIndex >= 0)
        {
            items[existingIndex] = summary;
        }
        else
        {
            items.Insert(0, summary);
        }

        _conversationCatalog = new AiConversationAuditCatalogPage(items, items.Count);
    }

    private async Task RunBusyAsync(string pendingMessage, Func<Task> action)
    {
        _isBusy = true;
        _errorMessage = null;
        _statusMessage = pendingMessage;

        try
        {
            await action();
        }
        finally
        {
            _isBusy = false;
        }
    }

    private bool TryCaptureResult<T>(BrowserCoachApiCallResult<T> result, Action<T> apply)
    {
        if (result.QuotaExceeded is not null)
        {
            _errorMessage = result.QuotaExceeded.Message;
            return false;
        }

        if (!result.IsImplemented)
        {
            _errorMessage = result.NotImplemented?.Message ?? "This coach route is not implemented yet.";
            return false;
        }

        if (!result.IsSuccess || result.Payload is null)
        {
            _errorMessage = result.ErrorMessage ?? "Coach gateway request failed.";
            return false;
        }

        apply(result.Payload);
        return true;
    }

    private string GetStatusChipClass()
    {
        if (_isBusy)
        {
            return "status-chip busy";
        }

        if (!string.IsNullOrWhiteSpace(_errorMessage))
        {
            return "status-chip error";
        }

        return _status is null ? "status-chip" : "status-chip ok";
    }

    private string GetStatusChipText()
    {
        if (_isBusy)
        {
            return "loading";
        }

        if (!string.IsNullOrWhiteSpace(_errorMessage))
        {
            return "error";
        }

        return _status?.Status ?? "idle";
    }

    private static string DescribeCredentialBinding(AiProviderRouteDecision routeDecision)
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

    private AiProviderHealthProjection? GetProviderHealth(string providerId)
        => _providerHealth.FirstOrDefault(item => string.Equals(item.ProviderId, providerId, StringComparison.Ordinal));

    private IReadOnlyList<AiProviderDescriptor> GetVisibleProviders()
        => _status?.Providers?
            .Where(provider => provider.AllowedRouteTypes.Contains(EffectiveRouteType, StringComparer.Ordinal))
            .ToArray()
            ?? [];

    private static string GetProviderHealthBadgeClass(AiProviderHealthProjection? providerHealth)
        => providerHealth?.CircuitState switch
        {
            AiProviderCircuitStates.Open => "badge error",
            AiProviderCircuitStates.Degraded => "badge warn",
            AiProviderCircuitStates.Closed => "badge good",
            _ => "badge"
        };

    private static string GetCacheBadgeClass(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "badge good",
            AiCacheStatuses.Miss => "badge warn",
            _ => "badge"
        };

    private static string FormatCacheStatus(AiCacheMetadata? cache)
        => cache?.Status switch
        {
            AiCacheStatuses.Hit => "cache hit",
            AiCacheStatuses.Miss => "cache miss",
            _ => "cache none"
        };

    private static string DescribeCache(AiCacheMetadata? cache)
        => cache is null
            ? "none"
            : $"{cache.Status} · {FormatTimestamp(cache.CachedAtUtc)}";

    private static string DescribeTransportMetadata(AiProviderDescriptor provider)
        => provider.TransportMetadataConfigured
            ? "ready"
            : provider.TransportBaseUrlConfigured || provider.TransportModelConfigured
                ? "partial"
                : "missing";

    private static string DescribeProviderBinding(AiProviderHealthProjection? providerHealth)
    {
        if (providerHealth is null || string.IsNullOrWhiteSpace(providerHealth.LastCredentialTier))
        {
            return "none";
        }

        return providerHealth.LastCredentialSlotIndex is int slotIndex
            ? $"{providerHealth.LastCredentialTier} / slot {slotIndex}"
            : providerHealth.LastCredentialTier;
    }

    private static string DescribeRouteDecision(AiProviderRouteDecision? routeDecision, string providerId)
    {
        if (routeDecision is null)
        {
            return providerId;
        }

        string binding = DescribeCredentialBinding(routeDecision);
        return string.Equals(binding, "none", StringComparison.Ordinal)
            ? $"{routeDecision.ProviderId} · {routeDecision.Reason}"
            : $"{routeDecision.ProviderId} · {routeDecision.Reason} · {binding}";
    }

    private static string DescribeGroundingCoverage(AiGroundingCoverage? coverage)
        => coverage is null
            ? "none"
            : $"{coverage.ScorePercent}% · {coverage.Summary}";

    private static string DescribeCoverageSignals(IReadOnlyList<string>? values)
        => values is null || values.Count == 0
            ? "none"
            : string.Join(", ", values);

    private static string DescribeCoverageBadge(AiGroundingCoverage? coverage)
        => coverage is null
            ? "coverage n/a"
            : $"coverage {coverage.ScorePercent}%";

    private static AiConversationAuditSummary CreateAuditSummary(AiConversationSnapshot conversation)
    {
        AiConversationTurnRecord? lastTurn = conversation.Turns?.LastOrDefault();
        AiConversationMessage? lastAssistantMessage = conversation.Messages.LastOrDefault(message =>
            string.Equals(message.Role, AiConversationRoles.Assistant, StringComparison.Ordinal));
        AiConversationMessage? lastMessage = conversation.Messages.LastOrDefault();

        return new AiConversationAuditSummary(
            ConversationId: conversation.ConversationId,
            RouteType: conversation.RouteType,
            MessageCount: conversation.Messages.Count,
            LastUpdatedAtUtc: lastTurn?.CreatedAtUtc ?? lastMessage?.CreatedAtUtc,
            RuntimeFingerprint: conversation.RuntimeFingerprint,
            CharacterId: conversation.CharacterId,
            LastAssistantAnswer: lastTurn?.AssistantAnswer ?? lastAssistantMessage?.Content ?? lastMessage?.Content,
            LastProviderId: lastTurn?.ProviderId ?? lastAssistantMessage?.ProviderId,
            Cache: lastTurn?.Cache,
            RouteDecision: lastTurn?.RouteDecision,
            GroundingCoverage: lastTurn?.GroundingCoverage,
            WorkspaceId: lastTurn?.WorkspaceId ?? conversation.WorkspaceId,
            FlavorLine: lastTurn?.FlavorLine,
            Budget: lastTurn?.Budget,
            StructuredAnswer: lastTurn?.StructuredAnswer);
    }

    private static string FormatTimestamp(DateTimeOffset? value)
        => value?.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'") ?? "n/a";

    private static bool IsPreviewActionDraft(AiActionDraft draft)
        => string.Equals(draft.ActionId, AiSuggestedActionIds.PreviewKarmaSpend, StringComparison.Ordinal)
            || string.Equals(draft.ActionId, AiSuggestedActionIds.PreviewNuyenSpend, StringComparison.Ordinal)
            || string.Equals(draft.ActionId, AiSuggestedActionIds.PreviewApplyPlan, StringComparison.Ordinal);

    private static bool IsRuntimeInspectorActionDraft(AiActionDraft draft)
        => string.Equals(draft.ActionId, AiSuggestedActionIds.OpenRuntimeInspector, StringComparison.Ordinal);

    private static bool IsBuildIdeaActionDraft(AiActionDraft draft)
        => string.Equals(draft.ActionId, AiSuggestedActionIds.BrowseBuildIdeas, StringComparison.Ordinal);

    private static IReadOnlyList<AiActionDraft> GetPreviewActionDrafts(AiStructuredAnswer? structuredAnswer)
        => structuredAnswer?.ActionDrafts.Where(IsPreviewActionDraft).ToArray() ?? [];

    private static IReadOnlyList<AiActionDraft> GetRuntimeInspectorActionDrafts(AiStructuredAnswer? structuredAnswer)
        => structuredAnswer?.ActionDrafts.Where(IsRuntimeInspectorActionDraft).ToArray() ?? [];

    private static IReadOnlyList<AiActionDraft> GetBuildIdeaActionDrafts(AiStructuredAnswer? structuredAnswer)
        => structuredAnswer?.ActionDrafts.Where(IsBuildIdeaActionDraft).ToArray() ?? [];

    private static string BuildActionDraftButtonKey(string scopeKey, AiActionDraft draft)
        => $"{scopeKey}:{draft.ActionId}";

    private static string BuildSuggestedActionButtonKey(string scopeKey, AiSuggestedAction action)
        => $"{scopeKey}:{action.ActionId}";

    private static IReadOnlyList<AiSuggestedAction> GetPreviewSuggestedActions(IReadOnlyList<AiSuggestedAction> actions)
        => actions.Where(action => IsPreviewActionDraft(CreateActionDraft(action))).ToArray();

    private static IReadOnlyList<AiSuggestedAction> GetRuntimeInspectorSuggestedActions(IReadOnlyList<AiSuggestedAction> actions)
        => actions.Where(action => IsRuntimeInspectorActionDraft(CreateActionDraft(action))).ToArray();

    private static IReadOnlyList<AiSuggestedAction> GetBuildIdeaSuggestedActions(IReadOnlyList<AiSuggestedAction> actions)
        => actions.Where(action => IsBuildIdeaActionDraft(CreateActionDraft(action))).ToArray();

    private static AiActionDraft CreateActionDraft(AiSuggestedAction action)
        => new(
            ActionId: NormalizeSuggestedActionId(action),
            Title: action.Title,
            Description: action.Description,
            RequiresConfirmation: action.RequiresConfirmation,
            RuntimeFingerprint: action.RuntimeFingerprint,
            CharacterId: action.CharacterId,
            WorkspaceId: action.WorkspaceId);

    private static string NormalizeSuggestedActionId(AiSuggestedAction action)
    {
        if (string.Equals(action.ActionId, AiSuggestedActionIds.PreviewKarmaSpend, StringComparison.Ordinal)
            || string.Equals(action.Title, "Preview Karma Spend", StringComparison.Ordinal))
        {
            return AiSuggestedActionIds.PreviewKarmaSpend;
        }

        if (string.Equals(action.ActionId, AiSuggestedActionIds.PreviewNuyenSpend, StringComparison.Ordinal)
            || string.Equals(action.Title, "Preview Nuyen Spend", StringComparison.Ordinal))
        {
            return AiSuggestedActionIds.PreviewNuyenSpend;
        }

        if (string.Equals(action.ActionId, AiSuggestedActionIds.PreviewApplyPlan, StringComparison.Ordinal)
            || string.Equals(action.Title, "Preview Apply Plan", StringComparison.Ordinal))
        {
            return AiSuggestedActionIds.PreviewApplyPlan;
        }

        if (string.Equals(action.ActionId, AiSuggestedActionIds.OpenRuntimeInspector, StringComparison.Ordinal)
            || string.Equals(action.Title, "Open Runtime Inspector", StringComparison.Ordinal))
        {
            return AiSuggestedActionIds.OpenRuntimeInspector;
        }

        if (string.Equals(action.ActionId, AiSuggestedActionIds.BrowseBuildIdeas, StringComparison.Ordinal)
            || string.Equals(action.Title, "Browse Build Ideas", StringComparison.Ordinal))
        {
            return AiSuggestedActionIds.BrowseBuildIdeas;
        }

        return action.ActionId;
    }

    private string NormalizeQueryText()
        => string.IsNullOrWhiteSpace(_buildIdeaQueryText)
            ? string.Empty
            : _buildIdeaQueryText.Trim();

    private string NormalizeMessageText()
        => string.IsNullOrWhiteSpace(_messageText)
            ? string.Empty
            : _messageText.Trim();

    private string ResolveBuildIdeaActionQueryText(AiActionDraft draft)
    {
        string queryText = NormalizeQueryText();
        if (!string.IsNullOrWhiteSpace(queryText)
            && !string.Equals(queryText, DefaultBuildIdeaQueryText, StringComparison.Ordinal))
        {
            return queryText;
        }

        string? conversationQuery = NormalizeOptionalText(_selectedConversation?.Turns?.LastOrDefault()?.UserMessage)
            ?? NormalizeOptionalText(
                _selectedConversation?.Messages
                    .LastOrDefault(message => string.Equals(message.Role, AiConversationRoles.User, StringComparison.Ordinal))
                    ?.Content);
        if (!string.IsNullOrWhiteSpace(conversationQuery))
        {
            return conversationQuery;
        }

        string messageText = NormalizeMessageText();
        if (!string.IsNullOrWhiteSpace(messageText)
            && !string.Equals(messageText, DefaultMessageText, StringComparison.Ordinal))
        {
            return messageText;
        }

        return NormalizeOptionalText(queryText)
            ?? NormalizeOptionalText(draft.Description)
            ?? NormalizeOptionalText(draft.Title)
            ?? DefaultBuildIdeaQueryText;
    }

    private string EffectiveRouteType
        => string.IsNullOrWhiteSpace(_routeType)
            ? DefaultRouteType
            : _routeType.Trim();

    private IReadOnlyList<string> GetAvailableRoutes()
        => _status?.Routes?.Count > 0
            ? _status.Routes
            : [AiRouteTypes.Coach, AiRouteTypes.Build, AiRouteTypes.Docs, AiRouteTypes.Recap, AiRouteTypes.Chat];

    private string? NormalizeRuntimeFingerprint()
        => string.IsNullOrWhiteSpace(_runtimeFingerprint)
            ? null
            : _runtimeFingerprint.Trim();

    private string? NormalizeCharacterId()
        => string.IsNullOrWhiteSpace(_characterId)
            ? null
            : _characterId.Trim();

    private string? NormalizeRulesetId()
        => string.IsNullOrWhiteSpace(_rulesetId)
            ? null
            : _rulesetId.Trim();

    private string? NormalizeWorkspaceId()
        => string.IsNullOrWhiteSpace(_workspaceId)
            ? null
            : _workspaceId.Trim();

    private void ApplyGroundingScope(AiGroundingBundle grounding)
    {
        if (!string.IsNullOrWhiteSpace(grounding.RuntimeFingerprint))
        {
            _runtimeFingerprint = grounding.RuntimeFingerprint;
        }

        if (!string.IsNullOrWhiteSpace(grounding.CharacterId))
        {
            _characterId = grounding.CharacterId;
        }

        if (!string.IsNullOrWhiteSpace(grounding.WorkspaceId))
        {
            _workspaceId = grounding.WorkspaceId;
        }
    }

    private void ApplyConversationScope(AiConversationSnapshot conversation)
    {
        if (!string.IsNullOrWhiteSpace(conversation.RouteType))
        {
            _routeType = conversation.RouteType;
        }

        if (!string.IsNullOrWhiteSpace(conversation.RuntimeFingerprint))
        {
            _runtimeFingerprint = conversation.RuntimeFingerprint;
        }

        if (!string.IsNullOrWhiteSpace(conversation.CharacterId))
        {
            _characterId = conversation.CharacterId;
        }

        if (!string.IsNullOrWhiteSpace(conversation.WorkspaceId))
        {
            _workspaceId = conversation.WorkspaceId;
        }
    }

    private bool TryResolveActionPreviewScope(AiActionDraft draft, out string characterId, out string runtimeFingerprint, out string? workspaceId)
    {
        characterId = draft.CharacterId ?? NormalizeCharacterId() ?? string.Empty;
        runtimeFingerprint = draft.RuntimeFingerprint ?? NormalizeRuntimeFingerprint() ?? string.Empty;
        workspaceId = draft.WorkspaceId ?? NormalizeWorkspaceId();
        if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(runtimeFingerprint))
        {
            _errorMessage = "Choose a runtime and character scope before running a non-mutating action preview.";
            return false;
        }

        return true;
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
