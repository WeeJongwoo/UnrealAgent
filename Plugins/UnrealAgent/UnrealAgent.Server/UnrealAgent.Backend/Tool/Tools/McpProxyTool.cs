using System.Text.Json;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Mcp;

namespace UnrealAgent.Backend.Tool.Tools;

/// <summary>
/// MCP 서버의 도구를 IAgentTool로 래핑하는 프록시입니다.
/// 실행 시 McpClient를 통해 MCP 서버에 tools/call을 전달합니다.
/// </summary>
/// 
/// <example>
/// Claude가 tool_use "mcp__my-server__read_file" 생성
///   → McpProxyTool.ExecuteAsync()
///     → McpClient.CallToolAsync("read_file", arguments)
///       → MCP 서버에 tools/call 전달
///         → 결과를 ToolResult로 반환
/// </example>
public sealed class McpProxyTool(McpClient Client, string OriginalName) : IAgentTool
{
    /// <summary>
    /// MCP 서버에 tools/call 요청을 보내고 결과를 반환합니다.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(string InputJson, AgentSession Session, CancellationToken Ct = default)
    {
        // JSON 문자열 → JsonElement로 변환
        JsonElement? Arguments = string.IsNullOrWhiteSpace(InputJson)
            ? null
            : JsonDocument.Parse(InputJson).RootElement;

        // MCP 서버에 도구 실행 요청
        ToolCallResult Result = await Client.CallToolAsync(OriginalName, Arguments, Ct);

        // MCP 응답에서 텍스트 추출
        string Text = string.Join("\n", Result.Content
            .Where(C => C is { Type: "text", Text: not null })
            .Select(C => C.Text!));

        return Result.IsError
            ? ToolResult.Error(Text)
            : ToolResult.Success(Text);
    }
}