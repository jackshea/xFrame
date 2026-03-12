#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""Unity Agent Bridge 的单文件 JSON-RPC WebSocket 客户端。"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import socket
import ssl
import struct
import sys
import time
from pathlib import Path
from typing import Any, Callable
from urllib.parse import urlparse


DEFAULT_TIMEOUT_SECONDS = 5.0
DEFAULT_PORT = 17777


class RpcError(Exception):
    """表示 Unity 侧返回的 JSON-RPC 错误。"""

    def __init__(self, code: int, message: str, data: Any = None) -> None:
        super().__init__(f"RPC error {code}: {message}")
        self.code = code
        self.rpc_message = message
        self.data = data


class WebSocketTransport:
    """基于 Python 标准库实现的最小 WebSocket 文本客户端。"""

    _GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"

    def __init__(self, endpoint: str, timeout_seconds: float) -> None:
        self._endpoint = endpoint
        self._timeout_seconds = max(timeout_seconds, 0.001)
        self._socket = self._connect()

    def _connect(self) -> socket.socket:
        parsed = urlparse(self._endpoint)
        if parsed.scheme not in ("ws", "wss"):
            raise ValueError(f"Unsupported endpoint scheme: {parsed.scheme}")

        host = parsed.hostname
        if not host:
            raise ValueError("WebSocket endpoint is missing host.")

        port = parsed.port or (443 if parsed.scheme == "wss" else 80)
        path = parsed.path or "/"
        if parsed.query:
            path = f"{path}?{parsed.query}"

        raw_socket = socket.create_connection((host, port), timeout=self._timeout_seconds)
        raw_socket.settimeout(self._timeout_seconds)

        if parsed.scheme == "wss":
            context = ssl.create_default_context()
            ws_socket = context.wrap_socket(raw_socket, server_hostname=host)
        else:
            ws_socket = raw_socket

        key = base64.b64encode(os.urandom(16)).decode("ascii")
        request = (
            f"GET {path} HTTP/1.1\r\n"
            f"Host: {host}:{port}\r\n"
            "Upgrade: websocket\r\n"
            "Connection: Upgrade\r\n"
            f"Sec-WebSocket-Key: {key}\r\n"
            "Sec-WebSocket-Version: 13\r\n"
            "\r\n"
        )
        ws_socket.sendall(request.encode("ascii"))

        response = self._read_http_response(ws_socket)
        self._validate_handshake(response, key)
        return ws_socket

    def _read_http_response(self, ws_socket: socket.socket) -> bytes:
        buffer = bytearray()
        deadline = time.monotonic() + self._timeout_seconds
        while b"\r\n\r\n" not in buffer:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise TimeoutError("WebSocket connect timeout.")

            ws_socket.settimeout(remaining)
            chunk = ws_socket.recv(4096)
            if not chunk:
                raise ConnectionError("WebSocket handshake failed: unexpected EOF.")
            buffer.extend(chunk)

        return bytes(buffer)

    def _validate_handshake(self, response: bytes, key: str) -> None:
        header_bytes, _, _ = response.partition(b"\r\n\r\n")
        header_text = header_bytes.decode("iso-8859-1")
        lines = header_text.split("\r\n")
        if not lines or "101" not in lines[0]:
            raise ConnectionError(f"WebSocket handshake failed: {lines[0] if lines else 'invalid response'}")

        headers: dict[str, str] = {}
        for line in lines[1:]:
            if ":" not in line:
                continue
            name, value = line.split(":", 1)
            headers[name.strip().lower()] = value.strip()

        expected_accept = base64.b64encode(
            hashlib.sha1(f"{key}{self._GUID}".encode("ascii")).digest()
        ).decode("ascii")
        actual_accept = headers.get("sec-websocket-accept")
        if actual_accept != expected_accept:
            raise ConnectionError("WebSocket handshake failed: invalid Sec-WebSocket-Accept.")

    def send(self, payload: str) -> None:
        encoded = payload.encode("utf-8")
        header = bytearray()
        header.append(0x81)
        length = len(encoded)
        mask_key = os.urandom(4)

        if length < 126:
            header.append(0x80 | length)
        elif length < 65536:
            header.append(0x80 | 126)
            header.extend(struct.pack("!H", length))
        else:
            header.append(0x80 | 127)
            header.extend(struct.pack("!Q", length))

        header.extend(mask_key)
        masked = bytes(value ^ mask_key[index % 4] for index, value in enumerate(encoded))
        self._socket.sendall(bytes(header) + masked)

    def receive(self) -> str:
        while True:
            opcode, payload = self._read_frame()
            if opcode == 0x1:
                return payload.decode("utf-8")
            if opcode == 0x8:
                raise ConnectionError("WebSocket closed by remote host.")
            if opcode == 0x9:
                self._send_control_frame(0xA, payload)
                continue
            if opcode == 0xA:
                continue

            raise ConnectionError(f"Unsupported WebSocket opcode: {opcode}")

    def _read_frame(self) -> tuple[int, bytes]:
        first, second = self._recv_exact(2)
        opcode = first & 0x0F
        masked = (second & 0x80) != 0
        length = second & 0x7F

        if length == 126:
            length = struct.unpack("!H", self._recv_exact(2))[0]
        elif length == 127:
            length = struct.unpack("!Q", self._recv_exact(8))[0]

        mask_key = self._recv_exact(4) if masked else b""
        payload = self._recv_exact(length) if length > 0 else b""
        if masked:
            payload = bytes(value ^ mask_key[index % 4] for index, value in enumerate(payload))

        return opcode, payload

    def _recv_exact(self, length: int) -> bytes:
        buffer = bytearray()
        while len(buffer) < length:
            chunk = self._socket.recv(length - len(buffer))
            if not chunk:
                raise ConnectionError("WebSocket receive failed: unexpected EOF.")
            buffer.extend(chunk)
        return bytes(buffer)

    def _send_control_frame(self, opcode: int, payload: bytes) -> None:
        mask_key = os.urandom(4)
        header = bytearray([0x80 | opcode, 0x80 | len(payload)])
        header.extend(mask_key)
        masked = bytes(value ^ mask_key[index % 4] for index, value in enumerate(payload))
        self._socket.sendall(bytes(header) + masked)

    def close(self) -> None:
        if self._socket is None:
            return

        try:
            self._send_control_frame(0x8, b"\x03\xe8done")
        except OSError:
            pass
        finally:
            try:
                self._socket.close()
            finally:
                self._socket = None


