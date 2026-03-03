using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UnityRpcClient;

public sealed class RpcErrorException : Exception
{
    public RpcErrorException(int code, string message, JsonNode? data)
        : base($"RPC error {code}: {message}")
    {
        Code = code;
        RpcMessage = message;
        DataNode = data;
    }

    public int Code { get; }

    public string RpcMessage { get; }

    public JsonNode? DataNode { get; }
}

public sealed class JsonRpcClientConfig
{
    public required string Endpoint { get; init; }

    public required string Token { get; init; }

    public double TimeoutSeconds { get; init; } = 5.0;
}

public interface ITransport : IAsyncDisposable
{
    Task SendAsync(string payload, CancellationToken cancellationToken);

    Task<string> ReceiveAsync(CancellationToken cancellationToken);
}

public sealed class WebSocketTransport : ITransport
{
    private readonly ClientWebSocket _socket = new();

    public WebSocketTransport(Uri endpoint, TimeSpan timeout)
    {
        using CancellationTokenSource cts = new(timeout);
        _socket.ConnectAsync(endpoint, cts.Token).GetAwaiter().GetResult();
    }

    public async Task SendAsync(string payload, CancellationToken cancellationToken)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(payload);
        await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task<string> ReceiveAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        using MemoryStream stream = new();

        while (true)
        {
            WebSocketReceiveResult result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new IOException("WebSocket closed by remote endpoint.");
            }

            await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
        }
        catch
        {
            // 关闭失败不影响主流程。
        }

        _socket.Dispose();
    }
}

public sealed class UnityJsonRpcClient
{
    private readonly JsonRpcClientConfig _config;
    private readonly Func<Uri, TimeSpan, ITransport> _transportFactory;
    private int _sequence;

    public UnityJsonRpcClient(
        JsonRpcClientConfig config,
        Func<Uri, TimeSpan, ITransport>? transportFactory = null)
    {
        _config = config;
        _transportFactory = transportFactory ?? ((uri, timeout) => new WebSocketTransport(uri, timeout));
    }

    public async Task<JsonNode?> CallAsync(string method, JsonObject? @params, CancellationToken cancellationToken)
    {
        Uri endpoint = new(_config.Endpoint);
        TimeSpan timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

        await using ITransport transport = _transportFactory(endpoint, timeout);

        await SendAndReceiveAsync(transport, "agent.ping", new JsonObject(), cancellationToken);

        try
        {
            return await SendAndReceiveAsync(transport, method, @params ?? new JsonObject(), cancellationToken);
        }
        catch (RpcErrorException ex) when (ex.Code == -32001)
        {
            await SendAndReceiveAsync(
                transport,
                "agent.authenticate",
                new JsonObject { ["token"] = _config.Token },
                cancellationToken);

            return await SendAndReceiveAsync(transport, method, @params ?? new JsonObject(), cancellationToken);
        }
    }

    private async Task<JsonNode?> SendAndReceiveAsync(
        ITransport transport,
        string method,
        JsonObject @params,
        CancellationToken cancellationToken)
    {
        JsonObject request = BuildRequest(NextId(), method, @params);
        await transport.SendAsync(request.ToJsonString(), cancellationToken);

        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(_config.TimeoutSeconds));
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        string responsePayload = await transport.ReceiveAsync(linked.Token);
        JsonNode? responseNode = JsonNode.Parse(responsePayload);
        JsonObject response = responseNode?.AsObject()
                              ?? throw new InvalidDataException("Invalid JSON-RPC response.");

        JsonNode? error = response["error"];
        if (error is JsonObject errorObject)
        {
            int code = errorObject["code"]?.GetValue<int>() ?? -32603;
            string message = errorObject["message"]?.GetValue<string>() ?? "Unknown";
            throw new RpcErrorException(code, message, errorObject["data"]);
        }

        return response["result"];
    }

    public static JsonObject BuildRequest(string requestId, string method, JsonObject @params)
    {
        JsonObject paramsCopy = JsonNode.Parse(@params.ToJsonString())?.AsObject() ?? new JsonObject();

        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = requestId,
            ["method"] = method,
            ["params"] = paramsCopy
        };
    }

    private string NextId()
    {
        _sequence++;
        return _sequence.ToString();
    }
}

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static Task<int> Main(string[] args)
    {
        return RunAsync(args);
    }

    public static async Task<int> RunAsync(
        string[] args,
        Func<JsonRpcClientConfig, UnityJsonRpcClient>? clientFactory = null)
    {
        if (args.Length == 0 || !string.Equals(args[0], "call", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 2;
        }

        Dictionary<string, string> options;
        try
        {
            options = ParseOptions(args.Skip(1).ToArray());
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }

        if (!options.TryGetValue("--endpoint", out string? endpoint) ||
            !options.TryGetValue("--token", out string? token) ||
            !options.TryGetValue("--method", out string? method))
        {
            PrintUsage();
            return 2;
        }

        string rawParams = options.TryGetValue("--params", out string? value) ? value : "{}";
        double timeoutSeconds = options.TryGetValue("--timeout", out string? timeoutText) &&
                                double.TryParse(timeoutText, out double parsed)
            ? parsed
            : 5.0;

        JsonNode? paramsNode;
        try
        {
            paramsNode = JsonNode.Parse(rawParams);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"invalid --params JSON: {ex.Message}");
            return 2;
        }

        if (paramsNode is not JsonObject paramObject)
        {
            Console.Error.WriteLine("--params must be a JSON object");
            return 2;
        }

        clientFactory ??= config => new UnityJsonRpcClient(config);

        UnityJsonRpcClient client = clientFactory(new JsonRpcClientConfig
        {
            Endpoint = endpoint,
            Token = token,
            TimeoutSeconds = timeoutSeconds
        });

        try
        {
            JsonNode? result = await client.CallAsync(method, paramObject, CancellationToken.None);
            string output = result?.ToJsonString(JsonOptions) ?? "null";
            Console.WriteLine(output);
            return 0;
        }
        catch (RpcErrorException ex)
        {
            JsonObject errorNode = new()
            {
                ["code"] = ex.Code,
                ["message"] = ex.RpcMessage
            };

            if (ex.DataNode != null)
            {
                errorNode["data"] = ex.DataNode.DeepClone();
            }

            Console.Error.WriteLine(errorNode.ToJsonString(JsonOptions));
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        Dictionary<string, string> options = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            string key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"missing value for option: {key}");
            }

            options[key] = args[i + 1];
            i++;
        }

        return options;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: dotnet run --project scripts/agent/UnityRpcClient/UnityRpcClient.csproj -- call " +
            "--endpoint <ws://host:port> --token <token> --method <rpc.method> [--params <json>] [--timeout <seconds>]");
    }
}
