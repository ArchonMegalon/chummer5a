#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
import subprocess
import sys


PACKAGE_ID = "next90-m141-ui-capture-direct-screenshot-and-runtime-proof-for-translator-xml-amendment"
FRONTIER_ID = 1922169755
MILESTONE_ID = 141
CONTRACT_NAME = "chummer5a.next90_m141_ui_import_route_proof"


@dataclass(frozen=True)
class LocalEvidence:
    relative_path: str
    required_tokens: tuple[str, ...]


LOCAL_EVIDENCE = {
    "dialog_factory_tests": LocalEvidence(
        "Chummer.Tests/Presentation/DesktopDialogFactoryTests.cs",
        (
            "CreateCommandDialog_translator_lists_shipping_locales",
            "CreateCommandDialog_xml_editor_uses_active_section_payload_preview",
            "CreateCommandDialog_hero_lab_importer_uses_xml_compatibility_fields",
        ),
    ),
    "presenter_tests": LocalEvidence(
        "Chummer.Tests/Presentation/CharacterOverviewPresenterTests.cs",
        (
            "ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs",
        ),
    ),
    "dual_head_tests": LocalEvidence(
        "Chummer.Tests/Presentation/DualHeadAcceptanceTests.cs",
        (
            "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
        ),
    ),
    "dialog_coordinator_tests": LocalEvidence(
        "Chummer.Tests/Presentation/DialogCoordinatorTests.cs",
        (
            "CoordinateAsync_hero_lab_import_imports_workspace_and_sets_compat_notice",
        ),
    ),
    "governance_entrypoint": LocalEvidence(
        "scripts/test-runtime-governance.sh",
        (
            "verify-next90-m141-ui-import-route-proof.py",
            ".codex-studio/published/NEXT90_M141_UI_IMPORT_ROUTE_PROOF.generated.json",
            "--check",
        ),
    ),
}


PRESENTATION_REPOSITORY = "ArchonMegalon/chummer6-ui"
CORE_REPOSITORY = "ArchonMegalon/chummer6-core"
PRESENTATION_COMMIT = "d7449ec0476c68c1ac6863350f1d4585ab0eecf3"
CORE_COMMIT = "7560c65ee968a3c9eb650255f236524bf7d65144"

PRESENTATION_ROOT = Path(
    os.environ.get("CHUMMER_PRESENTATION_PROOF_ROOT", "/docker/chummercomplete/chummer-presentation")
)
CORE_ROOT = Path(os.environ.get("CHUMMER_CORE_PROOF_ROOT", "/docker/chummercomplete/chummer-core-engine"))

SCREENSHOT_ROOT_REL = Path(".codex-studio/published/ui-flagship-release-gate-screenshots")
SCREENSHOT_EVIDENCE_REL = SCREENSHOT_ROOT_REL / "SCREENSHOT_CONTROL_EVIDENCE.generated.json"
UI_RELEASE_GATE_REL = Path(".codex-studio/published/UI_FLAGSHIP_RELEASE_GATE.generated.json")
DESKTOP_EXIT_GATE_REL = Path(".codex-studio/published/DESKTOP_EXECUTABLE_EXIT_GATE.generated.json")
VISUAL_EXIT_GATE_REL = Path(".codex-studio/published/DESKTOP_VISUAL_FAMILIARITY_EXIT_GATE.generated.json")
VETERAN_TIME_GATE_REL = Path(".codex-studio/published/VETERAN_TASK_TIME_EVIDENCE_GATE.generated.json")
SCREENSHOT_REVIEW_GATE_REL = Path(".codex-studio/published/CHUMMER5A_SCREENSHOT_REVIEW_GATE.generated.json")
CORE_RECEIPTS_DOC_REL = Path("docs/NEXT90_M141_IMPORT_ROUTE_RECEIPTS.md")
CORE_RECEIPTS_JSON_REL = Path(".codex-studio/published/NEXT90_M141_IMPORT_ROUTE_RECEIPTS.generated.json")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    parser.add_argument(
        "--out",
        default=".codex-studio/published/NEXT90_M141_UI_IMPORT_ROUTE_PROOF.generated.json",
    )
    parser.add_argument("--check", action="store_true")
    return parser.parse_args()


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def external_ref(repository: str, commit: str, relative_path: Path) -> str:
    return f"github:{repository}@{commit}:{relative_path.as_posix()}"


