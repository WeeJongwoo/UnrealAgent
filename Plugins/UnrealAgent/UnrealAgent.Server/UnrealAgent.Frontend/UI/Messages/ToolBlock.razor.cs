using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Chat;
using UnrealAgent.Frontend.UI.Messages.ToolRenderers;
using WebSearchBlock = UnrealAgent.Frontend.UI.Messages.ToolRenderers.WebSearchBlock;

namespace UnrealAgent.Frontend.UI.Messages;

/// <summary>
/// 도구 실행 블록의 코드-비하인드입니다.
/// 공통 셸(아이콘, 정보 바, 타이머, 색상 테마)만 담당하고,
/// 도구별 콘텐츠 렌더링은 ToolRenderers의 개별 컴포넌트에 위임합니다.
/// </summary>
public partial class ToolBlock : IDisposable
{
    /// <summary>표시할 Tool 메시지입니다.</summary>
    [Parameter] public ChatUIMessage.Tool Message { get; set; } = default!;
    
    /// <summary>경과 시간 갱신용 타이머입니다.</summary>
    private Timer? _Timer;
    
    //--------------------------------------------------------------------------
    // 도구 메타데이터 — 각 렌더러의 static GetInfo()에서 가져옵니다.
    //--------------------------------------------------------------------------

    /// <summary>
    /// summary 바에 표시할 도구별 UI 메타데이터입니다.
    /// Icon: Material Symbol 이름, Label: 카테고리, Font: 도구명 폰트, DisplayName: 세부 이름.
    /// </summary>
    public record struct ToolMeta(string Icon, string Label, string Font, string DisplayName);
    
    /// <summary>현재 도구의 메타데이터입니다. 각 렌더러 컴포넌트가 자신의 메타를 제공합니다.</summary>
    private ToolMeta Info => Message.Name switch
    {
        "web_search" => WebSearchBlock.GetInfo(Message),
        "web_fetch"  => WebFetchBlock.GetInfo(Message),
        _ when CodeBlock.IsCodeTool(Message.Name) => CodeBlock.GetInfo(Message),
        _            => new("terminal", "Tool:", "font-mono", Message.Name)
    };
    
    //--------------------------------------------------------------------------
    // 라이프사이클
    //--------------------------------------------------------------------------

    /// <summary>컴포넌트 초기화 시 진행 중이면 타이머를 시작합니다.</summary>
    protected override void OnInitialized()
    {
        if (!Message.bIsCompleted)
            _Timer = new Timer(_ => InvokeAsync(StateHasChanged), null, 0, 100);
    }

    /// <summary>파라미터 변경 시 완료되면 타이머를 정지합니다.</summary>
    protected override void OnParametersSet()
    {
        if (Message.bIsCompleted)
        {
            _Timer?.Dispose();
            _Timer = null;
        }
    }
    
    /// <summary>타이머 리소스를 해제합니다.</summary>
    public void Dispose()
    {
        _Timer?.Dispose();
        _Timer = null;
    }
    
    //--------------------------------------------------------------------------
    // 색상 테마 — 완료: 녹색(#4ba96c), 진행 중: 주황색(#d68a51)
    //--------------------------------------------------------------------------

    /// <summary>완료 여부에 따라 완료 값 또는 진행 중 값을 선택합니다.</summary>
    private string ByState(string Completed, string InProgress)
        => Message.bIsCompleted ? Completed : InProgress;

    /// <summary>아이콘 원형의 외곽선 색상입니다.</summary>
    private string BorderClass => ByState("border-[#4ba96c]", "border-[#d68a51]");

    /// <summary>아이콘 원형의 글로우 그림자입니다.</summary>
    private string ShadowClass => ByState(
        "shadow-[0_0_12px_rgba(75,169,108,0.15)]",
        "shadow-[0_0_12px_rgba(214,138,81,0.15)]");

    /// <summary>아이콘의 텍스트 색상입니다.</summary>
    private string IconColorClass => ByState("text-[#4ba96c]", "text-[#d68a51]");

    /// <summary>정보 바의 배경색입니다.</summary>
    private string BgClass => ByState("bg-[#1a2e1a]", "bg-[#2a1f0f]");

    /// <summary>정보 바의 외곽선 색상입니다.</summary>
    private string BorderBarClass => ByState("border-[#2a5a2a]", "border-[#5a3a1a]");

    /// <summary>정보 바의 텍스트 색상입니다.</summary>
    private string TextColorClass => ByState("text-[#4ba96c]", "text-[#d68a51]");

    /// <summary>타이머 숫자의 색상입니다.</summary>
    private string TimerColorClass => ByState("text-[#4ba96c]/70", "text-[#d68a51]/70");

    /// <summary>도구명 텍스트의 색상입니다.</summary>
    private string ToolNameClass => ByState("text-[#7ad89e]", "text-[#e8a86b]");

    /// <summary>도구명 배지의 외곽선 색상입니다.</summary>
    private string ToolNameBorderClass => ByState("border-[#7ad89e]/20", "border-[#e8a86b]/20");

    //--------------------------------------------------------------------------
    // 경과 시간 (ThinkingBlock과 동일한 타이머 패턴)
    //--------------------------------------------------------------------------

    /// <summary>경과 시간 표시 문자열입니다. 완료 시 고정, 진행 중이면 실시간 갱신됩니다.</summary>
    private string ElapsedDisplay => Message.bIsCompleted
        ? $"{Message.ElapsedSeconds:F1}s"
        : $"{(DateTime.Now - Message.StartTime).TotalSeconds:F1}s";
}