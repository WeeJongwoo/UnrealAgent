using Microsoft.AspNetCore.Components;
using UnrealAgent.Backend.Model;

namespace UnrealAgent.Frontend.UI.Input;

public partial class ModelSelector
{
    /// <summary>모델 설정 서비스입니다.</summary>
    [Inject] private ModelSettings Settings { get; set; } = null!;
    
    /// <summary>모델 레지스트리 서비스입니다.</summary>
    [Inject] private ModelRegistry Registry { get; set; } = null!;
    
    /// <summary>드롭다운 열림 상태입니다.</summary>
    private bool bIsOpen;
    
    /// <summary>드롭다운을 열거나 닫습니다.</summary>
    private void ToggleDropdown() => bIsOpen = !bIsOpen;
    
    /// <summary>현재 모델의 아이콘 글자입니다.</summary>
    private string ModelIcon => Settings.DisplayName.Length > 0
        ? Settings.DisplayName[0].ToString()
        : "U";
    
    /// <summary>모델을 선택하고 드롭다운을 닫습니다.</summary>
    private void SelectModel(IModel Model)
    {
        Settings.Select(Model);
        bIsOpen = false;
    }
    
    /// <summary>모델별 아이콘 배경색입니다.</summary>
    private static string GetIconBg(IModel Model) => Model.DisplayName[0] switch
    {
        'O' => "bg-[#444]",
        'S' => "bg-[#333]",
        _ => "bg-[#2a2a2a]"
    };
    
    /// <summary>모델별 아이콘 글자색입니다.</summary>
    private static string GetIconColor(IModel Model) => Model.DisplayName[0] switch
    {
        'O' => "text-[#e0e0e0]",
        'S' => "text-[#aaa]",
        _ => "text-[#888]"
    };
}