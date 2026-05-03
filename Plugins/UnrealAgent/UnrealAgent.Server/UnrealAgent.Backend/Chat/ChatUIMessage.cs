using System.Text.Json;

namespace UnrealAgent.Backend.Chat;

/// <summary>
/// UI에 표시되는 채팅 메시지입니다.
/// </summary>
public abstract record ChatUIMessage
{
    /// <summary>메시지 본문입니다.</summary>
    public abstract string Content { get; init; }
    
    /// <summary>메시지 생성 시간입니다.</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
    
    /// <summary>Content 뒤에 텍스트를 이어붙인 새 인스턴스를 반환합니다.</summary>
    public ChatUIMessage Append(string Text) => this with { Content = Content + Text };
    
    //-----------------------------------------------------------------------------
    // User
    //-----------------------------------------------------------------------------

    /// <summary>사용자 메시지입니다.</summary>
    public sealed record User(string Content) : ChatUIMessage
    {
        
    }
    
    //-----------------------------------------------------------------------------
    // Assistant
    //-----------------------------------------------------------------------------

    /// <summary>어시스턴트(AI) 응답입니다.</summary>
    public sealed record Assistant(string Content) : ChatUIMessage;
    
    /// <summary>사고 과정(Extended Thinking) 메시지입니다.</summary>
    public sealed record Thinking(string Content) : ChatUIMessage
    {
        /// <summary>사고 시작 시간입니다. UI에서 실시간 경과 시간을 계산합니다.</summary>
        public DateTime StartTime { get; init; }

        /// <summary>사고 과정에 소요된 최종 시간(초)입니다. 완료 후 확정됩니다.</summary>
        public double ElapsedSeconds { get; init; }

        /// <summary>완료 여부입니다.</summary>
        public bool bIsCompleted { get; init; }
    }
    
    //-----------------------------------------------------------------------------
    // Tool
    //-----------------------------------------------------------------------------

    /// <summary>도구 실행 메시지입니다.</summary>
    public sealed record Tool(string Name, string Content) : ChatUIMessage
    {
        /// <summary>Claude가 발급한 tool_use ID입니다.</summary>
        public string ToolUseId { get; init; } = "";

        /// <summary>도구 입력 파라미터(JSON)입니다.</summary>
        public string Input { get; init; } = "";
        
        /// <summary>도구 실행 시작 시간입니다. UI에서 실시간 경과 시간을 계산합니다.</summary>
        public DateTime StartTime { get; init; }
        
        /// <summary>도구 실행 소요 최종 시간(초)입니다. 완료 후 확정됩니다.</summary>
        public double ElapsedSeconds { get; init; }
        
        /// <summary>도구 실행 완료 여부입니다.</summary>
        public bool bIsCompleted { get; init; }
        
        /// <summary>JSON 문자열에서 지정 필드의 문자열 값을 추출합니다.</summary>
        public static string GetInputField(string Json, string FieldName, string Fallback = "")
        {
            if (string.IsNullOrEmpty(Json))
                return Fallback;

            try
            {
                using JsonDocument Doc = JsonDocument.Parse(Json);
                return Doc.RootElement.TryGetProperty(FieldName, out JsonElement Element)
                    ? Element.GetString() ?? Fallback
                    : Fallback;
            }
            catch
            {
                return Fallback;
            }
        }
    }
    
    
    /// <summary>시스템 메시지입니다.</summary>
    public sealed record System(string Content) : ChatUIMessage;
}