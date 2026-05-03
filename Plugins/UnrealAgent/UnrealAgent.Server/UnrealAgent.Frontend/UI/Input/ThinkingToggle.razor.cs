using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ThinkingToggle
{
    /// <summary>모델 설정 서비스입니다.</summary>
    [Inject] private ModelSettings Settings { get; set; } = null!;
    
    /// <summary>토글 상태를 전환합니다.</summary>
    private void Toggle() {  Settings.bThinkingEnabled = !Settings.bThinkingEnabled; }
    
    /// <summary>라벨 색상입니다. 활성화 시 흰색, 비활성화 시 회색입니다.</summary>
    private string LabelColorClass => Settings.bThinkingEnabled ? "text-white" : "text-[#666]";

    /// <summary>트랙 CSS 클래스입니다.</summary>
    private string TrackClass => Settings.bThinkingEnabled ? "think-on" : "think-off";
}