using System.Text.Json.Nodes;
using UnityRpcClient;
using Xunit;

namespace UnityRpcClient.Tests;

public sealed class UnityRpcClientTests
{
    [Fact]
    public void BuildRequest_ShouldIncludeJsonRpc()
    {
        JsonObject request = UnityJsonRpcClient.BuildRequest("1", "agent.ping", new JsonObject());

        Assert.Equal("2.0", request["jsonrpc"]?.GetValue<string>());
        Assert.Equal("1", request["id"]?.GetValue<string>());
        Assert.Equal("agent.ping", request["method"]?.GetValue<string>());
    }

    [Fact]
    public async Task CallAsync_WhenUnauthorized_ShouldAuthenticateThenRetry()
    {
        FakeTransport fake = new(
        [
            """{"jsonrpc":"2.0","id":"1","result":{"pong":true}}""",
            """{"jsonrpc":"2.0","id":"2","error":{"code":-32001,"message":"Unauthorized"}}""",
            """{"jsonrpc":"2.0","id":"3","result":{"authenticated":true}}""",
            """{"jsonrpc":"2.0","id":"4","result":{"found":true}}"""
        ]);

        UnityJsonRpcClient client = new(
            new JsonRpcClientConfig { Endpoint = "ws://127.0.0.1:17777", Token = "abc" },
            (_, _) => fake);

        JsonNode? result = await client.CallAsync(
            "unity.gameobject.find",
            new JsonObject { ["name"] = "Player" },
            CancellationToken.None);

        Assert.True(result?["found"]?.GetValue<bool>());
        Assert.Equal("agent.ping", fake.Sent[0]["method"]?.GetValue<string>());
        Assert.Equal("unity.gameobject.find", fake.Sent[1]["method"]?.GetValue<string>());
        Assert.Equal("agent.authenticate", fake.Sent[2]["method"]?.GetValue<string>());
        Assert.Equal("unity.gameobject.find", fake.Sent[3]["method"]?.GetValue<string>());
        Assert.True(fake.Closed);
    }

    [Fact]
    public async Task Main_WhenParamsNotJsonObject_ShouldReturnErrorCode()
    {
        int exitCode = await Program.Main(
        [
            "call",
            "--endpoint", "ws://127.0.0.1:17777",
            "--token", "abc",
            "--method", "agent.ping",
            "--params", "[]"
        ]);

        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task RunAsync_WhenRpcError_ShouldReturnErrorCode()
    {
        FakeTransport fake = new(
        [
            """{"jsonrpc":"2.0","id":"1","result":{"pong":true}}""",
            """{"jsonrpc":"2.0","id":"2","error":{"code":-32601,"message":"Method not found"}}"""
        ]);

        int exitCode = await Program.RunAsync(
        [
            "call",
            "--endpoint", "ws://127.0.0.1:17777",
            "--token", "abc",
            "--method", "agent.unknown",
            "--params", "{}"
        ],
        config => new UnityJsonRpcClient(config, (_, _) => fake));

        Assert.Equal(1, exitCode);
        Assert.True(fake.Closed);
    }

    private sealed class FakeTransport : ITransport
    {
        private readonly Queue<string> _responses;

        public FakeTransport(IEnumerable<string> responses)
        {
            _responses = new Queue<string>(responses);
        }

        public List<JsonObject> Sent { get; } = [];

        public bool Closed { get; private set; }

        public Task SendAsync(string payload, CancellationToken cancellationToken)
        {
            JsonObject request = JsonNode.Parse(payload)?.AsObject() ?? new JsonObject();
            Sent.Add(request);
            return Task.CompletedTask;
        }

        public Task<string> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No response queued.");
            }

            return Task.FromResult(_responses.Dequeue());
        }

        public ValueTask DisposeAsync()
        {
            Closed = true;
            return ValueTask.CompletedTask;
        }
    }
}
