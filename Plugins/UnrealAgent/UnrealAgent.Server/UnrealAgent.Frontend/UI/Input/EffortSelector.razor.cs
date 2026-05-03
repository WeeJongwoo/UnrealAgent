using Anthropic.Models.Messages;
using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class EffortSelector
{
    /// <summary>모델 설정 서비스입니다.</summary>
    [Inject] private ModelSettings Settings { get; set; } = null!;
    
    /// <summary>Effort 레벨 정의입니다.</summary>
    private record EffortLevel(string Label, Effort Value, string Description);
    
    /// <summary>선택 가능한 Effort 레벨 목록입니다.</summary>
    private static readonly EffortLevel[] Levels =
    [
        new("Low", Effort.Low, "빠른 응답, 기본 로직"),
        new("Mid", Effort.Medium, "속도와 복잡성의 균형"),
        new("High", Effort.High, "심층 분석, 복잡한 문제 해결")
    ];
    
    /// <summary>High 선택 시 Effort 라벨을 흰색으로 표시합니다.</summary>
    private string EffortLabelClass => Settings.Effort == Effort.High ? "text-white" : "text-[#666]";
    
    /// <summary>Effort 레벨을 변경합니다.</summary>
    private void SelectEffort(Effort Level)
    {
        Settings.Effort = Level;
    }
}