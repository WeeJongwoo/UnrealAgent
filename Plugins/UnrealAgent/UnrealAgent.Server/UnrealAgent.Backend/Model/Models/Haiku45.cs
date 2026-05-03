using UnrealAgent.Backend.Model.Attributes;

namespace UnrealAgent.Backend.Model.Models;

/// <summary>
/// Claude Haiku 4.5 모델 정의입니다.
/// </summary>
[AgentModel(Order = 2)]
public sealed class Haiku45 : IModel
{
    public const string ModelId = "claude-haiku-4-5-20251001";
    public string Id => ModelId;
    public string DisplayName => "Haiku 4.5";
    public string Description => "최전선급 지능을 갖춘 가장 빠른 모델입니다.";
    public int MaxOutputTokens => 64_000;
    public int ContextWindow => 200_000;
}