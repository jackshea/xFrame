#!/usr/bin/env python3
"""Unity Agent Bridge JSON-RPC client."""

from __future__ import annotations

import argparse
import json
from dataclasses import dataclass
from typing import Any, Callable, Dict, Optional


class RpcError(Exception):
    """JSON-RPC level error."""

    def __init__(self, code: int, message: str, data: Any = None) -> None:
        super().__init__(f"RPC error {code}: {message}")
        self.code = code
        self.message = message
        self.data = data


@dataclass
class JsonRpcClientConfig:
    endpoint: str
    token: str
    timeout_seconds: float = 5.0


class WebSocketTransport:
    """Thin websocket-client wrapper."""

    def __init__(self, endpoint: str, timeout_seconds: float = 5.0) -> None:
        try:
            import websocket  # type: ignore
        except Exception as exc:  # pragma: no cover
            raise RuntimeError(
                "websocket-client is required. Install with: pip install websocket-client"
            ) from exc

        self._ws = websocket.create_connection(endpoint, timeout=timeout_seconds)

    def send(self, payload: str) -> None:
        self._ws.send(payload)

    def recv(self) -> str:
        return self._ws.recv()

    def close(self) -> None:
        self._ws.close()


def build_request(request_id: str, method: str, params: Dict[str, Any]) -> Dict[str, Any]:
    return {"jsonrpc": "2.0", "id": request_id, "method": method, "params": params}


class UnityRpcClient:
    def __init__(
        self,
        config: JsonRpcClientConfig,
        transport_factory: Callable[[str], Any] = WebSocketTransport,
    ) -> None:
        self._config = config
        self._transport_factory = transport_factory
        self._seq = 0

    def _next_id(self) -> str:
        self._seq += 1
        return str(self._seq)

    def _send_and_receive(self, transport: Any, method: str, params: Dict[str, Any]) -> Any:
        request = build_request(self._next_id(), method, params)
        transport.send(json.dumps(request, ensure_ascii=False))
        response = json.loads(transport.recv())

        if "error" in response and response["error"] is not None:
            error = response["error"]
            raise RpcError(error.get("code", -32603), error.get("message", "Unknown"), error.get("data"))

        return response.get("result")

    def _authenticate(self, transport: Any) -> None:
        self._send_and_receive(transport, "agent.authenticate", {"token": self._config.token})

    def call(self, method: str, params: Optional[Dict[str, Any]] = None) -> Any:
        call_params = params or {}
        transport = self._transport_factory(self._config.endpoint, self._config.timeout_seconds)

        try:
            self._send_and_receive(transport, "agent.ping", {})
            try:
                return self._send_and_receive(transport, method, call_params)
            except RpcError as exc:
                if exc.code != -32001:
                    raise

                self._authenticate(transport)
                return self._send_and_receive(transport, method, call_params)
        finally:
            transport.close()


def create_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Call Unity Agent Bridge RPC")
    subparsers = parser.add_subparsers(dest="command", required=True)

    call_parser = subparsers.add_parser("call", help="Call RPC method")
    call_parser.add_argument("--endpoint", required=True, help="ws endpoint, e.g. ws://127.0.0.1:17777")
    call_parser.add_argument("--token", required=True, help="agent token")
    call_parser.add_argument("--method", required=True, help="rpc method")
    call_parser.add_argument("--params", default="{}", help="json object string")
    call_parser.add_argument("--timeout", type=float, default=5.0, help="connect/recv timeout seconds")

    return parser


def main(argv: Optional[list[str]] = None) -> int:
    parser = create_parser()
    args = parser.parse_args(argv)

    if args.command != "call":
        parser.error("unsupported command")

    try:
        params = json.loads(args.params)
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid --params JSON: {exc}") from exc

    if not isinstance(params, dict):
        raise ValueError("--params must be a JSON object")

    client = UnityRpcClient(
        JsonRpcClientConfig(endpoint=args.endpoint, token=args.token, timeout_seconds=args.timeout)
    )
    result = client.call(args.method, params)
    print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":  # pragma: no cover
    raise SystemExit(main())
