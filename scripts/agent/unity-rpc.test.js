'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');

const {
    buildProgressPayload,
    extractTestRunResult,
    RpcError,
    UnityJsonRpcClient,
    buildRequest,
    runAsync
} = require('./unity-rpc');

test('buildRequest 应包含 JSON-RPC 基本字段', () =>
{
    const request = buildRequest('1', 'agent.ping', {});

    assert.equal(request.jsonrpc, '2.0');
    assert.equal(request.id, '1');
    assert.equal(request.method, 'agent.ping');
    assert.deepEqual(request.params, {});
});

test('call 应先执行 ping 再调用目标方法', async () =>
{
    const fakeTransport = new FakeTransport([
        '{"jsonrpc":"2.0","id":"1","result":{"pong":true}}',
        '{"jsonrpc":"2.0","id":"2","result":{"found":true}}'
    ]);

    const client = new UnityJsonRpcClient(
        { endpoint: 'ws://127.0.0.1:17777', timeoutSeconds: 5 },
        () => fakeTransport);

    const result = await client.call('unity.gameobject.find', { name: 'Player' });

    assert.equal(result.found, true);
    assert.equal(fakeTransport.sent[0].method, 'agent.ping');
    assert.equal(fakeTransport.sent[1].method, 'unity.gameobject.find');
    assert.equal(fakeTransport.closed, true);
});

test('runAsync 在 params 不是对象时应返回 2', async () =>
{
    const stderr = createBufferWriter();

    const exitCode = await runAsync(
        [
            'call',
            '--endpoint', 'ws://127.0.0.1:17777',
            '--method', 'agent.ping',
            '--params', '[]'
        ],
        { stderr });

    assert.equal(exitCode, 2);
    assert.match(stderr.value(), /--params must be a JSON object/);
});

test('runAsync 在 RPC 错误时应返回 1 并输出错误 JSON', async () =>
{
    const stderr = createBufferWriter();

    const exitCode = await runAsync(
        [
            'call',
            '--endpoint', 'ws://127.0.0.1:17777',
            '--method', 'agent.unknown',
            '--params', '{}'
        ],
        {
            stderr,
            clientFactory: () => ({
                call: async () =>
                {
                    throw new RpcError(-32601, 'Method not found');
                }
            })
        });

    assert.equal(exitCode, 1);
    assert.match(stderr.value(), /"code":-32601/);
    assert.match(stderr.value(), /"message":"Method not found"/);
});

test('runAsync 应支持从环境变量解析 endpoint', async () =>
{
    const stdout = createBufferWriter();

    const exitCode = await runAsync(
        [
            'call',
            '--method', 'agent.ping',
            '--params', '{}'
        ],
        {
            stdout,
            env: {
                UNITY_RPC_HOST: '127.0.0.1',
                UNITY_RPC_PORT: '17777'
            },
            clientFactory: config => ({
                call: async () =>
                {
                    assert.equal(config.endpoint, 'ws://127.0.0.1:17777');
                    return { pong: true };
                }
            })
        });

    assert.equal(exitCode, 0);
    assert.match(stdout.value(), /"pong":true/);
});

test('runAsync 执行 unity.tests.run 时应转发服务端推送的进度事件', async () =>
{
    const stdout = createBufferWriter();
    const stderr = createBufferWriter();

    const exitCode = await runAsync(
        [
            'call',
            '--endpoint', 'ws://127.0.0.1:17777',
            '--method', 'unity.tests.run',
            '--params', '{"mode":"EditMode"}'
        ],
        {
            stdout,
            stderr,
            clientFactory: () => ({
                async runTestsWithProgress(params, onProgress)
                {
                    assert.equal(params.mode, 'EditMode');
                    onProgress({ event: 'started', runId: 'run-1', mode: 'EditMode', status: 'running' });
                    onProgress({
                        event: 'progress',
                        runId: 'run-1',
                        mode: 'EditMode',
                        status: 'running',
                        summary: { total: 2, completed: 1, currentTest: 'A' },
                        failures: []
                    });
                    onProgress({
                        event: 'completed',
                        runId: 'run-1',
                        mode: 'EditMode',
                        status: 'passed',
                        summary: { total: 2, completed: 2, passed: 2 },
                        failures: []
                    });

                    return {
                        completed: true,
                        runId: 'run-1',
                        mode: 'EditMode',
                        status: 'passed',
                        summary: { total: 2, completed: 2, passed: 2 },
                        failures: []
                    };
                }
            })
        });

    assert.equal(exitCode, 0);

    const progressLines = stderr.value().trim().split('\n').map(line => JSON.parse(line));
    assert.deepEqual(progressLines.map(line => line.event), ['started', 'progress', 'completed']);
    assert.equal(progressLines.at(-1).status, 'passed');
    assert.match(stdout.value(), /"status":"passed"/);
});

