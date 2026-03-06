#!/usr/bin/env node
'use strict';

const DEFAULT_TIMEOUT_SECONDS = 5;
const DEFAULT_PORT = 17777;

class RpcError extends Error
{
    constructor(code, message, data)
    {
        super(`RPC error ${code}: ${message}`);
        this.name = 'RpcError';
        this.code = code;
        this.rpcMessage = message;
        this.data = data;
    }
}

class WebSocketTransport
{
    constructor(endpoint, timeoutMs)
    {
        if (typeof WebSocket === 'undefined')
        {
            throw new Error('Global WebSocket is not available. Use Node.js 22+.');
        }

        this._queue = [];
        this._waiters = [];
        this._socket = new WebSocket(endpoint);
        this._ready = this._createReadyPromise(timeoutMs);

        this._socket.addEventListener('message', event =>
        {
            const payload = typeof event.data === 'string'
                ? event.data
                : Buffer.from(event.data).toString('utf8');

            if (this._waiters.length > 0)
            {
                const resolve = this._waiters.shift();
                resolve(payload);
                return;
            }

            this._queue.push(payload);
        });
    }

    _createReadyPromise(timeoutMs)
    {
        return new Promise((resolve, reject) =>
        {
            const timer = setTimeout(() =>
            {
                reject(new Error(`WebSocket connect timeout after ${timeoutMs}ms.`));
            }, timeoutMs);

            const cleanup = () =>
            {
                clearTimeout(timer);
                this._socket.removeEventListener('open', handleOpen);
                this._socket.removeEventListener('error', handleError);
            };

            const handleOpen = () =>
            {
                cleanup();
                resolve();
            };

            const handleError = event =>
            {
                cleanup();
                reject(event?.error instanceof Error ? event.error : new Error('WebSocket connection failed.'));
            };

            this._socket.addEventListener('open', handleOpen, { once: true });
            this._socket.addEventListener('error', handleError, { once: true });
        });
    }

    async send(payload)
    {
        await this._ready;
        this._socket.send(payload);
    }

    async receive(timeoutMs)
    {
        await this._ready;

        if (this._queue.length > 0)
        {
            return this._queue.shift();
        }

        return await new Promise((resolve, reject) =>
        {
            const timer = setTimeout(() =>
            {
                this._waiters = this._waiters.filter(item => item !== handleMessage);
                reject(new Error(`WebSocket receive timeout after ${timeoutMs}ms.`));
            }, timeoutMs);

            const handleMessage = payload =>
            {
                clearTimeout(timer);
                resolve(payload);
            };

            this._waiters.push(handleMessage);
        });
    }

    async close()
    {
        if (!this._socket || this._socket.readyState >= WebSocket.CLOSING)
        {
            return;
        }

        await new Promise(resolve =>
        {
            const finalize = () => resolve();

            this._socket.addEventListener('close', finalize, { once: true });
            this._socket.close(1000, 'done');

            setTimeout(finalize, 250);
        });
    }
}

class UnityJsonRpcClient
{
    constructor(config, transportFactory)
    {
        this._config = config;
        this._sequence = 0;
        this._transportFactory = transportFactory ?? ((endpoint, timeoutMs) => new WebSocketTransport(endpoint, timeoutMs));
    }

    async call(method, params)
    {
        const timeoutMs = Math.max(1, Math.floor(this._config.timeoutSeconds * 1000));
        const transport = this._transportFactory(this._config.endpoint, timeoutMs);

        try
        {
            await this._sendAndReceive(transport, 'agent.ping', {});

            try
            {
                return await this._sendAndReceive(transport, method, params ?? {});
            }
            catch (error)
            {
                if (!(error instanceof RpcError) || error.code !== -32001)
                {
                    throw error;
                }

                await this._sendAndReceive(transport, 'agent.authenticate', { token: this._config.token });
                return await this._sendAndReceive(transport, method, params ?? {});
            }
        }
        finally
        {
            await transport.close();
        }
    }

    async _sendAndReceive(transport, method, params)
    {
        const request = buildRequest(this._nextId(), method, params);
        await transport.send(JSON.stringify(request));

        const timeoutMs = Math.max(1, Math.floor(this._config.timeoutSeconds * 1000));
        const responsePayload = await transport.receive(timeoutMs);
        const response = JSON.parse(responsePayload);

        if (!response || typeof response !== 'object')
        {
            throw new Error('Invalid JSON-RPC response.');
        }

        if (response.error && typeof response.error === 'object')
        {
            const code = typeof response.error.code === 'number' ? response.error.code : -32603;
            const message = typeof response.error.message === 'string' ? response.error.message : 'Unknown';
            throw new RpcError(code, message, response.error.data);
        }

        return response.result ?? null;
    }

    _nextId()
    {
        this._sequence += 1;
        return String(this._sequence);
    }
}

function buildRequest(id, method, params)
{
    return {
        jsonrpc: '2.0',
        id,
        method,
        params: JSON.parse(JSON.stringify(params ?? {}))
    };
}