class UnityJsonRpcClient:
    """封装 ping + 调用 + 测试进度处理。"""

    def __init__(
        self,
        config: dict[str, Any],
        transport_factory: Callable[[str, float], Any] | None = None,
    ) -> None:
        self._config = config
        self._sequence = 0
        self._transport_factory = transport_factory or (lambda endpoint, timeout: WebSocketTransport(endpoint, timeout))

    def call(self, method: str, params: dict[str, Any] | None) -> Any:
        transport = self._transport_factory(self._config["endpoint"], float(self._config["timeout_seconds"]))
        try:
            self._send_and_receive(transport, "agent.ping", {})
            return self._send_and_receive(transport, method, params or {})
        finally:
            transport.close()

    def run_tests_with_progress(
        self,
        params: dict[str, Any] | None,
        on_progress: Callable[[Any], None] | None,
    ) -> Any:
        transport = self._transport_factory(self._config["endpoint"], float(self._config["timeout_seconds"]))
        try:
            self._send_and_receive(transport, "agent.ping", {}, on_progress)
            result = self._send_and_receive(transport, "unity.tests.run", params or {}, on_progress)
            if isinstance(result, dict) and result.get("completed") is True:
                return result

            while True:
                message = self._receive_message(transport)
                notification = message.get("notification")
                if notification is None:
                    continue

                if not _handle_progress_notification(notification, on_progress):
                    continue

                progress = notification.get("params", {}).get("payload")
                if isinstance(progress, dict) and progress.get("status") != "running":
                    return extract_test_run_result(progress)
        finally:
            transport.close()

    def _next_id(self) -> str:
        self._sequence += 1
        return str(self._sequence)

    def _send_and_receive(
        self,
        transport: Any,
        method: str,
        params: dict[str, Any],
        on_progress: Callable[[Any], None] | None = None,
    ) -> Any:
        request = build_request(self._next_id(), method, params)
        transport.send(json.dumps(request, ensure_ascii=False))

        while True:
            message = self._receive_message(transport)
            notification = message.get("notification")
            if notification is not None:
                _handle_progress_notification(notification, on_progress)
                continue

            response = message.get("response")
            if not isinstance(response, dict):
                raise ValueError("Invalid JSON-RPC response.")

            if str(response.get("id", "")) != request["id"]:
                continue

            error = response.get("error")
            if isinstance(error, dict):
                raise RpcError(
                    int(error.get("code", -32603)),
                    str(error.get("message", "Unknown")),
                    error.get("data"),
                )

            return response.get("result")

    def _receive_message(self, transport: Any) -> dict[str, Any]:
        raw_message = transport.receive()
        message = json.loads(raw_message)
        if not isinstance(message, dict):
            raise ValueError("Invalid JSON-RPC response.")

        is_notification = message.get("id") is None and isinstance(message.get("method"), str)
        return {"notification": message} if is_notification else {"response": message}


