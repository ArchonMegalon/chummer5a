#!/usr/bin/env bash
set -euo pipefail

dotnet test Chummer.Tests/Chummer.Tests.csproj \
  -c Release \
  -f net10.0 \
  -p:TargetFramework=net10.0 \
  --filter "(FullyQualifiedName~RuntimeInspectorServiceTests|FullyQualifiedName~RuleProfileApplicationServiceTests|FullyQualifiedName~BuildKitRegistryServiceTests|FullyQualifiedName~NpcVaultRegistryServiceTests|FullyQualifiedName~HubProjectCompatibilityServiceTests|FullyQualifiedName~HubInstallPreviewServiceTests|FullyQualifiedName~HubWebComponentTests|FullyQualifiedName~DesktopDialogFactoryTests|FullyQualifiedName~MigrationComplianceTests|FullyQualifiedName~CreateCommandDialog_xml_editor_uses_active_section_payload_preview|FullyQualifiedName~ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs|FullyQualifiedName~Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts|FullyQualifiedName~Materializer_emits_translator_xml_and_hero_lab_route_proof_with_direct_screenshot_tokens)"

python3 scripts/verify-next90-m141-ui-import-route-proof.py \
  --repo-root . \
  --out .codex-studio/published/NEXT90_M141_UI_IMPORT_ROUTE_PROOF.generated.json \
  --check
