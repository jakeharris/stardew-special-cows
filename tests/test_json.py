#!/usr/bin/env python3
"""
JSON validation tests for the SpecialCows.CP Content Patcher pack.

Checks:
  - Every .json file under SpecialCows.CP/ is valid JSON.
  - Each manifest.json has the required SMAPI/CP fields: Name, UniqueID, Version.
  - content.json has a "Format" field and a "Changes" array.
  - Each file under assets/data/ has a "Changes" array with at least one entry.

Exit codes:
  0 — all checks passed
  1 — one or more checks failed
"""

import json
import os
import pathlib
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parent.parent
CP_ROOT = REPO_ROOT / "SpecialCows.CP"

PASS = "\033[32mPASS\033[0m"
FAIL = "\033[31mFAIL\033[0m"


def label(path: pathlib.Path) -> str:
    """Return a short repo-relative label for display."""
    return str(path.relative_to(REPO_ROOT))


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def load_json(path: pathlib.Path) -> tuple[dict | list | None, str | None]:
    """Return (parsed, error_message). error_message is None on success."""
    try:
        with path.open(encoding="utf-8") as fh:
            return json.load(fh), None
    except json.JSONDecodeError as exc:
        return None, f"JSON parse error at line {exc.lineno}, col {exc.colno}: {exc.msg}"
    except OSError as exc:
        return None, f"IO error: {exc}"


def check(condition: bool, message: str, failures: list[str]) -> bool:
    """Record a failure message if condition is False. Returns condition."""
    if not condition:
        failures.append(message)
    return condition


# ---------------------------------------------------------------------------
# Per-file structural validators
# ---------------------------------------------------------------------------

def validate_manifest(data: dict, path: pathlib.Path, failures: list[str]) -> None:
    required = ["Name", "UniqueID", "Version"]
    for field in required:
        check(
            field in data,
            f"  manifest.json missing required field: '{field}'",
            failures,
        )


def validate_content_json(data: dict, path: pathlib.Path, failures: list[str]) -> None:
    check("Format" in data, "  content.json missing 'Format' field", failures)
    has_changes = check(
        "Changes" in data, "  content.json missing 'Changes' field", failures
    )
    if has_changes:
        check(
            isinstance(data["Changes"], list),
            "  content.json: 'Changes' must be an array",
            failures,
        )


def validate_data_file(data: dict, path: pathlib.Path, failures: list[str]) -> None:
    has_changes = check(
        "Changes" in data,
        f"  {label(path)}: missing top-level 'Changes' array",
        failures,
    )
    if has_changes:
        check(
            isinstance(data["Changes"], list),
            f"  {label(path)}: 'Changes' must be an array",
            failures,
        )
        check(
            len(data["Changes"]) >= 1,
            f"  {label(path)}: 'Changes' array must have at least one entry",
            failures,
        )


# ---------------------------------------------------------------------------
# Main runner
# ---------------------------------------------------------------------------

def run_tests() -> int:
    json_files = sorted(CP_ROOT.rglob("*.json"))

    if not json_files:
        print(f"[{FAIL}] No .json files found under {label(CP_ROOT)}")
        return 1

    overall_failures: list[str] = []
    results: list[tuple[str, bool, list[str]]] = []

    for path in json_files:
        file_failures: list[str] = []

        # ── 1. Parse check ──────────────────────────────────────────────────
        data, parse_error = load_json(path)
        if parse_error:
            file_failures.append(f"  {parse_error}")
            results.append((label(path), False, file_failures))
            overall_failures.extend(file_failures)
            continue  # no point running structural checks on broken JSON

        # ── 2. Structural checks ────────────────────────────────────────────
        name = path.name
        relative = path.relative_to(CP_ROOT)
        parts = relative.parts  # e.g. ('assets', 'data', 'items.json')

        if name == "manifest.json":
            validate_manifest(data, path, file_failures)
        elif name == "content.json":
            validate_content_json(data, path, file_failures)
        elif len(parts) >= 2 and parts[0] == "assets" and parts[1] == "data":
            validate_data_file(data, path, file_failures)

        passed = len(file_failures) == 0
        results.append((label(path), passed, file_failures))
        overall_failures.extend(file_failures)

    # ── Print results ────────────────────────────────────────────────────────
    print()
    print("JSON Validation Results")
    print("=" * 60)
    for file_label, passed, file_failures in results:
        status = PASS if passed else FAIL
        print(f"  [{status}]  {file_label}")
        for msg in file_failures:
            print(f"\033[33m{msg}\033[0m")
    print("=" * 60)

    total = len(results)
    passed_count = sum(1 for _, ok, _ in results if ok)
    failed_count = total - passed_count

    if failed_count == 0:
        print(f"  All {total} file(s) passed.\n")
        return 0
    else:
        print(f"  {passed_count}/{total} passed, {failed_count} failed.\n")
        return 1


if __name__ == "__main__":
    sys.exit(run_tests())