def _handle_progress_notification(notification: dict[str, Any], on_progress: Callable[[Any], None] | None) -> bool:
    if notification.get("method") != "agent.event":
        return False

    params = notification.get("params")
    if not isinstance(params, dict) or params.get("name") != "unity.tests.progress":
        return False

    if callable(on_progress):
        on_progress(params.get("payload"))

    return True


def build_request(request_id: str, method: str, params: dict[str, Any] | None) -> dict[str, Any]:
    return {
        "jsonrpc": "2.0",
        "id": request_id,
        "method": method,
        "params": json.loads(json.dumps(params or {}, ensure_ascii=False)),
    }


def parse_options(args: list[str]) -> dict[str, str]:
    options: dict[str, str] = {}
    index = 0
    while index < len(args):
        key = args[index]
        if not key.startswith("--"):
            index += 1
            continue

        if index + 1 >= len(args):
            raise ValueError(f"missing value for option: {key}")

        options[key.lower()] = args[index + 1]
        index += 2

    return options


def resolve_option_or_env(options: dict[str, str], option_key: str, env_key: str, env: dict[str, str]) -> str | None:
    option_value = options.get(option_key)
    if isinstance(option_value, str) and option_value.strip():
        return option_value.strip()

    env_value = env.get(env_key)
    return env_value.strip() if isinstance(env_value, str) and env_value.strip() else None


def resolve_endpoint(options: dict[str, str], env: dict[str, str], cwd: str | None = None) -> str | None:
    endpoint = resolve_option_or_env(options, "--endpoint", "UNITY_RPC_ENDPOINT", env)
    if endpoint:
        return endpoint

    host = env.get("UNITY_RPC_HOST", "").strip()
    port_value = env.get("UNITY_RPC_PORT", "").strip()
    port = int(port_value) if port_value.isdigit() else DEFAULT_PORT
    if host:
        return f"ws://{host}:{port}"

    instance_key = resolve_option_or_env(options, "--instance", "UNITY_RPC_INSTANCE", env)
    settings = load_agent_bridge_settings(cwd)
    if settings is None:
        return None

    instances = settings.get("Instances")
    running_instances = [
        instance
        for instance in instances or []
        if isinstance(instance, dict)
        and instance.get("IsRunning") is True
        and isinstance(instance.get("Host"), str)
        and isinstance(instance.get("Port"), int)
    ]

    if not running_instances:
        host_value = settings.get("Host")
        port_value = settings.get("Port")
        if isinstance(host_value, str) and host_value.strip() and isinstance(port_value, int):
            return f"ws://{host_value.strip()}:{port_value}"
        return None

    selected = select_instance(running_instances, instance_key)
    if selected is None:
        selected = sort_instances_by_last_seen(running_instances)[0]

    return f"ws://{selected['Host'].strip()}:{selected['Port']}" if selected else None


def build_progress_payload(payload: dict[str, Any] | None) -> dict[str, Any]:
    payload = payload or {}
    return {
        "event": payload.get("event"),
        "source": "unity.tests.run",
        "runId": payload.get("runId"),
        "mode": payload.get("mode"),
        "filter": payload.get("filter"),
        "status": payload.get("status"),
        "summary": payload.get("summary"),
        "failures": payload.get("failures"),
    }


def extract_test_run_result(progress: dict[str, Any] | None) -> dict[str, Any]:
    progress = progress or {}
    return {
        "completed": progress.get("status") != "running",
        "runId": progress.get("runId"),
        "mode": progress.get("mode"),
        "filter": progress.get("filter"),
        "status": progress.get("status"),
        "summary": progress.get("summary"),
        "failures": progress.get("failures"),
    }


