namespace UnrealAgent.Backend.Conversation;

/// <summary>
/// 사용자 메시지 1건과 그에 대한 에이전트 응답 전체를 포함하는 구간입니다.
/// 모델이 도구를 반복 호출하면 AssistantSpan이 여러 개 쌓입니다.
/// </summary>
public sealed class MessageSpan
{
    /// <summary>사용자 입력입니다. 대화 압축 시에는 null입니다.</summary>
    public UserInput? UserInput { get; init; }

    /// <summary>이 메시지에서 수행된 API 호출 결과 목록입니다.</summary>
    public List<AssistantSpan> AssistantSpans { get; } = [];
}