#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""unity-rpc.py 的回归测试。"""

from __future__ import annotations

import importlib.util
import io
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).with_name("unity-rpc.py")
SPEC = importlib.util.spec_from_file_location("unity_rpc", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC is not None and SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class FakeTransport:
    def __init__(self, responses: list[str]) -> None:
        self._responses = list(responses)
        self.sent: list[dict] = []
        self.closed = False

    def send(self, payload: str) -> None:
        self.sent.append(json.loads(payload))

    def receive(self) -> str:
        if not self._responses:
            raise RuntimeError("No response queued.")
        return self._responses.pop(0)

    def close(self) -> None:
        self.closed = True


class UnityRpcTests(unittest.TestCase):
    def test_build_request_should_contain_json_rpc_fields(self) -> None:
        request = MODULE.build_request("1", "agent.ping", {})

        self.assertEqual("2.0", request["jsonrpc"])
        self.assertEqual("1", request["id"])
        self.assertEqual("agent.ping", request["method"])
        self.assertEqual({}, request["params"])

    def test_call_should_ping_before_target_method(self) -> None:
        transport = FakeTransport(
            [
                '{"jsonrpc":"2.0","id":"1","result":{"pong":true}}',
                '{"jsonrpc":"2.0","id":"2","result":{"found":true}}',
            ]
        )
        client = MODULE.UnityJsonRpcClient(
            {"endpoint": "ws://127.0.0.1:17777", "timeout_seconds": 5},
            lambda _endpoint, _timeout: transport,
        )

        result = client.call("unity.gameobject.find", {"name": "Player"})

        self.assertTrue(result["found"])
        self.assertEqual("agent.ping", transport.sent[0]["method"])
        self.assertEqual("unity.gameobject.find", transport.sent[1]["method"])
        self.assertTrue(transport.closed)

    def test_run_async_should_return_2_when_params_is_not_object(self) -> None:
        stderr = io.StringIO()

        exit_code = MODULE.run_async(
            [
                "call",
                "--endpoint",
                "ws://127.0.0.1:17777",
                "--method",
                "agent.ping",
                "--params",
                "[]",
            ],
            stderr=stderr,
        )

        self.assertEqual(2, exit_code)
        self.assertIn("--params must be a JSON object", stderr.getvalue())

    def test_run_async_should_write_rpc_error_json(self) -> None:
        stderr = io.StringIO()
        original_client = MODULE.UnityJsonRpcClient

        class StubClient:
            def __init__(self, _config: dict) -> None:
                pass

            def call(self, _method: str, _params: dict) -> None:
                raise MODULE.RpcError(-32601, "Method not found")

        MODULE.UnityJsonRpcClient = StubClient
        try:
            exit_code = MODULE.run_async(
                [
                    "call",
                    "--endpoint",
                    "ws://127.0.0.1:17777",
                    "--method",
                    "agent.unknown",
                    "--params",
                    "{}",
                ],
                stderr=stderr,
            )
        finally:
            MODULE.UnityJsonRpcClient = original_client

        self.assertEqual(1, exit_code)
        self.assertIn('"code": -32601', stderr.getvalue())
        self.assertIn('"message": "Method not found"', stderr.getvalue())

    def test_run_async_should_resolve_endpoint_from_env(self) -> None:
        stdout = io.StringIO()
        original_client = MODULE.UnityJsonRpcClient

        class StubClient:
            def __init__(self, config: dict) -> None:
                self._config = config

            def call(self, _method: str, _params: dict) -> dict:
                self_endpoint = self._config["endpoint"]
                return {"endpoint": self_endpoint, "pong": True}

        MODULE.UnityJsonRpcClient = StubClient
        try:
            exit_code = MODULE.run_async(
                ["call", "--method", "agent.ping", "--params", "{}"],
                stdout=stdout,
                env={"UNITY_RPC_HOST": "127.0.0.1", "UNITY_RPC_PORT": "17777"},
            )
        finally:
            MODULE.UnityJsonRpcClient = original_client

        self.assertEqual(0, exit_code)
        self.assertIn('"endpoint": "ws://127.0.0.1:17777"', stdout.getvalue())

    def test_run_async_should_stream_test_progress(self) -> None:
        stdout = io.StringIO()
        stderr = io.StringIO()
        original_client = MODULE.UnityJsonRpcClient

        class StubClient:
            def __init__(self, _config: dict) -> None:
                pass

            def run_tests_with_progress(self, params: dict, on_progress) -> dict:
                assert params == {"mode": "EditMode"}
                on_progress({"event": "started", "runId": "run-1", "mode": "EditMode", "status": "running"})
                on_progress(
                    {
                        "event": "progress",
                        "runId": "run-1",
                        "mode": "EditMode",
                        "status": "running",
                        "summary": {"total": 2, "completed": 1, "currentTest": "A"},
                        "failures": [],
                    }
                )
                on_progress(
                    {
                        "event": "completed",
                        "runId": "run-1",
                        "mode": "EditMode",
                        "status": "passed",
                        "summary": {"total": 2, "completed": 2, "passed": 2},
                        "failures": [],
                    }
                )
                return {
                    "completed": True,
                    "runId": "run-1",
                    "mode": "EditMode",
                    "status": "passed",
                    "summary": {"total": 2, "completed": 2, "passed": 2},
                    "failures": [],
                }

        MODULE.UnityJsonRpcClient = StubClient
        try:
            exit_code = MODULE.run_async(
                [
                    "call",
                    "--endpoint",
                    "ws://127.0.0.1:17777",
                    "--method",
                    "unity.tests.run",
                    "--params",
                    '{"mode":"EditMode"}',
                ],
                stdout=stdout,
                stderr=stderr,
            )
        finally:
            MODULE.UnityJsonRpcClient = original_client

        self.assertEqual(0, exit_code)
        progress_lines = [json.loads(line) for line in stderr.getvalue().strip().splitlines()]
        self.assertEqual(["started", "progress", "completed"], [line["event"] for line in progress_lines])
        self.assertEqual("passed", progress_lines[-1]["status"])
        self.assertIn('"status": "passed"', stdout.getvalue())

    def test_run_tests_with_progress_should_handle_notifications_before_result(self) -> None:
        transport = FakeTransport(
            [
                '{"jsonrpc":"2.0","id":"1","result":{"pong":true}}',
                '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"started","runId":"run-2","status":"running"}}}',
                '{"jsonrpc":"2.0","id":"2","result":{"started":true,"completed":false,"runId":"run-2","status":"running"}}',
                '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"progress","runId":"run-2","status":"running","summary":{"total":1,"completed":1}}}}',
                '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"completed","runId":"run-2","status":"passed","summary":{"total":1,"completed":1,"passed":1},"failures":[]}}}',
            ]
        )
        notifications: list[dict] = []
        client = MODULE.UnityJsonRpcClient(
            {"endpoint": "ws://127.0.0.1:17777", "timeout_seconds": 5},
            lambda _endpoint, _timeout: transport,
        )

        result = client.run_tests_with_progress({"mode": "EditMode"}, notifications.append)

        self.assertEqual("passed", result["status"])
        self.assertEqual(["started", "progress", "completed"], [item["event"] for item in notifications])
        self.assertTrue(transport.closed)

    def test_resolve_endpoint_should_choose_latest_running_instance(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            settings_path = Path(temp_dir) / "UserSettings" / "AgentBridgeSettings.json"
            settings_path.parent.mkdir(parents=True, exist_ok=True)
            settings_path.write_text(
                json.dumps(
                    {
                        "Instances": [
                            {
                                "InstanceId": "unity-a",
                                "ProcessId": 1001,
                                "Host": "127.0.0.1",
                                "Port": 17777,
                                "IsRunning": True,
                                "LastSeenUtc": "2026-03-11T01:00:00.0000000Z",
                            },
                            {
                                "InstanceId": "unity-b",
                                "ProcessId": 1002,
                                "Host": "127.0.0.1",
                                "Port": 17778,
                                "IsRunning": True,
                                "LastSeenUtc": "2026-03-11T02:00:00.0000000Z",
                            },
                        ]
                    }
                ),
                encoding="utf-8",
            )

            endpoint = MODULE.resolve_endpoint({}, {}, temp_dir)

        self.assertEqual("ws://127.0.0.1:17778", endpoint)

    def test_resolve_endpoint_should_choose_specific_instance(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            settings_path = Path(temp_dir) / "UserSettings" / "AgentBridgeSettings.json"
            settings_path.parent.mkdir(parents=True, exist_ok=True)
            settings_path.write_text(
                json.dumps(
                    {
                        "Instances": [
                            {
                                "InstanceId": "unity-a",
                                "ProcessId": 1001,
                                "Host": "127.0.0.1",
                                "Port": 17777,
                                "IsRunning": True,
                                "LastSeenUtc": "2026-03-11T01:00:00.0000000Z",
                            },
                            {
                                "InstanceId": "unity-b",
                                "ProcessId": 1002,
                                "Host": "127.0.0.1",
                                "Port": 17778,
                                "IsRunning": True,
                                "LastSeenUtc": "2026-03-11T02:00:00.0000000Z",
                            },
                        ]
                    }
                ),
                encoding="utf-8",
            )

            endpoint = MODULE.resolve_endpoint({"--instance": "1001"}, {}, temp_dir)

        self.assertEqual("ws://127.0.0.1:17777", endpoint)


if __name__ == "__main__":
    unittest.main()
