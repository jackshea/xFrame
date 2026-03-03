import json
import unittest

from scripts.agent.unity_rpc_client import JsonRpcClientConfig
from scripts.agent.unity_rpc_client import UnityRpcClient
from scripts.agent.unity_rpc_client import build_request
from scripts.agent.unity_rpc_client import main


class FakeTransport:
    def __init__(self, responses):
        self.responses = list(responses)
        self.sent = []
        self.closed = False

    def send(self, payload):
        self.sent.append(json.loads(payload))

    def recv(self):
        if not self.responses:
            raise RuntimeError("no response")
        return json.dumps(self.responses.pop(0), ensure_ascii=False)

    def close(self):
        self.closed = True


class UnityRpcClientTests(unittest.TestCase):
    def test_build_request_should_include_jsonrpc(self):
        req = build_request("1", "agent.ping", {})
        self.assertEqual("2.0", req["jsonrpc"])
        self.assertEqual("1", req["id"])
        self.assertEqual("agent.ping", req["method"])

    def test_call_when_unauthorized_should_auth_then_retry(self):
        fake = FakeTransport(
            [
                {"jsonrpc": "2.0", "id": "1", "result": {"pong": True}},
                {"jsonrpc": "2.0", "id": "2", "error": {"code": -32001, "message": "Unauthorized"}},
                {"jsonrpc": "2.0", "id": "3", "result": {"authenticated": True}},
                {"jsonrpc": "2.0", "id": "4", "result": {"found": True}},
            ]
        )

        client = UnityRpcClient(
            JsonRpcClientConfig(endpoint="ws://127.0.0.1:17777", token="abc"),
            transport_factory=lambda *_: fake,
        )

        result = client.call("unity.gameobject.find", {"name": "Player"})
        self.assertTrue(result["found"])
        self.assertEqual("agent.ping", fake.sent[0]["method"])
        self.assertEqual("unity.gameobject.find", fake.sent[1]["method"])
        self.assertEqual("agent.authenticate", fake.sent[2]["method"])
        self.assertEqual("unity.gameobject.find", fake.sent[3]["method"])
        self.assertTrue(fake.closed)

    def test_main_invalid_params_should_raise(self):
        with self.assertRaises(ValueError):
            main([
                "call",
                "--endpoint",
                "ws://127.0.0.1:17777",
                "--token",
                "abc",
                "--method",
                "agent.ping",
                "--params",
                "[]",
            ])


if __name__ == "__main__":
    unittest.main()
