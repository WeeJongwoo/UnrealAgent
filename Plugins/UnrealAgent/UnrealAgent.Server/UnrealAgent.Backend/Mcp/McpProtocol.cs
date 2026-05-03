using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnrealAgent.Backend.Mcp;

//-----------------------------------------------------------------------------
// JSON-RPC 요청
//-----------------------------------------------------------------------------

/// <summary>
/// JSON-RPC 2.0 요청 메시지입니다.
/// </summary>
public sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Params { get; init; }
}

//-----------------------------------------------------------------------------
// JSON-RPC 응답
//-----------------------------------------------------------------------------

/// <summary>
/// JSON-RPC 2.0 응답 메시지입니다.
/// </summary>
public sealed class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; init; }

    public bool IsSuccess => Error is null;
}

//-----------------------------------------------------------------------------
// JSON-RPC 에러
//-----------------------------------------------------------------------------

/// <summary>
/// JSON-RPC 2.0 에러 객체입니다.
/// </summary>
public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

//-----------------------------------------------------------------------------
// initialize 파라미터 / 결과
//-----------------------------------------------------------------------------

/// <summary>
/// initialize 요청의 params입니다.
/// </summary>
public sealed class InitializeParams
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = "2025-03-26";

    [JsonPropertyName("clientInfo")]
    public ClientInfo ClientInfo { get; init; } = new();
}

/// <summary>클라이언트 정보입니다.</summary>
public sealed class ClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "unreal-agent";

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";
}

/// <summary>
/// initialize 응답의 result입니다.
/// </summary>
public sealed class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; init; } = string.Empty;

    [JsonPropertyName("serverInfo")]
    public ServerInfo ServerInfo { get; init; } = new();

    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; init; } = new();
}

/// <summary>서버 정보입니다.</summary>
public sealed class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
}

/// <summary>서버가 지원하는 기능입니다.</summary>
public sealed class ServerCapabilities
{
    [JsonPropertyName("tools")]
    public JsonElement? Tools { get; init; }

    /// <summary>서버가 도구를 제공하는지 여부입니다.</summary>
    public bool HasTools => Tools is not null;
}

//-----------------------------------------------------------------------------
// tools/list 결과
//-----------------------------------------------------------------------------

/// <summary>
/// tools/list 응답의 result입니다.
/// </summary>
public sealed class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpToolDefinition> Tools { get; init; } = [];
}

/// <summary>
/// MCP 서버가 제공하는 도구 하나의 정의입니다.
/// </summary>
public sealed class McpToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("inputSchema")]
    public JsonElement InputSchema { get; init; }
}

//-----------------------------------------------------------------------------
// tools/call 파라미터 / 결과
//-----------------------------------------------------------------------------

/// <summary>
/// tools/call 요청의 params입니다.
/// </summary>
public sealed class ToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }
}

/// <summary>
/// tools/call 응답의 result입니다.
/// </summary>
public sealed class ToolCallResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; init; } = [];

    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}

/// <summary>
/// MCP 콘텐츠 블록입니다. (text, image 등)
/// </summary>
public sealed class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}