test('runTestsWithProgress 应处理响应前到达的进度通知', async () =>
{
    const notifications = [];
    const fakeTransport = new FakeTransport([
        '{"jsonrpc":"2.0","id":"1","result":{"pong":true}}',
        '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"started","runId":"run-2","status":"running"}}}',
        '{"jsonrpc":"2.0","id":"2","result":{"started":true,"completed":false,"runId":"run-2","status":"running"}}',
        '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"progress","runId":"run-2","status":"running","summary":{"total":1,"completed":1}}}}',
        '{"jsonrpc":"2.0","method":"agent.event","params":{"name":"unity.tests.progress","payload":{"event":"completed","runId":"run-2","status":"passed","summary":{"total":1,"completed":1,"passed":1},"failures":[]}}}'
    ]);

    const client = new UnityJsonRpcClient(
        { endpoint: 'ws://127.0.0.1:17777', timeoutSeconds: 5 },
        () => fakeTransport);

    const result = await client.runTestsWithProgress(
        { mode: 'EditMode' },
        payload => notifications.push(payload));

    assert.equal(result.status, 'passed');
    assert.deepEqual(notifications.map(item => item.event), ['started', 'progress', 'completed']);
    assert.equal(fakeTransport.closed, true);
});

test('buildProgressPayload 应包含标准进度字段', () =>
{
    const payload = buildProgressPayload({ event: 'progress', runId: 'run-3', status: 'running' });

    assert.equal(payload.event, 'progress');
    assert.equal(payload.source, 'unity.tests.run');
    assert.equal(payload.runId, 'run-3');
    assert.equal(payload.status, 'running');
});

test('extractTestRunResult 应将完成事件转换为最终结果', () =>
{
    const result = extractTestRunResult({
        event: 'completed',
        runId: 'run-4',
        mode: 'PlayMode',
        status: 'failed',
        summary: { total: 3, completed: 3, failed: 1 },
        failures: [{ name: 'SampleTest' }]
    });

    assert.equal(result.completed, true);
    assert.equal(result.mode, 'PlayMode');
    assert.equal(result.status, 'failed');
    assert.equal(result.failures[0].name, 'SampleTest');
});

test('resolveEndpoint 应支持从本地多实例配置中选择最近运行实例', () =>
{
    const endpoint = require('./unity-rpc').resolveEndpoint(
        new Map(),
        {},
        {
            cwd: '/repo',
            fs: createJsonFs({
                '/repo/UserSettings/AgentBridgeSettings.json': {
                    Instances: [
                        {
                            InstanceId: 'unity-a',
                            ProcessId: 1001,
                            Host: '127.0.0.1',
                            Port: 17777,
                            IsRunning: true,
                            LastSeenUtc: '2026-03-11T01:00:00.0000000Z'
                        },
                        {
                            InstanceId: 'unity-b',
                            ProcessId: 1002,
                            Host: '127.0.0.1',
                            Port: 17778,
                            IsRunning: true,
                            LastSeenUtc: '2026-03-11T02:00:00.0000000Z'
                        }
                    ]
                }
            })
        });

    assert.equal(endpoint, 'ws://127.0.0.1:17778');
});

test('resolveEndpoint 应支持按 instance 参数选择指定实例', () =>
{
    const endpoint = require('./unity-rpc').resolveEndpoint(
        new Map([['--instance', '1001']]),
        {},
        {
            cwd: '/repo',
            fs: createJsonFs({
                '/repo/UserSettings/AgentBridgeSettings.json': {
                    Instances: [
                        {
                            InstanceId: 'unity-a',
                            ProcessId: 1001,
                            Host: '127.0.0.1',
                            Port: 17777,
                            IsRunning: true,
                            LastSeenUtc: '2026-03-11T01:00:00.0000000Z'
                        },
                        {
                            InstanceId: 'unity-b',
                            ProcessId: 1002,
                            Host: '127.0.0.1',
                            Port: 17778,
                            IsRunning: true,
                            LastSeenUtc: '2026-03-11T02:00:00.0000000Z'
                        }
                    ]
                }
            })
        });

    assert.equal(endpoint, 'ws://127.0.0.1:17777');
});

class FakeTransport
{
    constructor(responses)
    {
        this._responses = [...responses];
        this.sent = [];
        this.closed = false;
    }

    async send(payload)
    {
        this.sent.push(JSON.parse(payload));
    }

    async receive()
    {
        if (this._responses.length === 0)
        {
            throw new Error('No response queued.');
        }

        return this._responses.shift();
    }

    async close()
    {
        this.closed = true;
    }
}

function createBufferWriter()
{
    let content = '';

    return {
        write(chunk)
        {
            content += chunk;
        },
        value()
        {
            return content;
        }
    };
}

function createJsonFs(files)
{
    return {
        existsSync(filePath)
        {
            return Object.prototype.hasOwnProperty.call(files, filePath);
        },
        readFileSync(filePath)
        {
            return JSON.stringify(files[filePath]);
        }
    };
}
