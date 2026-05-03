namespace UnrealAgent.Backend.Model.Attributes;

/// <summary>
/// ModelRegistry가 자동 스캔하는 모델 마커 어트리뷰트입니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AgentModelAttribute : Attribute
{
    /// <summary>레거시 모델 여부입니다.</summary>
    public bool bIsLegacy { get; set; } = false;

    /// <summary>정렬 순서입니다. 낮을수록 먼저 표시됩니다.</summary>
    public int Order { get; set; } = 100;
}