#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import List


def is_wsl() -> bool:
    return "WSL_DISTRO_NAME" in os.environ or "WSL_INTEROP" in os.environ


def looks_like_windows_path(path: str) -> bool:
    if len(path) >= 3 and path[1] == ":" and path[2] in ("\\", "/"):
        return True
    if "\\" in path:
        return True
    return False


def run_wslpath(arg: str, value: str) -> str:
    completed = subprocess.run(["wslpath", arg, value], capture_output=True, text=True, check=False)
    if completed.returncode != 0:
        raise SystemExit(f"Error: failed to convert path via wslpath: {value}")
    return completed.stdout.strip()


def to_unix_path(path: str) -> str:
    if is_wsl() and looks_like_windows_path(path):
        return run_wslpath("-u", path)
    return path


def to_windows_path(path: str) -> str:
    if is_wsl():
        return run_wslpath("-w", path)
    return path


def resolve_unity_path(cli_unity: str | None, dry_run: bool) -> str:
    unity = cli_unity or os.environ.get("UNITY_EDITOR_PATH")
    if not unity:
        raise SystemExit("Error: Unity path is required. Use --unity or set UNITY_EDITOR_PATH.")

    unity = to_unix_path(unity)

    if not dry_run and not Path(unity).is_file():
        raise SystemExit(f"Error: Unity editor not found at '{unity}'.")

    return unity


def build_args(
    project_path: str,
    platform: str,
    results_file: Path,
    log_file: Path,
    test_filter: str | None,
) -> List[str]:
    normalized_project_path = to_windows_path(project_path)
    normalized_results_file = to_windows_path(str(results_file))
    normalized_log_file = to_windows_path(str(log_file))

    args = [
        "-batchmode",
        "-projectPath",
        normalized_project_path,
        "-runTests",
        "-testPlatform",
        platform,
        "-testResults",
        normalized_results_file,
        "-logFile",
        normalized_log_file,
        "-quit",
    ]
    if test_filter:
        args.extend(["-testFilter", test_filter])
    return args


def run_one(
    unity: str,
    project_path: str,
    platform: str,
    results_file: Path,
    log_file: Path,
    test_filter: str | None,
    dry_run: bool,
) -> None:
    args = build_args(project_path, platform, results_file, log_file, test_filter)

    print(f"[validate] platform={platform}")
    print(f"[validate] results={results_file}")
    print(f"[validate] log={log_file}")

    if dry_run:
        quoted = " ".join([f'"{unity}"'] + [f'"{a}"' for a in args])
        print(f"[dry-run] {quoted}")
        return

    cmd = [unity] + args
    completed = subprocess.run(cmd, check=False)
    if completed.returncode != 0:
        raise SystemExit(f"Error: Unity test run failed with exit code {completed.returncode}.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run Unity tests in a deterministic AI-friendly way.",
        formatter_class=argparse.RawTextHelpFormatter,
    )
    parser.add_argument("command", choices=["single", "suite", "full"], help="Validation command")
    parser.add_argument("--platform", choices=["EditMode", "PlayMode"], help="Required for single/suite")
    parser.add_argument("--filter", help="Required for single")
    parser.add_argument("--unity", help="Unity editor executable path")
    parser.add_argument("--project-path", default=str(Path.cwd()), help="Unity project path")
    parser.add_argument("--results-dir", default="Temp/AIValidation", help="Results directory")
    parser.add_argument("--logs-dir", default="Temp/AIValidation/logs", help="Logs directory")
    parser.add_argument("--dry-run", action="store_true", help="Print command only")
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    if args.command in {"single", "suite"} and not args.platform:
        raise SystemExit(f"Error: {args.command} requires --platform.")

    if args.command == "single" and not args.filter:
        raise SystemExit("Error: single requires --filter.")

    unity = resolve_unity_path(args.unity, args.dry_run)
    project_path = str(Path(args.project_path).resolve())

    results_dir = Path(args.results_dir)
    logs_dir = Path(args.logs_dir)
    results_dir.mkdir(parents=True, exist_ok=True)
    logs_dir.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")

    if args.command == "single":
        platform = args.platform
        run_one(
            unity,
            project_path,
            platform,
            results_dir / f"{platform}-single-{timestamp}.xml",
            logs_dir / f"{platform}-single-{timestamp}.log",
            args.filter,
            args.dry_run,
        )
    elif args.command == "suite":
        platform = args.platform
        run_one(
            unity,
            project_path,
            platform,
            results_dir / f"{platform}-suite-{timestamp}.xml",
            logs_dir / f"{platform}-suite-{timestamp}.log",
            None,
            args.dry_run,
        )
    else:
        run_one(
            unity,
            project_path,
            "EditMode",
            results_dir / f"EditMode-suite-{timestamp}.xml",
            logs_dir / f"EditMode-suite-{timestamp}.log",
            None,
            args.dry_run,
        )
        run_one(
            unity,
            project_path,
            "PlayMode",
            results_dir / f"PlayMode-suite-{timestamp}.xml",
            logs_dir / f"PlayMode-suite-{timestamp}.log",
            None,
            args.dry_run,
        )

    print("[validate] done")


if __name__ == "__main__":
    main()
