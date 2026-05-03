namespace UnrealAgent.Backend.Mode;

/// <summary>
/// 에이전트 실행 모드입니다.
/// </summary>
public enum AgentMode
{
    /// <summary>일반적인 상태입니다.</summary>
    Normal,
    /// <summary>모든 도구가 자동 승인됩니다.</summary>
    Edit,
    /// <summary>모든 도구가 차단되며, 계획적인 분석을 수행합니다.</summary>
    Plan
}