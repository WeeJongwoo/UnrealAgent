using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Sonnet 4.6 모델 정의입니다.
/// </summary>
[AgentModel(Order = 1)]
public sealed class Sonnet46 : IModel
{
    public const string ModelId = "claude-sonnet-4-6";
    public string Id => ModelId;
    public string DisplayName => "Sonnet 4.6";
    public string Description => "속도와 지능의 최적 균형을 갖춘 모델입니다.";
    public int MaxOutputTokens => 64_000;
    public int ContextWindow => 200_000;
}