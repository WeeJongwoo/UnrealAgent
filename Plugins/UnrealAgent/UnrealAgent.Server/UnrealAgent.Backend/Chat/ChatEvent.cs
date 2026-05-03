using UnrealAgent.Backend.Security;

namespace UnrealAgent.Backend.Chat;

/// <summary>
/// 에이전트에서 UI로 전달되는 스트리밍 이벤트입니다.
/// </summary>
public abstract record ChatEvent
{
    /// <summary>사용자 메시지입니다. </summary>
    public sealed record User(string Content) : ChatEvent;
    
    /// <summary>Claude의 텍스트 응답입니다.</summary>
    public sealed record Assistant(string Content) : ChatEvent;
    
    /// <summary>Claude의 사고 과정(Extended Thinking) 응답입니다.</summary>
    public sealed record Thinking(string Content) : ChatEvent;
    
    /// <summary>도구 실행 시작입니다.</summary>
    public sealed record ToolStart(string ToolUseId, string Name, string Input) : ChatEvent;
    
    /// <summary>도구 실행 결과입니다.</summary>
    public sealed record ToolEnd(string ToolUseId, string Name, string Result) : ChatEvent;
    
    /// <summary>도구 실행 권한 요청입니다. UI에서 허용/거부 다이얼로그를 표시합니다.</summary>
    public sealed record ToolPermissionRequest(string ToolUseId, string ToolName, string InputJson) : ChatEvent
    {
        /// <summary>UI에서 SetResult로 응답하면 AgentLoop의 await가 깨어납니다.</summary>
        public TaskCompletionSource<ToolPermission> Tcs { get; } = new();
    }
    
    /// <summary>시스템 메시지입니다 (커맨드 결과, 에러 등).</summary>
    public sealed record System(string Content) : ChatEvent;
    
    /// <summary>스트림 종료입니다.</summary>
    public sealed record Done : ChatEvent;
}