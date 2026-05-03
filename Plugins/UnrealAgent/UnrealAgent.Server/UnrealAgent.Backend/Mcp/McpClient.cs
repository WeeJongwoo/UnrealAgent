using System.Net.Http.Json;
using System.Text.Json;

namespace UnrealAgent.Backend.Mcp;

/// <summary>
/// HTTP를 통해 MCP 서버와 통신하는 클라이언트입니다.
/// initialize → tools/list → tools/call 흐름을 처리합니다.
/// </summary>
public class McpClient(HttpClient Http, string ServerName, string Url)
{
    /// <summary>JSON-RPC 요청 ID 카운터입니다.</summary>
    private int NextId;
    
    /// <summary>initialize 결과입니다. 연결 후 서버 정보를 담고 있습니다.</summary>
    public InitializeResult? ServerResult { get; private set; }
    
    /// <summary>서버가 도구를 지원하는지 여부입니다.</summary>
    public bool HasTools => ServerResult?.Capabilities.HasTools ?? false;
    
    //-------------------------------------------------------------------------
    // initialize 핸드셰이크
    //-------------------------------------------------------------------------
    
    /// <summary>
    /// MCP 서버에 initialize 요청을 보내고 서버 정보를 받습니다.
    /// </summary>
    public async Task<InitializeResult> InitializeAsync(CancellationToken Ct = default)
    {
        InitializeResult Result = await SendAsync<InitializeParams, InitializeResult>(
            "initialize",
            new InitializeParams(),
            Ct
        );

        ServerResult = Result;

        return Result;
    }
    
    //-------------------------------------------------------------------------
    // tools/list
    //-------------------------------------------------------------------------

    /// <summary>
    /// 서버에서 사용 가능한 도구 목록을 가져옵니다.
    /// </summary>
    public async Task<List<McpToolDefinition>> ListToolsAsync(CancellationToken Ct = default)
    {
        ToolsListResult Result = await SendAsync<object, ToolsListResult>(
            "tools/list",
            new { },
            Ct
        );

        return Result.Tools;
    }
    
    //-------------------------------------------------------------------------
    // tools/call
    //-------------------------------------------------------------------------

    /// <summary>
    /// MCP 서버의 도구를 실행합니다.
    /// </summary>
    public async Task<ToolCallResult> CallToolAsync(string ToolName, JsonElement? Arguments, CancellationToken Ct = default)
    {
        return await SendAsync<ToolCallParams, ToolCallResult>(
            "tools/call",
            new ToolCallParams { Name = ToolName, Arguments = Arguments },
            Ct
        );
    }
    
    //-------------------------------------------------------------------------
    // 내부 통신
    //-------------------------------------------------------------------------

    /// <summary>
    /// JSON-RPC 요청을 보내고 응답의 result를 역직렬화하여 반환합니다.
    /// </summary>
    /// <example>
    /// 요청 예시:
    /// POST http://localhost:3000/mcp
    /// {"jsonrpc":"2.0", "id":1, "method":"initialize", "params":{"protocolVersion":"2025-03-26", "clientInfo":{...}}}
    ///
    /// 응답 예시:
    /// {"jsonrpc":"2.0", "id":1, "result":{"protocolVersion":"2025-03-26", "serverInfo":{...}, "capabilities":{...}}}
    /// </example>
    private async Task<TResult> SendAsync<TParams, TResult>(string Method, TParams Params, CancellationToken Ct)
    {
        JsonRpcRequest Request = new()
        {
            Id = Interlocked.Increment(ref NextId),
            Method = Method,
            Params = Params
        };

        // UE HTTP 서버는 Content-Length 헤더를 필수로 요구합니다.
        // PostAsJsonAsync는 chunked transfer를 사용할 수 있으므로,
        // 직접 직렬화하여 Content-Length가 포함된 요청을 보냅니다.
        string JsonBody = JsonSerializer.Serialize(Request);
        using StringContent Content = new(JsonBody, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage HttpResponse = await Http.PostAsync(Url, Content, Ct);
        HttpResponse.EnsureSuccessStatusCode();

        JsonRpcResponse? RpcResponse = await HttpResponse.Content.ReadFromJsonAsync<JsonRpcResponse>(Ct);

        if (RpcResponse is null)
            throw new InvalidOperationException($"[{ServerName}] 빈 응답을 받았습니다.");

        if (!RpcResponse.IsSuccess)
            throw new InvalidOperationException($"[{ServerName}] {RpcResponse.Error!.Message} (code: {RpcResponse.Error.Code})");

        return RpcResponse.Result!.Value.Deserialize<TResult>()
               ?? throw new InvalidOperationException($"[{ServerName}] result 역직렬화에 실패했습니다.");
    }
}