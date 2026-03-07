using Chummer.Api.Owners;
using Chummer.Application.AI;
using Chummer.Application.Owners;
using Chummer.Contracts.AI;
using Microsoft.AspNetCore.Mvc;

namespace Chummer.Api.Endpoints;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/ai/status", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.GetStatus(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/providers", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListProviders(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/provider-health", (string? providerId, string? routeType, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiApiResult<IReadOnlyList<AiProviderHealthProjection>> result = aiGatewayService.ListProviderHealth(ownerContextAccessor.Current);
            if (!result.IsImplemented || (string.IsNullOrWhiteSpace(providerId) && string.IsNullOrWhiteSpace(routeType)))
            {
                return ToResult(result);
            }

            IReadOnlyList<AiProviderHealthProjection> filtered = result.Payload?
                .Where(item =>
                    (string.IsNullOrWhiteSpace(providerId) || string.Equals(item.ProviderId, providerId, StringComparison.Ordinal))
                    && (string.IsNullOrWhiteSpace(routeType) || item.AllowedRouteTypes.Contains(routeType, StringComparer.Ordinal)))
                .ToArray() ?? [];
            return Results.Ok(filtered);
        });

        app.MapGet("/api/ai/conversations", (string? conversationId, string? routeType, string? characterId, string? runtimeFingerprint, string? workspaceId, int? maxCount, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListConversations(
                ownerContextAccessor.Current,
                new AiConversationCatalogQuery(conversationId, routeType, characterId, runtimeFingerprint, maxCount ?? 20, workspaceId))));

        app.MapGet("/api/ai/conversation-audits", (string? conversationId, string? routeType, string? characterId, string? runtimeFingerprint, string? workspaceId, int? maxCount, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListConversationAudits(
                ownerContextAccessor.Current,
                new AiConversationCatalogQuery(conversationId, routeType, characterId, runtimeFingerprint, maxCount ?? 20, workspaceId))));

        app.MapGet("/api/ai/tools", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListTools(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/retrieval-corpora", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListRetrievalCorpora(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/route-policies", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListRoutePolicies(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/route-budgets", ([FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.ListRouteBudgets(ownerContextAccessor.Current)));

        app.MapGet("/api/ai/route-budget-statuses", (string? routeType, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiApiResult<IReadOnlyList<AiRouteBudgetStatusProjection>> result = aiGatewayService.ListRouteBudgetStatuses(ownerContextAccessor.Current);
            if (!result.IsImplemented || string.IsNullOrWhiteSpace(routeType))
            {
                return ToResult(result);
            }

            IReadOnlyList<AiRouteBudgetStatusProjection> filtered = result.Payload?
                .Where(status => string.Equals(status.RouteType, routeType, StringComparison.Ordinal))
                .ToArray() ?? [];
            return Results.Ok(filtered);
        });

        app.MapGet("/api/ai/prompts", (string? routeType, string? personaId, int? maxCount, [FromServices] IAiPromptRegistryService promptRegistryService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            IEnumerable<AiPromptDescriptor> items = AiGatewayDefaults.CreateRoutePolicies()
                .Select(policy => promptRegistryService.GetPrompt(ownerContextAccessor.Current, policy.RouteType))
                .OfType<AiPromptDescriptor>();

            if (!string.IsNullOrWhiteSpace(routeType))
            {
                items = items.Where(item => string.Equals(item.RouteType, routeType, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(personaId))
            {
                items = items.Where(item => string.Equals(item.PersonaId, personaId, StringComparison.Ordinal));
            }

            AiPromptDescriptor[] materialized = items
                .Take(Math.Max(1, maxCount ?? 20))
                .ToArray();

            return Results.Ok(new AiPromptCatalog(materialized, materialized.Length));
        });

        app.MapGet("/api/ai/prompts/{promptId}", (string promptId, [FromServices] IAiPromptRegistryService promptRegistryService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiPromptDescriptor? prompt = promptRegistryService.GetPrompt(ownerContextAccessor.Current, promptId);
            return prompt is null
                ? Results.NotFound(new
                {
                    error = "ai_prompt_not_found",
                    promptId
                })
                : Results.Ok(prompt);
        });

        app.MapGet("/api/ai/build-ideas", (string? routeType, string? queryText, string? rulesetId, int? maxCount, [FromServices] IBuildIdeaCardCatalogService buildIdeaCardCatalogService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            string effectiveRouteType = string.IsNullOrWhiteSpace(routeType)
                ? AiRouteTypes.Build
                : routeType;
            IReadOnlyList<BuildIdeaCard> items = buildIdeaCardCatalogService.SearchBuildIdeas(
                ownerContextAccessor.Current,
                effectiveRouteType,
                queryText ?? string.Empty,
                rulesetId,
                maxCount ?? 10);
            return Results.Ok(new AiBuildIdeaCatalog(items, items.Count));
        });

        app.MapGet("/api/ai/build-ideas/{ideaId}", (string ideaId, [FromServices] IBuildIdeaCardCatalogService buildIdeaCardCatalogService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            BuildIdeaCard? card = buildIdeaCardCatalogService.GetBuildIdea(ownerContextAccessor.Current, ideaId);
            return card is null
                ? Results.NotFound(new
                {
                    error = "ai_build_idea_not_found",
                    ideaId
                })
                : Results.Ok(card);
        });

        app.MapGet("/api/ai/hub/projects", (string? queryText, string? type, string? rulesetId, int? maxCount, [FromServices] IAiHubProjectSearchService hubProjectSearchService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            Results.Ok(hubProjectSearchService.SearchProjects(
                ownerContextAccessor.Current,
                new AiHubProjectSearchQuery(
                    QueryText: queryText ?? string.Empty,
                    Type: type,
                    RulesetId: rulesetId,
                    MaxCount: maxCount ?? 10))));

        app.MapGet("/api/ai/hub/projects/{kind}/{itemId}", (string kind, string itemId, string? rulesetId, [FromServices] IAiHubProjectSearchService hubProjectSearchService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiHubProjectDetailProjection? detail = hubProjectSearchService.GetProjectDetail(ownerContextAccessor.Current, kind, itemId, rulesetId);
            return detail is null
                ? Results.NotFound(new
                {
                    error = "ai_hub_project_not_found",
                    kind,
                    itemId,
                    rulesetId
                })
                : Results.Ok(detail);
        });

        app.MapGet("/api/ai/explain", (string? runtimeFingerprint, string? characterId, string? capabilityId, string? explainEntryId, string? rulesetId, [FromServices] IAiExplainService aiExplainService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiExplainValueProjection? detail = aiExplainService.GetExplainValue(
                ownerContextAccessor.Current,
                new AiExplainValueQuery(runtimeFingerprint, characterId, capabilityId, explainEntryId, rulesetId));
            return detail is null
                ? Results.NotFound(new
                {
                    error = "ai_explain_not_found",
                    runtimeFingerprint,
                    characterId,
                    capabilityId,
                    explainEntryId,
                    rulesetId
                })
                : Results.Ok(detail);
        });

        app.MapGet("/api/ai/runtime/{runtimeFingerprint}/summary", (string runtimeFingerprint, string? rulesetId, [FromServices] IAiDigestService aiDigestService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiRuntimeSummaryProjection? summary = aiDigestService.GetRuntimeSummary(ownerContextAccessor.Current, runtimeFingerprint, rulesetId);
            return summary is null
                ? Results.NotFound(new
                {
                    error = "ai_runtime_summary_not_found",
                    runtimeFingerprint,
                    rulesetId
                })
                : Results.Ok(summary);
        });

        app.MapGet("/api/ai/characters/{characterId}/digest", (string characterId, [FromServices] IAiDigestService aiDigestService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiCharacterDigestProjection? digest = aiDigestService.GetCharacterDigest(ownerContextAccessor.Current, characterId);
            return digest is null
                ? Results.NotFound(new
                {
                    error = "ai_character_digest_not_found",
                    characterId
                })
                : Results.Ok(digest);
        });

        app.MapGet("/api/ai/session/characters/{characterId}/digest", (string characterId, [FromServices] IAiDigestService aiDigestService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiSessionDigestProjection? digest = aiDigestService.GetSessionDigest(ownerContextAccessor.Current, characterId);
            return digest is null
                ? Results.NotFound(new
                {
                    error = "ai_session_digest_not_found",
                    characterId
                })
                : Results.Ok(digest);
        });

        app.MapPost("/api/ai/preview/karma-spend", (AiSpendPlanPreviewRequest? request, [FromServices] IAiActionPreviewService actionPreviewService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiActionPreviewReceipt? receipt = actionPreviewService.PreviewKarmaSpend(ownerContextAccessor.Current, request);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "ai_karma_preview_not_found",
                    characterId = request?.CharacterId,
                    runtimeFingerprint = request?.RuntimeFingerprint
                })
                : Results.Ok(receipt);
        });

        app.MapPost("/api/ai/preview/nuyen-spend", (AiSpendPlanPreviewRequest? request, [FromServices] IAiActionPreviewService actionPreviewService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiActionPreviewReceipt? receipt = actionPreviewService.PreviewNuyenSpend(ownerContextAccessor.Current, request);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "ai_nuyen_preview_not_found",
                    characterId = request?.CharacterId,
                    runtimeFingerprint = request?.RuntimeFingerprint
                })
                : Results.Ok(receipt);
        });

        app.MapPost("/api/ai/apply-preview", (AiApplyPreviewRequest? request, [FromServices] IAiActionPreviewService actionPreviewService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiActionPreviewReceipt? receipt = actionPreviewService.CreateApplyPreview(ownerContextAccessor.Current, request);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "ai_apply_preview_not_found",
                    characterId = request?.CharacterId,
                    runtimeFingerprint = request?.RuntimeFingerprint
                })
                : Results.Ok(receipt);
        });

        app.MapPost("/api/ai/preview/{routeType}", (string routeType, AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            AiGatewayDefaults.IsKnownRouteType(routeType)
                ? ToResult(aiGatewayService.PreviewTurn(ownerContextAccessor.Current, routeType, request))
                : Results.BadRequest(new
                {
                    error = "unknown_ai_route_type",
                    routeType
                }));

        app.MapGet("/api/ai/conversations/{conversationId}", (string conversationId, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.GetConversation(ownerContextAccessor.Current, conversationId)));

        app.MapPost("/api/ai/chat", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendChatTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/coach", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendCoachTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/coach/query", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendCoachTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/build", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendBuildTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/build-lab/query", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendBuildTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/docs/query", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendDocsTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/media/portrait/prompt", (AiPortraitPromptRequest? request, [FromServices] IAiPortraitPromptService portraitPromptService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiPortraitPromptProjection? prompt = request is null
                ? null
                : portraitPromptService.CreatePortraitPrompt(ownerContextAccessor.Current, request);
            return prompt is null
                ? Results.NotFound(new
                {
                    error = "ai_portrait_prompt_not_found",
                    characterId = request?.CharacterId,
                    runtimeFingerprint = request?.RuntimeFingerprint
                })
                : Results.Ok(prompt);
        });

        app.MapPost("/api/ai/history/drafts", (AiHistoryDraftRequest? request, [FromServices] IAiHistoryDraftService historyDraftService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            AiHistoryDraftProjection? draft = request is null
                ? null
                : historyDraftService.CreateHistoryDraft(ownerContextAccessor.Current, request);
            return draft is null
                ? Results.NotFound(new
                {
                    error = "ai_history_draft_not_found",
                    characterId = request?.CharacterId,
                    runtimeFingerprint = request?.RuntimeFingerprint,
                    sessionId = request?.SessionId,
                    transcriptId = request?.TranscriptId
                })
                : Results.Ok(draft);
        });

        app.MapPost("/api/ai/media/queue", (AiMediaQueueRequest? request, [FromServices] IAiMediaQueueService mediaQueueService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
        {
            if (request is null || !IsKnownMediaJobType(request.JobType))
            {
                return Results.BadRequest(new
                {
                    error = "unknown_ai_media_job_type",
                    jobType = request?.JobType
                });
            }

            AiMediaQueueReceipt? receipt = mediaQueueService.QueueMediaJob(ownerContextAccessor.Current, request);
            return receipt is null
                ? Results.NotFound(new
                {
                    error = "ai_media_queue_not_found",
                    characterId = request.CharacterId,
                    runtimeFingerprint = request.RuntimeFingerprint,
                    jobType = request.JobType
                })
                : Results.Ok(receipt);
        });

        app.MapPost("/api/ai/media/portrait", (AiMediaJobRequest? request, [FromServices] IAiMediaJobService mediaJobService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(mediaJobService.QueuePortraitJob(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/media/dossier", (AiMediaJobRequest? request, [FromServices] IAiMediaJobService mediaJobService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(mediaJobService.QueueDossierJob(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/media/route-video", (AiMediaJobRequest? request, [FromServices] IAiMediaJobService mediaJobService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(mediaJobService.QueueRouteVideoJob(ownerContextAccessor.Current, request)));

        app.MapGet("/api/ai/media/assets", (string? assetKind, string? characterId, string? state, int? maxCount, [FromServices] IAiMediaAssetCatalogService mediaAssetCatalogService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(mediaAssetCatalogService.ListMediaAssets(
                ownerContextAccessor.Current,
                new AiMediaAssetQuery(assetKind, characterId, state, maxCount ?? 20))));

        app.MapGet("/api/ai/media/assets/{assetId}", (string assetId, [FromServices] IAiMediaAssetCatalogService mediaAssetCatalogService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(mediaAssetCatalogService.GetMediaAsset(ownerContextAccessor.Current, assetId)));

        app.MapGet("/api/ai/approvals", (string? state, string? targetKind, int? maxCount, [FromServices] IAiApprovalOrchestrator approvalOrchestrator, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(approvalOrchestrator.ListApprovals(
                ownerContextAccessor.Current,
                new AiApprovalQuery(state, targetKind, maxCount ?? 20))));

        app.MapPost("/api/ai/approvals", (AiApprovalSubmitRequest? request, [FromServices] IAiApprovalOrchestrator approvalOrchestrator, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(approvalOrchestrator.SubmitApproval(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/approvals/{approvalId}/resolve", (string approvalId, AiApprovalResolveRequest? request, [FromServices] IAiApprovalOrchestrator approvalOrchestrator, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(approvalOrchestrator.ResolveApproval(ownerContextAccessor.Current, approvalId, request)));

        app.MapPost("/api/ai/session/transcripts", (AiTranscriptSubmissionRequest? request, [FromServices] ITranscriptProvider transcriptProvider, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(transcriptProvider.SubmitTranscript(ownerContextAccessor.Current, request)));

        app.MapGet("/api/ai/session/transcripts/{transcriptId}", (string transcriptId, [FromServices] ITranscriptProvider transcriptProvider, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(transcriptProvider.GetTranscript(ownerContextAccessor.Current, transcriptId)));

        app.MapGet("/api/ai/session/recap-drafts", (string? sessionId, int? maxCount, [FromServices] IAiRecapDraftService recapDraftService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(recapDraftService.ListRecapDrafts(
                ownerContextAccessor.Current,
                new AiRecapDraftQuery(sessionId, maxCount ?? 20))));

        app.MapPost("/api/ai/session/recap-drafts", (AiRecapDraftRequest? request, [FromServices] IAiRecapDraftService recapDraftService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(recapDraftService.CreateRecapDraft(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/recap", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendRecapTurn(ownerContextAccessor.Current, request)));

        app.MapPost("/api/ai/session/recap", (AiConversationTurnRequest? request, [FromServices] IAiGatewayService aiGatewayService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(aiGatewayService.SendRecapTurn(ownerContextAccessor.Current, request)));

        app.MapGet("/api/ai/admin/evals", (string? routeType, int? maxCount, [FromServices] IAiEvaluationService evaluationService, [FromServices] IOwnerContextAccessor ownerContextAccessor) =>
            ToResult(evaluationService.ListEvaluations(
                ownerContextAccessor.Current,
                new AiEvaluationQuery(routeType, maxCount ?? 20))));

        return app;
    }

    private static IResult ToResult<T>(AiApiResult<T> result)
    {
        if (result.QuotaExceeded is not null)
        {
            return Results.Json(result.QuotaExceeded, statusCode: StatusCodes.Status429TooManyRequests);
        }

        if (result.IsImplemented)
        {
            return Results.Ok(result.Payload);
        }

        AiNotImplementedReceipt receipt = result.NotImplemented
            ?? throw new InvalidOperationException("AI API result was not implemented but did not include a receipt.");
        return Results.Json(receipt, statusCode: StatusCodes.Status501NotImplemented);
    }

    private static bool IsKnownMediaJobType(string? jobType)
        => string.Equals(jobType, AiMediaJobTypes.Portrait, StringComparison.OrdinalIgnoreCase)
            || string.Equals(jobType, AiMediaJobTypes.Dossier, StringComparison.OrdinalIgnoreCase)
            || string.Equals(jobType, AiMediaJobTypes.RouteVideo, StringComparison.OrdinalIgnoreCase);
}
