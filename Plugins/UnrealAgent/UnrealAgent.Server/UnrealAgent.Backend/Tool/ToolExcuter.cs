using System.Runtime.CompilerServices;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Chat;
using UnrealAgent.Backend.Conversation;
using UnrealAgent.Backend.Core;
using UnrealAgent.Backend.Tool.Attributes;

namespace UnrealAgent.Backend.Tool;

/// <summary>
/// 도구를 실행하고 결과를 AssistantSpan에 기록합니다.
/// </summary>
public sealed class ToolExecutor(ToolRegistry ToolRegistry)
{
    /// <summary>
    /// 도구를 실행하고 결과를 AssistantSpan의 ToolExecutions에 추가합니다.
    /// 도구가 세션 모드를 변경하면 ModeChanged 이벤트를 발행합니다.
    /// </summary>
    public async IAsyncEnumerable<ChatEvent> ExecuteAsync(Block.ToolUse ToolCall, AssistantSpan AssistantSpan,
        AgentSession Session, [EnumeratorCancellation] CancellationToken Ct = default)
    {
        // 도구 시작 알림
        yield return new ChatEvent.ToolStart(ToolCall.Id, ToolCall.Name, ToolCall.InputJson);
        
        // 도구 실행
        ToolResult Result = await ToolRegistry.ExecuteAsync(ToolCall.Name, ToolCall.InputJson, Session, Ct);

        // 도구 실행 결과 저장
        AssistantSpan.ToolExecution ToolExecution = new AssistantSpan.ToolExecution(ToolCall.Id, ToolCall.Name, Result.Content, !Result.bIsSuccess);
        AssistantSpan.ToolExecutions.Add(ToolExecution);
        
        // 도구 종료 알림
        yield return new ChatEvent.ToolEnd(ToolCall.Id, ToolCall.Name, Result.Content);
    }
    
    
}