function parseOptions(args)
{
    const options = new Map();

    for (let index = 0; index < args.length; index += 1)
    {
        const key = args[index];
        if (!key.startsWith('--'))
        {
            continue;
        }

        if (index + 1 >= args.length)
        {
            throw new Error(`missing value for option: ${key}`);
        }

        options.set(key.toLowerCase(), args[index + 1]);
        index += 1;
    }

    return options;
}

function resolveOptionOrEnv(options, optionKey, envKey, env)
{
    const optionValue = options.get(optionKey);
    if (typeof optionValue === 'string' && optionValue.trim() !== '')
    {
        return optionValue.trim();
    }

    const envValue = env[envKey];
    return typeof envValue === 'string' && envValue.trim() !== '' ? envValue.trim() : null;
}

function resolveEndpoint(options, env)
{
    const endpoint = resolveOptionOrEnv(options, '--endpoint', 'UNITY_RPC_ENDPOINT', env);
    if (endpoint)
    {
        return endpoint;
    }

    const host = typeof env.UNITY_RPC_HOST === 'string' ? env.UNITY_RPC_HOST.trim() : '';
    if (!host)
    {
        return null;
    }

    const portValue = typeof env.UNITY_RPC_PORT === 'string' ? env.UNITY_RPC_PORT.trim() : '';
    const port = Number.isInteger(Number(portValue)) && portValue !== '' ? Number(portValue) : DEFAULT_PORT;
    return `ws://${host}:${port}`;
}

function printUsage(stderr)
{
    stderr.write(
        'Usage: node scripts/agent/unity-rpc.js call ' +
        '--endpoint <ws://host:port> --token <token> --method <rpc.method> [--params <json>] [--timeout <seconds>]\n' +
        'Env fallback: UNITY_RPC_ENDPOINT or UNITY_RPC_HOST(+UNITY_RPC_PORT), UNITY_RPC_TOKEN\n');
}

async function runAsync(args, dependencies = {})
{
    const stdout = dependencies.stdout ?? process.stdout;
    const stderr = dependencies.stderr ?? process.stderr;
    const env = dependencies.env ?? process.env;

    if (args.length === 0 || String(args[0]).toLowerCase() !== 'call')
    {
        printUsage(stderr);
        return 2;
    }

    let options;
    try
    {
        options = parseOptions(args.slice(1));
    }
    catch (error)
    {
        stderr.write(`${error.message}\n`);
        printUsage(stderr);
        return 2;
    }

    const endpoint = resolveEndpoint(options, env);
    const token = resolveOptionOrEnv(options, '--token', 'UNITY_RPC_TOKEN', env);
    const method = options.get('--method');

    if (!method || !endpoint || !token)
    {
        if (!endpoint)
        {
            stderr.write('missing endpoint: pass --endpoint or set UNITY_RPC_ENDPOINT (or UNITY_RPC_HOST)\n');
        }

        if (!token)
        {
            stderr.write('missing token: pass --token or set UNITY_RPC_TOKEN\n');
        }

        printUsage(stderr);
        return 2;
    }

    const rawParams = options.get('--params') ?? '{}';
    const timeoutSeconds = Number.parseFloat(options.get('--timeout') ?? `${DEFAULT_TIMEOUT_SECONDS}`);
    const safeTimeout = Number.isFinite(timeoutSeconds) ? timeoutSeconds : DEFAULT_TIMEOUT_SECONDS;

    let params;
    try
    {
        params = JSON.parse(rawParams);
    }
    catch (error)
    {
        stderr.write(`invalid --params JSON: ${error.message}\n`);
        return 2;
    }

    if (!params || Array.isArray(params) || typeof params !== 'object')
    {
        stderr.write('--params must be a JSON object\n');
        return 2;
    }

    const clientFactory = dependencies.clientFactory ?? (config => new UnityJsonRpcClient(config));
    const client = clientFactory({
        endpoint,
        token,
        timeoutSeconds: safeTimeout
    });

    try
    {
        const result = await client.call(method, params);
        stdout.write(`${JSON.stringify(result ?? null)}\n`);
        return 0;
    }
    catch (error)
    {
        if (error instanceof RpcError)
        {
            const payload = {
                code: error.code,
                message: error.rpcMessage
            };

            if (typeof error.data !== 'undefined')
            {
                payload.data = error.data;
            }

            stderr.write(`${JSON.stringify(payload)}\n`);
            return 1;
        }

        stderr.write(`${error.message}\n`);
        return 1;
    }
}

async function main(argv)
{
    return await runAsync(argv);
}

if (require.main === module)
{
    main(process.argv.slice(2))
        .then(exitCode =>
        {
            process.exitCode = exitCode;
        })
        .catch(error =>
        {
            process.stderr.write(`${error.message}\n`);
            process.exitCode = 1;
        });
}

module.exports = {
    DEFAULT_PORT,
    DEFAULT_TIMEOUT_SECONDS,
    RpcError,
    UnityJsonRpcClient,
    WebSocketTransport,
    buildRequest,
    main,
    parseOptions,
    resolveEndpoint,
    resolveOptionOrEnv,
    runAsync
};