def load_agent_bridge_settings(cwd: str | None = None) -> dict[str, Any] | None:
    settings_path = Path(cwd or os.getcwd()) / "UserSettings" / "AgentBridgeSettings.json"
    if not settings_path.exists():
        return None

    try:
        return json.loads(settings_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None


def sort_instances_by_last_seen(instances: list[dict[str, Any]]) -> list[dict[str, Any]]:
    return sorted(
        instances,
        key=lambda item: _parse_timestamp(item.get("LastSeenUtc")),
        reverse=True,
    )


def _parse_timestamp(value: Any) -> float:
    if not isinstance(value, str) or not value:
        return 0.0

    normalized = value.replace("Z", "+00:00")
    try:
        from datetime import datetime

        return datetime.fromisoformat(normalized).timestamp()
    except ValueError:
        return 0.0


def select_instance(instances: list[dict[str, Any]], instance_key: str | None) -> dict[str, Any] | None:
    if not instance_key:
        return None

    for instance in instances:
        if (
            str(instance.get("InstanceId", "")) == instance_key
            or str(instance.get("ProcessId", "")) == instance_key
            or str(instance.get("Port", "")) == instance_key
        ):
            return instance

    return None


def print_usage(stderr: Any) -> None:
    stderr.write(
        "Usage: python3 scripts/agent/unity-rpc.py call "
        "[--endpoint <ws://host:port> | --instance <instanceId|processId|port>] "
        "--method <rpc.method> [--params <json>] [--timeout <seconds>]\n"
        "Env fallback: UNITY_RPC_ENDPOINT, UNITY_RPC_HOST(+UNITY_RPC_PORT), "
        "or UserSettings/AgentBridgeSettings.json\n"
    )


def run_async(args: list[str], stdout: Any = None, stderr: Any = None, env: dict[str, str] | None = None) -> int:
    stdout = stdout or sys.stdout
    stderr = stderr or sys.stderr
    env = env or dict(os.environ)

    if not args or args[0].lower() != "call":
        print_usage(stderr)
        return 2

    try:
        options = parse_options(args[1:])
    except ValueError as error:
        stderr.write(f"{error}\n")
        print_usage(stderr)
        return 2

    endpoint = resolve_endpoint(options, env)
    method = options.get("--method")
    if not endpoint or not method:
        if not endpoint:
            stderr.write(
                "missing endpoint: pass --endpoint, set UNITY_RPC_ENDPOINT/UNITY_RPC_HOST, "
                "or start AgentBridge so UserSettings/AgentBridgeSettings.json contains a running instance\n"
            )
        print_usage(stderr)
        return 2

    raw_params = options.get("--params", "{}")
    try:
        params = json.loads(raw_params)
    except json.JSONDecodeError as error:
        stderr.write(f"invalid --params JSON: {error}\n")
        return 2

    if not isinstance(params, dict):
        stderr.write("--params must be a JSON object\n")
        return 2

    timeout_seconds = DEFAULT_TIMEOUT_SECONDS
    raw_timeout = options.get("--timeout")
    if raw_timeout is not None:
        try:
            timeout_seconds = float(raw_timeout)
        except ValueError:
            timeout_seconds = DEFAULT_TIMEOUT_SECONDS

    client = UnityJsonRpcClient({"endpoint": endpoint, "timeout_seconds": timeout_seconds})

    try:
        if method == "unity.tests.run":
            final_result = client.run_tests_with_progress(
                params,
                lambda payload: stderr.write(json.dumps(build_progress_payload(payload), ensure_ascii=False) + "\n"),
            )
            stdout.write(json.dumps(final_result, ensure_ascii=False) + "\n")
            return 0

        result = client.call(method, params)
        stdout.write(json.dumps(result, ensure_ascii=False) + "\n")
        return 0
    except RpcError as error:
        payload = {"code": error.code, "message": error.rpc_message}
        if error.data is not None:
            payload["data"] = error.data
        stderr.write(json.dumps(payload, ensure_ascii=False) + "\n")
        return 1
    except Exception as error:  # noqa: BLE001
        stderr.write(f"{error}\n")
        return 1


def main(argv: list[str] | None = None) -> int:
    return run_async(argv or sys.argv[1:])


if __name__ == "__main__":
    sys.exit(main())
