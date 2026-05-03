using Markdig;
using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;

namespace UnrealAgent.Frontend.UI.Messages;

public partial class AssistantMessage
{
    /// <summary>표시할 어시스턴트 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.Assistant Message { get; set; } = null!;
    
    /// <summary>Markdig 파이프라인입니다.</summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
    
    /// <summary>Markdown 텍스트를 HTML로 변환합니다. </summary>
    private static string RenderMarkdown(string Md) => Markdown.ToHtml(Md, Pipeline);
}