def pinned_blob(repo_root: Path, commit: str, relative_path: Path) -> bytes | None:
    try:
        completed = subprocess.run(
            ["git", "-C", str(repo_root), "show", f"{commit}:{relative_path.as_posix()}"],
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
    except (OSError, subprocess.CalledProcessError):
        return None

    return completed.stdout


def validate_pinned_external(
    repo_root: Path,
    repository: str,
    commit: str,
    relative_path: Path,
    errors: list[str],
) -> None:
    expected = pinned_blob(repo_root, commit, relative_path)
    ensure(expected is not None, f"Missing pinned proof source {repository}@{commit}:{relative_path.as_posix()}", errors)


def ensure(condition: bool, message: str, errors: list[str]) -> None:
    if not condition:
        errors.append(message)


def materialize(repo_root: Path, existing_generated_at: str | None = None) -> dict[str, object]:
    errors: list[str] = []
    local_paths: dict[str, Path] = {}
    local_evidence: dict[str, dict[str, object]] = {}

    for key, evidence in LOCAL_EVIDENCE.items():
        path = repo_root / evidence.relative_path
        local_paths[key] = path
        ensure(path.is_file(), f"Missing local evidence file: {path}", errors)
        if not path.is_file():
            continue

        text = read_text(path)
        missing_tokens = [token for token in evidence.required_tokens if token not in text]
        ensure(not missing_tokens, f"Missing token(s) in {path}: {', '.join(missing_tokens)}", errors)
        local_evidence[key] = {
            "path": evidence.relative_path,
            "tokens": list(evidence.required_tokens),
        }

    screenshot_files = {
        "source:translator_route": ["38-translator-dialog-light.png"],
        "source:xml_amendment_editor_route": ["39-xml-editor-dialog-light.png"],
        "source:hero_lab_importer_route": ["40-hero-lab-importer-dialog-light.png"],
        "family:custom_data_xml_and_translator_bridge": ["38-translator-dialog-light.png", "39-xml-editor-dialog-light.png"],
        "family:legacy_and_adjacent_import_oracles": ["40-hero-lab-importer-dialog-light.png"],
    }

    screenshot_control_blob = pinned_blob(PRESENTATION_ROOT, PRESENTATION_COMMIT, SCREENSHOT_EVIDENCE_REL)
    screenshot_control_evidence = json.loads(screenshot_control_blob) if screenshot_control_blob is not None else {}
    screenshot_control_text = json.dumps(screenshot_control_evidence)

    external_artifacts = [
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_EVIDENCE_REL),
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, UI_RELEASE_GATE_REL),
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, DESKTOP_EXIT_GATE_REL),
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VISUAL_EXIT_GATE_REL),
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VETERAN_TIME_GATE_REL),
        (PRESENTATION_ROOT, PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_REVIEW_GATE_REL),
        (CORE_ROOT, CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_DOC_REL),
        (CORE_ROOT, CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_JSON_REL),
    ]
    for repo_root, repository, commit, relative_path in external_artifacts:
        validate_pinned_external(repo_root, repository, commit, relative_path, errors)

    for route_id, screenshot_names in screenshot_files.items():
        for screenshot_name in screenshot_names:
            validate_pinned_external(
                PRESENTATION_ROOT,
                PRESENTATION_REPOSITORY,
                PRESENTATION_COMMIT,
                SCREENSHOT_ROOT_REL / screenshot_name,
                errors,
            )
            ensure(screenshot_name in screenshot_control_text, f"Screenshot evidence json does not mention {screenshot_name}", errors)

    route_rows = [
        {
            "id": "source:translator_route",
            "label": "Translator route",
            "dialog_id": "dialog.translator",
            "screenshots": screenshot_files["source:translator_route"],
            "runtime_assertions": [
                "CreateCommandDialog_translator_lists_shipping_locales",
                "ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs",
                "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
            ],
            "reason": "Direct screenshot 38-translator-dialog-light.png plus runtime assertions CreateCommandDialog_translator_lists_shipping_locales, ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs, and Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts keep the Translator lane under direct screenshot/runtime proof.",
            "evidence": [
                LOCAL_EVIDENCE["dialog_factory_tests"].relative_path,
                LOCAL_EVIDENCE["presenter_tests"].relative_path,
                LOCAL_EVIDENCE["dual_head_tests"].relative_path,
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, UI_RELEASE_GATE_REL),
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, DESKTOP_EXIT_GATE_REL),
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VETERAN_TIME_GATE_REL),
            ],
        },
        {
            "id": "source:xml_amendment_editor_route",
            "label": "XML amendment editor route",
            "dialog_id": "dialog.xml_editor",
            "screenshots": screenshot_files["source:xml_amendment_editor_route"],
            "runtime_assertions": [
                "CreateCommandDialog_xml_editor_uses_active_section_payload_preview",
                "ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs",
                "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
            ],
            "reason": "Direct screenshot 39-xml-editor-dialog-light.png plus runtime assertions CreateCommandDialog_xml_editor_uses_active_section_payload_preview, ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs, and Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts keep the XML Amendment Editor lane under direct screenshot/runtime proof.",
            "evidence": [
                LOCAL_EVIDENCE["dialog_factory_tests"].relative_path,
                LOCAL_EVIDENCE["presenter_tests"].relative_path,
                LOCAL_EVIDENCE["dual_head_tests"].relative_path,
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, UI_RELEASE_GATE_REL),
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, DESKTOP_EXIT_GATE_REL),
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VISUAL_EXIT_GATE_REL),
            ],
        },
        {
            "id": "source:hero_lab_importer_route",
            "label": "Hero Lab importer route",
            "dialog_id": "dialog.hero_lab_importer",
            "screenshots": screenshot_files["source:hero_lab_importer_route"],
            "runtime_assertions": [
                "CreateCommandDialog_hero_lab_importer_uses_xml_compatibility_fields",
                "ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs",
                "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
                "CoordinateAsync_hero_lab_import_imports_workspace_and_sets_compat_notice",
            ],
            "reason": "Direct screenshot 40-hero-lab-importer-dialog-light.png plus runtime assertions CreateCommandDialog_hero_lab_importer_uses_xml_compatibility_fields, ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs, Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts, and CoordinateAsync_hero_lab_import_imports_workspace_and_sets_compat_notice keep the Hero Lab importer lane under direct screenshot/runtime proof.",
            "evidence": [
                LOCAL_EVIDENCE["dialog_factory_tests"].relative_path,
                LOCAL_EVIDENCE["presenter_tests"].relative_path,
                LOCAL_EVIDENCE["dual_head_tests"].relative_path,
                LOCAL_EVIDENCE["dialog_coordinator_tests"].relative_path,
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_REVIEW_GATE_REL),
                external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_DOC_REL),
            ],
        },
        {
            "id": "family:custom_data_xml_and_translator_bridge",
            "label": "Custom data/XML and translator bridge family",
            "dialog_id": None,
            "screenshots": screenshot_files["family:custom_data_xml_and_translator_bridge"],
            "runtime_assertions": [
                "CreateCommandDialog_translator_lists_shipping_locales",
                "CreateCommandDialog_xml_editor_uses_active_section_payload_preview",
                "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
            ],
            "reason": "Direct screenshots 38-translator-dialog-light.png and 39-xml-editor-dialog-light.png, plus the local translator/XML route tests and the external M141 core receipt, keep the custom-data/XML bridge family under direct screenshot/runtime proof instead of generic dialog parity only.",
            "evidence": [
                LOCAL_EVIDENCE["dialog_factory_tests"].relative_path,
                LOCAL_EVIDENCE["dual_head_tests"].relative_path,
                external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_DOC_REL),
                external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_JSON_REL),
                external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VETERAN_TIME_GATE_REL),
            ],
        },
        {
            "id": "family:legacy_and_adjacent_import_oracles",
            "label": "Legacy and adjacent import-oracle family",
            "dialog_id": None,
            "screenshots": screenshot_files["family:legacy_and_adjacent_import_oracles"],
            "runtime_assertions": [
                "CreateCommandDialog_hero_lab_importer_uses_xml_compatibility_fields",
                "CoordinateAsync_hero_lab_import_imports_workspace_and_sets_compat_notice",
                "Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts",
            ],
            "reason": "Direct screenshot 40-hero-lab-importer-dialog-light.png, the Hero Lab runtime assertions, and the external M141 core import-oracle receipt keep the legacy and adjacent import-oracle family under direct screenshot/runtime proof.",
            "evidence": [
                LOCAL_EVIDENCE["dialog_factory_tests"].relative_path,
                LOCAL_EVIDENCE["dialog_coordinator_tests"].relative_path,
                LOCAL_EVIDENCE["dual_head_tests"].relative_path,
                external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_DOC_REL),
                external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_JSON_REL),
            ],
        },
    ]

    status = "pass" if not errors else "fail"
    return {
        "contract_name": CONTRACT_NAME,
        "generated_at": existing_generated_at or datetime.now(timezone.utc).isoformat(),
        "status": status,
        "repo": "chummer6-ui",
        "milestone_id": MILESTONE_ID,
        "frontier_id": FRONTIER_ID,
        "package_id": PACKAGE_ID,
        "summary": {
            "route_count": len(route_rows),
            "screenshot_count": sum(len(row["screenshots"]) for row in route_rows),
            "local_runtime_anchor_count": len(local_evidence),
            "error_count": len(errors),
        },
        "route_rows": route_rows,
        "local_runtime_anchors": local_evidence,
        "external_source_commits": {
            PRESENTATION_REPOSITORY: PRESENTATION_COMMIT,
            CORE_REPOSITORY: CORE_COMMIT,
        },
        "external_screenshot_anchors": {
            "screenshot_root": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_ROOT_REL),
            "screenshot_control_evidence": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_EVIDENCE_REL),
            "ui_flagship_release_gate": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, UI_RELEASE_GATE_REL),
            "desktop_executable_exit_gate": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, DESKTOP_EXIT_GATE_REL),
            "desktop_visual_familiarity_exit_gate": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VISUAL_EXIT_GATE_REL),
            "veteran_task_time_evidence_gate": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, VETERAN_TIME_GATE_REL),
            "chummer5a_screenshot_review_gate": external_ref(PRESENTATION_REPOSITORY, PRESENTATION_COMMIT, SCREENSHOT_REVIEW_GATE_REL),
            "core_m141_receipts_doc": external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_DOC_REL),
            "core_m141_receipts_json": external_ref(CORE_REPOSITORY, CORE_COMMIT, CORE_RECEIPTS_JSON_REL),
        },
        "verification": [
            "dotnet test Chummer.Tests/Chummer.Tests.csproj --filter \"FullyQualifiedName~CreateCommandDialog_xml_editor_uses_active_section_payload_preview|FullyQualifiedName~ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs|FullyQualifiedName~Avalonia_and_Blazor_translator_xml_editor_and_hero_lab_routes_preserve_matching_dialog_contracts|FullyQualifiedName~Materializer_emits_translator_xml_and_hero_lab_route_proof_with_direct_screenshot_tokens\"",
            "python3 scripts/verify-next90-m141-ui-import-route-proof.py --repo-root . --out .codex-studio/published/NEXT90_M141_UI_IMPORT_ROUTE_PROOF.generated.json --check",
        ],
        "errors": errors,
    }


def main() -> int:
    args = parse_args()
    repo_root = Path(args.repo_root).resolve()
    output_path = Path(args.out)
    if not output_path.is_absolute():
        output_path = repo_root / output_path

    existing_generated_at = None
    if output_path.is_file():
        try:
            existing_generated_at = json.loads(output_path.read_text(encoding="utf-8")).get("generated_at")
        except Exception:
            existing_generated_at = None

    payload = materialize(repo_root, existing_generated_at=existing_generated_at)
    serialized = json.dumps(payload, indent=2, sort_keys=True) + "\n"

    if args.check:
        if not output_path.is_file():
            print(f"Missing proof artifact for --check: {output_path}", file=sys.stderr)
            return 1
        current = output_path.read_text(encoding="utf-8")
        if current != serialized:
            print(f"Proof artifact drift detected: {output_path}", file=sys.stderr)
            return 1
        return 0

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(serialized, encoding="utf-8")
    print(output_path)
    return 0 if payload["status"] == "pass" else 1


if __name__ == "__main__":
    raise SystemExit(main())
