using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Mode;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ModeSwitcher : ComponentBase
{
    /// <summary>현재 대화 세션입니다.</summary>
    [Inject] private AgentSession Session { get; set; } = null!;

    /// <summary>현재 모드입니다.</summary>
    private AgentMode CurrentMode => Session.Mode;
    
    /// <summary>모드를 직접 설정합니다.</summary>
    private void SetMode(AgentMode Mode)
    {
        Session.Mode = Mode;
    }
    
    /// <summary>모드를 순환 전환합니다: Normal → Edit → Plan → Normal</summary>
    public void CycleMode()
    {
        Session.Mode = Session.Mode switch
        {
            AgentMode.Normal => AgentMode.Edit,
            AgentMode.Edit   => AgentMode.Plan,
            _                => AgentMode.Normal
        };
        
        StateHasChanged();
    }
    
    /// <summary>모드별 도트 색상 클래스를 반환합니다.</summary>
    private static string DotColor(AgentMode Mode, bool bIsActive) => Mode switch
    {
        AgentMode.Normal => bIsActive ? "bg-[#94a3b8]" : "bg-[#94a3b8]/50",
        AgentMode.Edit   => bIsActive ? "bg-[#a855f7]" : "bg-[#a855f7]/50",
        AgentMode.Plan   => bIsActive ? "bg-[#22c55e]" : "bg-[#22c55e]/50",
        _                => "bg-[#94a3b8]/50"
    };

    /// <summary>모드별 텍스트 색상 클래스를 반환합니다.</summary>
    private static string TextColor(AgentMode Mode, bool bIsActive) => Mode switch
    {
        AgentMode.Normal => bIsActive ? "text-[#cbd5e1]" : "text-[#94a3b8]",
        AgentMode.Edit   => bIsActive ? "text-[#a855f7]" : "text-[#94a3b8]",
        AgentMode.Plan   => bIsActive ? "text-[#22c55e]" : "text-[#94a3b8]",
        _                => "text-[#94a3b8]"
    };
}