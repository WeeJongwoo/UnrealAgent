using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;
using static UnrealAgent.Frontend.UI.Messages.ToolBlock;

namespace UnrealAgent.Frontend.UI.Messages.ToolRenderers;

/// <summary>
/// web_fetch 도구의 콘텐츠 렌더러입니다.
/// URL 헤더와 페치된 콘텐츠를 표시합니다.
/// </summary>
public partial class WebFetchBlock : ComponentBase
{
    /// <summary>표시할 Tool 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.Tool Message { get; set; } = default!;

    /// <summary>이 도구의 summary 바 메타데이터입니다.</summary>
    public static ToolMeta GetInfo(ChatUIMessage.Tool Msg)
        => new("language", "Web Fetch", "font-mono", GetDomain(Msg));

    /// <summary>권한 다이얼로그에 표시할 요약입니다.</summary>
    public static string GetPermissionSummary(string InputJson)
        => ChatUIMessage.Tool.GetInputField(InputJson, "url");
    
    /// <summary>입력 JSON에서 원본 URL을 가져옵니다.</summary>
    private string FetchUrl => ChatUIMessage.Tool.GetInputField(Message.Input, "url");

    /// <summary>URL의 도메인을 추출합니다.</summary>
    private string Domain => GetDomain(Message);

    /// <summary>입력 JSON에서 URL의 도메인을 추출합니다.</summary>
    private static string GetDomain(ChatUIMessage.Tool Msg)
    {
        string Url = ChatUIMessage.Tool.GetInputField(Msg.Input, "url");
        return Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed) ? Parsed.Host : Url;
    }
}