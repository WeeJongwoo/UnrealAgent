using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Opus 4.6 모델 정의입니다.
/// </summary>
[AgentModel(Order = 0)]
public sealed class Opus46 : IModel
{
    public const string ModelId = "claude-opus-4-6";
    public string Id => ModelId;
    public string DisplayName => "Opus 4.6";
    public string Description => "에이전트 구축과 코딩에 최적화된 최고 지능 모델입니다.";
    public int MaxOutputTokens => 128_000;
    public int ContextWindow => 200_000;
}