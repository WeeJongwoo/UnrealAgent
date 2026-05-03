using System.Text.Json;
using Anthropic.Models.Messages;
using Block = UnrealAgent.Backend.Core.Block;

namespace UnrealAgent.Backend.Conversation;

/// <summary>
/// Claude API 대화 히스토리를 관리합니다.
/// MessageSpan 기반으로 사용자 턴과 API 호출 결과를 구조화하여 저장합니다.
/// </summary>
public sealed class Conversation
{
    /// <summary>메시지 구간(사용자 1턴) 목록입니다.</summary>
    private readonly List<MessageSpan> MessageSpans = [];
    
    /// <summary>MessageSpan을 추가하고 반환합니다.</summary>
    public MessageSpan AddMessageSpan(UserInput Input)
    {
        MessageSpan MessageSpan = new() { UserInput = Input };
        MessageSpans.Add(MessageSpan);
        
        return MessageSpan;
    }
    
    /// <summary>
    /// 도메인 모델을 Anthropic API 메시지 형식으로 변환합니다.
    /// API는 user ↔ assistant 교대를 요구하며, tool_result는 user role로 전송합니다.
    /// 예: [user] → [assistant: text+tool_use] → [user: tool_result] → [assistant: text] …
    /// </summary>
    public List<MessageParam> ToAnthropicMessages()
    {
        List<MessageParam> Messages = [];

        foreach (MessageSpan MessageSpan in MessageSpans)
        {
            // user 메시지입니다. 
            if (MessageSpan.UserInput is not null)
                Messages.Add(ConvertUserInput(MessageSpan.UserInput));

            // Assistant 메세지 입니다.
            foreach (AssistantSpan Span in MessageSpan.AssistantSpans)
            {
                //Assistant 대답
                Messages.Add(ConvertAssistantBlocks(Span.AssistantBlocks));
                
                // Assistant 도구 실행 결과
                if (Span.ToolExecutions.Count > 0)
                    Messages.Add(ConvertToolResults(Span.ToolExecutions));
            }
        }
        
        return Messages;
    }
    
    /// <summary>
    /// UserInput을 Anthropic API 메시지로 변환합니다.
    /// 이미지가 없으면 텍스트 전용, 있으면 이미지 + 텍스트 블록으로 구성합니다.
    /// </summary>
    private static MessageParam ConvertUserInput(UserInput Input)
    {
        List<ContentBlockParam> Blocks = new List<ContentBlockParam>();
        
        if (!string.IsNullOrWhiteSpace(Input.Text))
            Blocks.Add(new TextBlockParam { Text = Input.Text });
        
        return new MessageParam { Role = Role.User, Content = Blocks };
    }
    
    /// <summary>
    /// 도메인 Block 목록을 Anthropic API 어시스턴트 메시지로 변환합니다.
    /// </summary>
    private static MessageParam ConvertAssistantBlocks(IReadOnlyList<Block> Blocks)
    {
        List<ContentBlockParam> ContentBlocks = new List<ContentBlockParam>();

        foreach (Block Block in Blocks)
        {
            switch (Block)
            {
                case Block.Text { Content: { } Content }:
                {
                    ContentBlocks.Add(new TextBlockParam { Text = Content });
                    break;
                }

                case Block.Thinking { Content: { } Content, Signature: { } Signature }:
                {
                    ContentBlocks.Add(new ThinkingBlockParam { Thinking = Content, Signature = Signature });
                    break;
                }
                
                case Block.ToolUse { Id: { } Id, Name: { } Name, InputJson: { } InputJson }:
                {
                    Dictionary<string, JsonElement> ParsedInput = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(InputJson) ?? new Dictionary<string, JsonElement>();
                    ContentBlocks.Add(new ToolUseBlockParam { ID = Id, Name = Name, Input = ParsedInput });
                    break;
                }
            }
        }
        
        return new MessageParam { Role = Role.Assistant, Content = ContentBlocks };
    }
    
    /// <summary>
    /// 도구 실행 결과를 Anthropic API user 메시지(ToolResult)로 변환합니다.
    /// </summary>
    private static MessageParam ConvertToolResults(IReadOnlyList<AssistantSpan.ToolExecution> Executions)
    {
        List<ContentBlockParam> ResultBlocks = Executions.Select(E => (ContentBlockParam)new ToolResultBlockParam
        {
            ToolUseID = E.ToolUseId,
            Content = E.Output,
            IsError = E.bIsError ? true : null
        }).ToList();
        
        return new MessageParam { Role = Role.User, Content = ResultBlocks };
    }
}