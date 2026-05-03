namespace UnrealAgent.Backend.Model;

/// <summary>
/// 모델 정의 인터페이스입니다.
/// 각 모델 클래스가 이 인터페이스를 구현합니다.
/// </summary>
public interface IModel
{
    /// <summary>Claude API 모델 ID입니다 (예: "claude-opus-4-6").</summary>
    string Id { get; }

    /// <summary>UI에 표시할 모델 이름입니다 (예: "Claude Opus 4.6").</summary>
    string DisplayName { get; }

    /// <summary>모델 설명입니다.</summary>
    string Description { get; }

    /// <summary>최대 출력 토큰 수입니다.</summary>
    int MaxOutputTokens { get; }

    /// <summary>컨텍스트 윈도우 크기입니다.</summary>
    int ContextWindow { get; }
}