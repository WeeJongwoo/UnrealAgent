namespace UnrealAgent.Backend.Chat;

/// <summary>
/// UI에 표시할 채팅 메시지 저장소입니다.
/// ChatEvent를 처리하여 ChatUIMessage 목록을 구성합니다.
/// </summary>
public sealed class ChatStore
{
    /// <summary>UI에 표시되는 채팅 메시지 목록입니다.</summary>
    public List<ChatUIMessage> Messages { get; } = [];
    
    /// <summary>현재 턴에서 응답 수신이 시작되었는지 여부입니다.</summary>
    public bool bIsReceiving { get; private set; }

    /// <summary>
    /// ChatEvent를 처리하여 UI 메시지를 업데이트합니다.
    /// </summary>
    public void Process(ChatEvent Evt)
    {
        bIsReceiving = Evt is ChatEvent.User;

        switch (Evt)
        {
            case ChatEvent.User { Content: var Content }:
            {
                Messages.Add(new ChatUIMessage.User(Content));

                break;
            }

            case ChatEvent.Assistant { Content: var Content }:
            {
                ThinkingComplete();

                if (Messages.Count > 0 && Messages[^1] is ChatUIMessage.Assistant)
                {
                    Messages[^1] = Messages[^1].Append(Content);
                }
                else
                {
                    Messages.Add(new ChatUIMessage.Assistant(Content));
                }

                break;
            }

            case ChatEvent.Thinking { Content: var Content }:
            {
                if (Messages.Count > 0 && Messages[^1] is ChatUIMessage.Thinking { bIsCompleted: false })
                {
                    Messages[^1] = Messages[^1].Append(Content);
                }
                else
                {
                    Messages.Add(new ChatUIMessage.Thinking(Content)
                    {
                        StartTime = DateTime.Now,
                        bIsCompleted = false
                    });
                }

                break;
            }
            
            case ChatEvent.ToolStart { ToolUseId: var ToolUseId, Name: var Name, Input: var Input }:
            {
                ThinkingComplete();

                Messages.Add(new ChatUIMessage.Tool(Name, "")
                {
                    ToolUseId = ToolUseId,
                    Input = Input,
                    StartTime = DateTime.Now,
                    bIsCompleted = false
                });
                
                break;
            }
            
            case ChatEvent.ToolEnd { ToolUseId: var EndToolUseId, Name: var Name, Result: var Result }:
            {
                (int ToolIdx, ChatUIMessage.Tool? ToolMsg) = FindPendingTool(EndToolUseId, Name);

                if (ToolIdx < 0)
                    return;

                Messages[ToolIdx] = ToolMsg! with
                {
                    Content = Result,
                    ElapsedSeconds = (DateTime.Now - ToolMsg.StartTime).TotalSeconds,
                    bIsCompleted = true
                };

                break;
            }
            
            case ChatEvent.System { Content: var Content }:
            {
                Messages.Add(new ChatUIMessage.System(Content));
                break;
            }

            case ChatEvent.Done:
            {
                ThinkingComplete();

                break;
            }
        }
    }
    
    /// <summary>미완료 Thinking 메시지를 완료 처리합니다.</summary>
    private void ThinkingComplete()
    {
        if (Messages.Count > 0 && Messages[^1] is ChatUIMessage.Thinking { bIsCompleted: false } T)
        {
            Messages[^1] = T with
            {
                ElapsedSeconds = (DateTime.Now - T.StartTime).TotalSeconds,
                bIsCompleted = true
            };
        }
    }
    
    /// <summary>tool_use ID가 일치하는 pending Tool 메시지의 인덱스와 객체를 찾습니다.</summary>
    private (int Index, ChatUIMessage.Tool? Tool) FindPendingTool(string ToolUseId, string Name)
    {
        for (int I = Messages.Count - 1; I >= 0; I--)
        {
            if (Messages[I] is ChatUIMessage.Tool { bIsCompleted: false } T &&
                (string.IsNullOrWhiteSpace(ToolUseId) ? T.Name == Name : T.ToolUseId == ToolUseId))
                return (I, T);
        }

        return (-1, null);
    }
}