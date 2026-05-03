namespace UnrealAgent.Backend.Core;

public abstract record Block
{
    /// <summary>텍스트 응답 블록입니다.</summary>
    public sealed record Text(string Content) : Block;

    /// <summary>사고 과정(Extended Thinking) 블록입니다.</summary>
    public sealed record Thinking(string Content, string? Signature) : Block;
    
    /// <summary>도구 호출 블록입니다.</summary>
    public sealed record ToolUse(string Id, string Name, string InputJson) : Block;
}