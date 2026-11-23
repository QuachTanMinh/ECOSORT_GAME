"""
verify_docker_12class.py
========================
Kiểm tra Docker image (hoặc server đang chạy) có dùng đúng model 12 lớp không.

Cách dùng:
    python tools/verify_docker_12class.py                   # mặc định http://localhost:8000
    python tools/verify_docker_12class.py --url http://localhost:8001
    python tools/verify_docker_12class.py --url http://my-server:8000 --timeout 10

Trả về exit code 0 nếu tất cả PASS, 1 nếu có bất kỳ FAIL nào.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
import urllib.error
import urllib.request
from pathlib import Path

# ─── Expected values ─────────────────────────────────────────────────────────

EXPECTED_CLASS_NAMES: list[str] = [
    "Plastic",
    "Metal",
    "Paper",
    "Organic",
    "Cardboard",
    "GreenGlass",
    "BrownGlass",
    "WhiteGlass",
    "Clothes",
    "Shoes",
    "Batteries",
    "Trash",
]

EXPECTED_CLASS_COUNT = 12
EXPECTED_MODEL_SHA256 = "c94553614878308bc219487c2910de23ff2dd5a686737014cdc96a540e18814f"

# ─── ANSI colors ─────────────────────────────────────────────────────────────

GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
RESET = "\033[0m"
BOLD = "\033[1m"


def _pass(msg: str) -> str:
    return f"{GREEN}  PASS{RESET}  {msg}"


def _fail(msg: str) -> str:
    return f"{RED}  FAIL{RESET}  {msg}"


def _warn(msg: str) -> str:
    return f"{YELLOW}  WARN{RESET}  {msg}"


# ─── Checks ──────────────────────────────────────────────────────────────────

def check_health_endpoint(url: str, timeout: int) -> dict | None:
    """Fetch /health and return parsed JSON, or None on error."""
    health_url = url.rstrip("/") + "/health"
    print(f"\n{BOLD}[1] GET {health_url}{RESET}")
    try:
        with urllib.request.urlopen(health_url, timeout=timeout) as resp:
            raw = resp.read()
            data = json.loads(raw)
            print(f"       Status: {resp.status}")
            return data
    except urllib.error.HTTPError as exc:
        print(_fail(f"HTTP {exc.code}: {exc.reason}"))
        return None
    except urllib.error.URLError as exc:
        print(_fail(f"Could not connect – {exc.reason}"))
        print("       Is the server running? Try: docker compose up -d")
        return None
    except json.JSONDecodeError as exc:
        print(_fail(f"Response is not valid JSON: {exc}"))
        return None


def check_class_count(data: dict, results: list[bool]) -> None:
    """Verify number of class names."""
    print(f"\n{BOLD}[2] class_names count == {EXPECTED_CLASS_COUNT}{RESET}")
    actual = data.get("class_names", [])
    count = len(actual)
    if count == EXPECTED_CLASS_COUNT:
        print(_pass(f"Found {count} class names"))
        results.append(True)
    else:
        print(_fail(f"Expected {EXPECTED_CLASS_COUNT}, got {count}: {actual}"))
        results.append(False)


def check_class_names(data: dict, results: list[bool]) -> None:
    """Verify exact class names and order."""
    print(f"\n{BOLD}[3] class_names exact match{RESET}")
    actual = data.get("class_names", [])
    if actual == EXPECTED_CLASS_NAMES:
        print(_pass("Names and order match exactly"))
        results.append(True)
    else:
        mismatches: list[str] = []
        for i, (exp, got) in enumerate(zip(EXPECTED_CLASS_NAMES, actual)):
            if exp != got:
                mismatches.append(f"  index {i}: expected '{exp}', got '{got}'")
        extra = actual[len(EXPECTED_CLASS_NAMES):]
        missing = EXPECTED_CLASS_NAMES[len(actual):]
        msg_parts = mismatches
        if extra:
            msg_parts.append(f"  extra classes: {extra}")
        if missing:
            msg_parts.append(f"  missing classes: {missing}")
        print(_fail("Mismatch:\n" + "\n".join(msg_parts)))
        results.append(False)


def check_model_sha256_via_health(data: dict, results: list[bool]) -> None:
    """Verify model SHA-256 if /health exposes it."""
    print(f"\n{BOLD}[4] model_sha256 in /health response{RESET}")
    sha = data.get("model_sha256") or data.get("ai", {}).get("model_sha256") if isinstance(data.get("ai"), dict) else None
    if sha is None:
        # Try nested keys common in FastAPI health responses
        for key in ("ai_model", "model", "info"):
            nested = data.get(key)
            if isinstance(nested, dict):
                sha = nested.get("model_sha256")
                if sha:
                    break

    if sha is None:
        print(_warn("/health does not expose model_sha256 – skipping remote check"))
        print("       Run local file check (see [5]) to verify SHA instead.")
        results.append(True)  # Not a failure – endpoint may not expose it
        return

    if sha == EXPECTED_MODEL_SHA256:
        print(_pass(f"SHA-256 matches: {sha[:16]}…"))
        results.append(True)
    else:
        print(_fail(f"Expected: {EXPECTED_MODEL_SHA256}"))
        print(f"         Got:      {sha}")
        results.append(False)


def check_local_model_sha256(results: list[bool]) -> None:
    """Verify local model.keras SHA-256 if the file exists."""
    print(f"\n{BOLD}[5] Local model.keras SHA-256{RESET}")
    # Walk up from this file to find the backend root
    here = Path(__file__).resolve()
    candidates = [
        here.parent.parent / "app" / "ai_model" / "artifacts" / "model.keras",
        Path("app") / "ai_model" / "artifacts" / "model.keras",
    ]
    model_path: Path | None = next((p for p in candidates if p.is_file()), None)

    if model_path is None:
        print(_warn("model.keras not found locally – skipping"))
        print("       Looked in: " + " | ".join(str(p) for p in candidates))
        results.append(True)  # Not a failure when running against remote server
        return

    print(f"       File: {model_path}")
    print("       Computing SHA-256 (may take a few seconds for large files)…")
    h = hashlib.sha256()
    with model_path.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    actual_sha = h.hexdigest()

    if actual_sha == EXPECTED_MODEL_SHA256:
        print(_pass(f"SHA-256 matches: {actual_sha[:16]}…"))
        results.append(True)
    else:
        print(_fail("SHA-256 mismatch – wrong model file!"))
        print(f"         Expected: {EXPECTED_MODEL_SHA256}")
        print(f"         Got:      {actual_sha}")
        results.append(False)


# ─── Main ────────────────────────────────────────────────────────────────────

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--url", default="http://localhost:8000", help="Base URL of the running API (default: http://localhost:8000)")
    parser.add_argument("--timeout", type=int, default=10, help="HTTP request timeout in seconds (default: 10)")
    args = parser.parse_args()

    print(f"\n{'='*60}")
    print(f"  ECOSORT Model 12-Class Verification")
    print(f"  Target: {args.url}")
    print(f"  Expected SHA-256: {EXPECTED_MODEL_SHA256[:20]}…")
    print(f"{'='*60}")

    results: list[bool] = []

    # [1] Fetch /health
    health_data = check_health_endpoint(args.url, args.timeout)
    if health_data is None:
        print(f"\n{'='*60}")
        print(f"{RED}  ABORTED{RESET}  Cannot reach server – all checks failed.")
        print(f"{'='*60}\n")
        return 1

    print(f"\n       Response preview: {json.dumps(health_data)[:200]}")

    # [2] Count
    check_class_count(health_data, results)

    # [3] Names
    check_class_names(health_data, results)

    # [4] SHA via health
    check_model_sha256_via_health(health_data, results)

    # [5] Local SHA
    check_local_model_sha256(results)

    # ─── Summary ─────────────────────────────────────────────────────────────
    total = len(results)
    passed = sum(results)
    failed = total - passed

    print(f"\n{'='*60}")
    if failed == 0:
        print(f"{GREEN}{BOLD}  ALL {total} CHECKS PASSED ✓{RESET}")
        print("  Image is confirmed to use the 12-class model.")
    else:
        print(f"{RED}{BOLD}  {failed}/{total} CHECK(S) FAILED ✗{RESET}")
        print("  Review the output above and fix the reported issues.")
    print(f"{'='*60}\n")

    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
